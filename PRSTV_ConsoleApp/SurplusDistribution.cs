using Azure.Core;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PRSTV_ClassLibrary.CountClasses;
using PRSTV_ConsoleApp.Data;
using PRSTV_ConsoleApp.LocalDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PRSTV_ConsoleApp
{
    public sealed class SurplusDistribution
    {
        private SurplusDistribution() { }

        public static async Task<SurplusDistribution> CreateAsync(int electionId,int currentCountNumber,CandidateCountState electedCandidateWithSurplus, IAppUi? ui = null)
        {
            var instance = new SurplusDistribution();

            using var countDb = new CountContext();
            using var rawDb = new RawBallotDbContext();

            var nextCountNumber = currentCountNumber + 1;

            // Quota is required for deterministic quota-trimming in the next count.
            var quota = await countDb.Elections
                .AsNoTracking()
                .Where(e => e.ElectionId == electionId)
                .Select(e => e.Quota)
                .FirstAsync();

            // 1) Determine who is "continuing" in the CURRENT count (excluding elected/excluded as needed)
            var continuingCandidateIds = await GetContinuingCandidateIdsAsync(countDb, electionId, currentCountNumber);

            // 2) Determine which ballots are eligible to transfer from this candidate's surplus
            // Common rule: transfer ballots that were last transferred to them (LatestCountTransfered = max)
            var latestTransferCount = await GetCountWithLatestTransferedBallotsAsync(countDb, electionId, electedCandidateWithSurplus.CandidateId);

            var sourceBallots = await GetBallotsForSurplusTransferAsync(countDb, electionId, latestTransferCount, electedCandidateWithSurplus.CandidateId);

            if (sourceBallots.Count > 0)
            {
                // 3) Load their preferences from raw DB
                List<long> ballotIds = sourceBallots.Select(b => b.RandomBallotId).Distinct().ToList();
                var prefsByBallot = await LoadPreferencesByBallotAsync(rawDb, ballotIds, electionId);

                // 4) Build candidate-to-candidate transfers for next count.
                // IMPORTANT: In surplus distribution, last-parcel ballots with *no next continuing preference*
                // are "non-transferable" and are *retained with the elected candidate*.
                // They are NOT inserted as exhausted rows here; they will be carried forward like any other
                // untouched ballot, and any reduction needed to bring the elected candidate down to quota is
                // handled deterministically by quota-trimming ("non-transferable not effective").
                var nextRows = BuildNextBallotStateRows(electionId, nextCountNumber, sourceBallots, prefsByBallot, continuingCandidateIds);

                int lastParcelNumber = nextRows.Count;

                // Only the rows with a next continuing preference are transferable.
                // Rows with IsExhausted==true represent "no next continuing preference" and are retained.
                var nonTransferableBallots = nextRows.Where(x => x.IsExhausted);
                var transferable = nextRows.Where(x => !x.IsExhausted);

                int totalTransferablesRetainedFromQuota = lastParcelNumber - electedCandidateWithSurplus.Surplus;
                int transferablesRetainedFromQuota = totalTransferablesRetainedFromQuota - nonTransferableBallots.Count();
                

                double transferFactor;
                if (transferable.Count() <= 0)
                {
                    transferFactor = 0;
                }
                else if (transferablesRetainedFromQuota <= electedCandidateWithSurplus.Surplus)
                {
                    transferFactor = 1;
                }
                else
                {
                    transferFactor = (double)electedCandidateWithSurplus.Surplus / (double)(transferable.Count());
                }



                ui?.Log("lastParcelNumber: " + lastParcelNumber);
                ui?.Log("Transferable: " + transferable.Count());
                ui?.Log("Non-Transferable: " + nonTransferableBallots.Count());
                ui?.Log("transferablesRetainedFromQuota: " + transferablesRetainedFromQuota);
                ui?.Log("totalTransferablesRetainedFromQuota: " + totalTransferablesRetainedFromQuota);
                ui?.Log("Surplus: " + electedCandidateWithSurplus.Surplus);
                ui?.Log("Transfer Factor: " + transferFactor);
                List<BallotCurrentState> transfers;

                // Stop "visual" or "engine" rounding-up when transfer factor is effectively 1.
                // If TransferFactor is >= 0.999999, transfer everything that is transferable.
                if (transferFactor >= 0.999999)
                {
                    transfers = transferable.ToList();
                }
                else
                {
                    // Only distribute transferables; exhausted (non-transferable) ballots are inserted separately.
                    transfers = DistributeVotesWithTransferFactor(transferable.ToList(), electedCandidateWithSurplus.Surplus, transferFactor, ui);
                }

                // Insert ONLY the candidate-to-candidate transfers.
                // Non-transferables (no next continuing preference) are retained with the elected candidate
                // by carry-forward, and any "excess above quota" is handled by quota-trimming.
                await countDb.BallotCurrentStates.AddRangeAsync(transfers);
                await countDb.SaveChangesAsync();
            }

            // 5) Carry forward everything else (including ballots still sitting with elected candidate that did NOT transfer)
            await CarryForwardUntouchedBallotsAsync(countDb, electionId, currentCountNumber, nextCountNumber);

            // 5.5) Quota-trimming: ensure the elected candidate's total in the NEXT count is exactly the quota.
            // Any "spare" ballots above quota still sitting with the elected candidate are converted to exhausted
            // ballots ("non-transferable not effective").
            await TrimElectedCandidateToQuotaAsync(
                countDb,
                electionId,
                nextCountNumber,
                electedCandidateWithSurplus.CandidateId,
                quota);

            // 6) Insert candidate snapshot for next count (same approach you used)
            await InsertCandidateSnapshotAsync(countDb, electionId, currentCountNumber, nextCountNumber, electedCandidateWithSurplus.CandidateId);


            return instance;
        }

        // ---------------------------
        // Continuing candidates
        // ---------------------------

        private static Task<HashSet<int>> GetContinuingCandidateIdsAsync(CountContext countDb, int electionId, int currentCount)
        {
            // Adjust statuses to match your rules:
            // - Running only
            // - Or Running + Elected (if your rules allow receiving later transfers)
            return countDb.Candidates
                .AsNoTracking()
                .Where(c => c.ElectionId == electionId
                         && c.CountNumber == currentCount
                         && c.Status == (int)CandidateStatus.Running)
                .Select(c => c.CandidateId)
                .ToHashSetAsync();
        }

        // ---------------------------
        // Select which ballots to transfer (surplus rule)
        // ---------------------------

        private sealed record SourceBallot(long RandomBallotId, int CurrentPreference);

        private static async Task<int> GetCountWithLatestTransferedBallotsAsync(CountContext countDb, int electionId, int candidateId)
        {
            const string sql = @"
                SELECT TOP 1 LatestCountTransfered
                FROM PRSTV_Local.dbo.BallotCurrentStates
                WHERE CurrentCandidateId = @CandidateId
                  AND ElectionId = @ElectionId
                ORDER BY LatestCountTransfered DESC;";

            await countDb.Database.OpenConnectionAsync();
            try
            {
                var connection = countDb.Database.GetDbConnection();
                await using var command = connection.CreateCommand();
                command.CommandText = sql;

                var p1 = command.CreateParameter();
                p1.ParameterName = "@CandidateId";
                p1.Value = candidateId;
                command.Parameters.Add(p1);

                var p2 = command.CreateParameter();
                p2.ParameterName = "@ElectionId";
                p2.Value = electionId;
                command.Parameters.Add(p2);

                var result = await command.ExecuteScalarAsync();
                return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
            finally
            {
                await countDb.Database.CloseConnectionAsync();
            }
        }

        private static Task<List<SourceBallot>> GetBallotsForSurplusTransferAsync(CountContext countDb,int electionId,int fromCountNumber,int candidateId)
        {
            // Your rule here matches what you were already attempting:
            // ballots currently with candidate, AND were last transferred at that count.
            return countDb.BallotCurrentStates
                .AsNoTracking()
                .Where(b => b.ElectionId == electionId
                         && b.CountNumber == fromCountNumber
                         && b.LatestCountTransfered == fromCountNumber
                         && b.CurrentCandidateId == candidateId
                         && !b.IsExhausted)
                .Select(b => new SourceBallot(b.RandomBallotId, b.CurrentPreference))
                .ToListAsync();
        }

        // ---------------------------
        // Raw DB prefs (same as ExclusionDistribution)
        // ---------------------------

        private sealed record BallotPref(long RandomBallotId, int CandidateId, int Preference);

        private static async Task<Dictionary<long, List<BallotPref>>> LoadPreferencesByBallotAsync(RawBallotDbContext rawDb, List<long> ballotIds, int electionId)
        {
            var prefs = await (
               from p in rawDb.BallotPreferences.AsNoTracking()
               join bp in rawDb.BallotPapers.AsNoTracking()
                   on p.RandomBallotId equals bp.RandomBallotId
               where ballotIds.Contains(p.RandomBallotId)
                  && bp.ElectionId == electionId
               select new BallotPref(
                   p.RandomBallotId,
                   p.CandidateId,
                   p.Preference
               )
           ).ToListAsync();
            return prefs
                .GroupBy(p => p.RandomBallotId)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(x => x.Preference).ToList());
        }

        // ---------------------------
        // Build next rows (same logic as exclusion)
        // ---------------------------

        private static List<BallotCurrentState> BuildNextBallotStateRows(int electionId,int nextCount ,List<SourceBallot> sourceBallots, Dictionary<long, List<BallotPref>> prefsByBallot, HashSet<int> continuingCandidateIds)
        {
            var nextRows = new List<BallotCurrentState>(sourceBallots.Count);
            // For each source ballot, find next continuing preference or exhaust
            foreach (var ballot in sourceBallots)
            {
                // No preferences found at all -> exhausted
                if (!prefsByBallot.TryGetValue(ballot.RandomBallotId, out var preferences))
                {
                    nextRows.Add(CreateExhaustedBallotRow(electionId, nextCount, ballot.RandomBallotId, ballot.CurrentPreference));
                    continue;
                }
                // Find next valid preference for continuing candidate
                var nextValidPref = preferences.FirstOrDefault(p =>
                    p.Preference > ballot.CurrentPreference &&
                    continuingCandidateIds.Contains(p.CandidateId));
                // Create appropriate next row
                nextRows.Add(
                    nextValidPref is null
                        ? CreateExhaustedBallotRow(electionId, nextCount, ballot.RandomBallotId, ballot.CurrentPreference)
                        : CreateTransferredBallotRow(electionId, nextCount, ballot.RandomBallotId, nextValidPref.CandidateId, nextValidPref.Preference)
                );
            }

            return nextRows;
        }


        private static List<BallotCurrentState> DistributeVotesWithTransferFactor(List<BallotCurrentState> transferable, int surplus, double transferFactor, IAppUi? ui = null)
        {
            var groupedByCandidate = transferable
                .Where(b => b.CurrentCandidateId.HasValue)
                .GroupBy(b => b.CurrentCandidateId)
                .ToDictionary(g => g.Key, g => g.ToList());
            Dictionary<int, double> candidateVoteTransfers = new Dictionary<int, double>();
            Dictionary<int, int> candidateVoteTransfersRounded = new Dictionary<int, int>();
            List<BallotCurrentState> Transfers = new List<BallotCurrentState>();

            foreach (var group in groupedByCandidate)
            {
                var candidateId = group.Key;
                var ballots = group.Value;
                int totalVotesForCandidate = ballots.Count;
                double voteTransferUnits = totalVotesForCandidate * transferFactor;
                ui?.Log($"{candidateId}: {voteTransferUnits}");
                candidateVoteTransfers[candidateId.Value] = voteTransferUnits;
                candidateVoteTransfersRounded[candidateId.Value] = (int)Math.Floor(voteTransferUnits);
            }

            ui?.Log($"Sum Before: {candidateVoteTransfersRounded.Values.Sum()}");
            while (candidateVoteTransfersRounded.Values.Sum() < surplus)
            {
                int candidateToRoundUp = GetCandidateWithHighestDecimalRemainder(candidateVoteTransfers);
                candidateVoteTransfers[candidateToRoundUp] = Math.Ceiling(candidateVoteTransfers[candidateToRoundUp]);

                candidateVoteTransfersRounded[candidateToRoundUp] = (int)candidateVoteTransfers[candidateToRoundUp]++;
                ui?.Log($"Candidate Rounded: {candidateToRoundUp}");
            }
            ui?.Log($"Sum After: {candidateVoteTransfersRounded.Values.Sum()}");

            foreach (var kvp in candidateVoteTransfersRounded)
            {
                candidateVoteTransfersRounded[kvp.Key] = kvp.Value;
                ui?.Log($"Candidate {kvp.Key} final transfer units: {kvp.Value}");
            }


            if (candidateVoteTransfersRounded.Values.Sum() > surplus)
            {
                throw new Exception("Error in transfer factor calculation: total transfers exceed surplus after rounding.");
            }

            foreach(var group in groupedByCandidate)
            {
                group.Value.OrderBy(b => b.RandomBallotId)
                    .Take(candidateVoteTransfersRounded[group.Key.Value])
                    .ToList()
                    .ForEach(b => Transfers.Add(b));
            }
           
            return Transfers;
        }
        private static int GetCandidateWithHighestDecimalRemainder(Dictionary<int, double> candidateVoteTransfers)
        {
            return candidateVoteTransfers
                .Select(kvp => new { CandidateId = kvp.Key, Remainder = kvp.Value - Math.Floor(kvp.Value) })
                .OrderByDescending(x => x.Remainder)
                .First().CandidateId;
        }
        private static BallotCurrentState CreateExhaustedBallotRow(int electionId, int nextCount, long ballotId, int currentPreference)
        {
            return new BallotCurrentState
            {
                ElectionId = electionId,
                CountNumber = nextCount,
                RandomBallotId = ballotId,
                CurrentCandidateId = null,
                CurrentPreference = currentPreference,
                IsExhausted = true,
                LatestCountTransfered = nextCount
            };
        }

        private static BallotCurrentState CreateTransferredBallotRow(
            int electionId, int nextCount, long ballotId, int nextCandidateId, int nextPreference)
        {
            return new BallotCurrentState
            {
                ElectionId = electionId,
                CountNumber = nextCount,
                RandomBallotId = ballotId,
                CurrentCandidateId = nextCandidateId,
                CurrentPreference = nextPreference,
                IsExhausted = false,
                LatestCountTransfered = nextCount
            };
        }
        // ---------------------------
        // Carry forward & snapshot (reuse from exclusion)
        // ---------------------------

        private static Task CarryForwardUntouchedBallotsAsync(CountContext countDb, int electionId, int currentCount, int nextCount)
        {
            return countDb.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO BallotCurrentStates
                    (ElectionId, CountNumber, RandomBallotId, CurrentCandidateId, CurrentPreference, IsExhausted, LatestCountTransfered)
                SELECT
                    {electionId}       AS ElectionId,
                    {nextCount}        AS CountNumber,
                    cur.RandomBallotId,
                    cur.CurrentCandidateId,
                    cur.CurrentPreference,
                    cur.IsExhausted,
                    cur.LatestCountTransfered
                FROM BallotCurrentStates AS cur
                WHERE cur.ElectionId = {electionId}
                  AND cur.CountNumber = {currentCount}
                  AND NOT EXISTS (
                      SELECT 1
                      FROM BallotCurrentStates AS nxt
                      WHERE nxt.ElectionId = {electionId}
                        AND nxt.CountNumber = {nextCount}
                        AND nxt.RandomBallotId = cur.RandomBallotId
                  );
            ");
        }

        private static Task TrimElectedCandidateToQuotaAsync(
            CountContext countDb,
            int electionId,
            int nextCount,
            int electedCandidateId,
            int quota)
        {
            // Deterministic quota-trimming ("non-transferable not effective"):
            // - Find all non-exhausted ballots assigned to the elected candidate in nextCount
            // - Keep only the first `quota` by deterministic ordering on RandomBallotId
            // - Exhaust the remainder
            return countDb.Database.ExecuteSqlInterpolatedAsync($@"
                WITH Ranked AS (
                    SELECT
                        b.RandomBallotId,
                        ROW_NUMBER() OVER (ORDER BY b.RandomBallotId ASC) AS rn
                    FROM BallotCurrentStates b
                    WHERE b.ElectionId = {electionId}
                      AND b.CountNumber = {nextCount}
                      AND b.IsExhausted = 0
                      AND b.CurrentCandidateId = {electedCandidateId}
                )
                UPDATE b
                SET
                    b.CurrentCandidateId = NULL,
                    b.IsExhausted = 1,
                    b.LatestCountTransfered = {nextCount}
                FROM BallotCurrentStates b
                INNER JOIN Ranked r
                    ON r.RandomBallotId = b.RandomBallotId
                WHERE b.ElectionId = {electionId}
                  AND b.CountNumber = {nextCount}
                  AND b.IsExhausted = 0
                  AND b.CurrentCandidateId = {electedCandidateId}
                  AND r.rn > {quota};
            ");
        }

        private static Task InsertCandidateSnapshotAsync(CountContext countDb, int electionId, int currentCount, int nextCount, int electedCandidateId)
        {
            var sql = @"
            WITH Totals AS (
                SELECT CAST(b.CurrentCandidateId AS BIGINT) AS CandidateId,
                       COUNT(*) AS TotalVotes
                FROM BallotCurrentStates b
                WHERE b.ElectionId = @electionId
                  AND b.CountNumber = @nextCount
                  AND b.IsExhausted = 0
                  AND b.CurrentCandidateId IS NOT NULL
                GROUP BY b.CurrentCandidateId
            ),
            Transfers AS (
                SELECT CAST(b.CurrentCandidateId AS BIGINT) AS CandidateId,
                       COUNT(*) AS TransferredVotes
                FROM BallotCurrentStates b
                WHERE b.ElectionId = @electionId
                  AND b.CountNumber = @nextCount
                  AND b.IsExhausted = 0
                  AND b.CurrentCandidateId IS NOT NULL
                  AND b.LatestCountTransfered = @nextCount
                GROUP BY b.CurrentCandidateId
            )
            INSERT INTO CandidateCountStates
                (ElectionId, CandidateId, Name, Status, Surplus, CountNumber, TotalVotes, BallotsTransferredAmount)
            SELECT
                @electionId AS ElectionId,
                prev.CandidateId,
                prev.Name,
                prev.Status,
                CASE WHEN prev.CandidateId = @electedCandidateId THEN 0 ELSE prev.Surplus END AS Surplus,
                @nextCount AS CountNumber,
                ISNULL(t.TotalVotes, 0) AS TotalVotes,
                NULLIF(ISNULL(tr.TransferredVotes, 0), 0) AS BallotsTransferredAmount
            FROM CandidateCountStates prev
            LEFT JOIN Totals    t  ON t.CandidateId  = prev.CandidateId
            LEFT JOIN Transfers tr ON tr.CandidateId = prev.CandidateId
            WHERE prev.ElectionId = @electionId
              AND prev.CountNumber = @currentCount;
            ";

            return countDb.Database.ExecuteSqlRawAsync(
                sql,
                new SqlParameter("@electionId", electionId),
                new SqlParameter("@nextCount", nextCount),
                new SqlParameter("@currentCount", currentCount),
                new SqlParameter("@electedCandidateId", electedCandidateId)
            );
        }

    }
}
