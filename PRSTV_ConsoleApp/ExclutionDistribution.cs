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
    public class ExclusionDistribution
    {
        public int CandidatesToExcludeAmount { get; private set; } = 1;

        private ExclusionDistribution() { }

        public static async Task<ExclusionDistribution> CreateAsync(int electionId,int currentCountNumber,CandidateCountState excludedCandidate)
        {

            return await CreateAsync(electionId, currentCountNumber, new List<CandidateCountState> { excludedCandidate });
        }

        public static async Task<ExclusionDistribution> CreateAsync(int electionId,int currentCountNumber,List<CandidateCountState> excludedCandidates)
        {
            var instance = new ExclusionDistribution
            {
                CandidatesToExcludeAmount = excludedCandidates.Count
            };

            var excludedCandidateIds = excludedCandidates
                .Select(c => c.CandidateId)
                .ToHashSet();

            using var countDb = new CountContext();
            using var rawDb = new RawBallotDbContext();

            var nextCountNumber = currentCountNumber + 1;

            // 1) Move ballots that belong to excluded candidate(s) into next count
            await instance.TransferBallotsFromExcludedCandidatesAsync(electionId,currentCountNumber,nextCountNumber,excludedCandidateIds,rawDb,countDb);

            // 2) Copy the rest of the ballots forward + create candidate snapshot for next count
            await instance.BuildNextCountSnapshotAsync(electionId,currentCountNumber,nextCountNumber,excludedCandidateIds,countDb);

            return instance;
        }

        /// <summary>
        /// Transfers ballots that are currently assigned to excluded candidates.
        /// Only these ballots are inserted for next count; everything else is carried forward later.
        /// </summary>
        private async Task TransferBallotsFromExcludedCandidatesAsync(int electionId,int currentCount,int nextCount,HashSet<int> excludedCandidateIds,RawBallotDbContext rawDb,CountContext countDb)
        {
            // Running candidates in the CURRENT count (excluding the ones being excluded)
            var runningCandidateIds = await GetRunningCandidateIdsAsync(countDb,electionId,currentCount,excludedCandidateIds);

            var runningSet = runningCandidateIds.ToHashSet();

            // Ballots currently sitting with ANY excluded candidate at current count
            var excludedCandidateIdsInt = excludedCandidateIds.Select(id => (int)id).ToHashSet();

            var excludedBallots = await GetBallotsHeldByExcludedCandidatesAsync(countDb,electionId,currentCount,excludedCandidateIdsInt);

            if (excludedBallots.Count == 0)
                return;

            // Pull preferences for those ballots from raw DB (Postgres)
            var ballotIds = excludedBallots.Select(b => b.RandomBallotId).Distinct().ToList();

            var prefsByBallot = await LoadPreferencesByBallotAsync(rawDb, ballotIds, electionId);

            // Decide next placement for each ballot and create next-count rows
            var nextRows = BuildNextBallotStateRows(electionId,nextCount,excludedBallots,prefsByBallot,runningSet);

            await countDb.BallotCurrentStates.AddRangeAsync(nextRows);
            await countDb.SaveChangesAsync();
        }

        private async Task BuildNextCountSnapshotAsync(int electionId, int currentCount, int nextCount, HashSet<int> excludedCandidateIds, CountContext countDb)
        {
            // Carry-forward ballots not already inserted into nextCount
            await CarryForwardUntouchedBallotsAsync(countDb, electionId, currentCount, nextCount);

            // Insert candidate snapshot for nextCount (recompute totals/transfers from ballots)
            await InsertCandidateSnapshotAsync(countDb, electionId, currentCount, nextCount, excludedCandidateIds);
        }

        // Readable helper queries

        private static Task<List<int>> GetRunningCandidateIdsAsync(CountContext countDb,int electionId,int currentCount,HashSet<int> excludedCandidateIds)
        {
            return countDb.Candidates
                .AsNoTracking()
                .Where(c =>
                    c.ElectionId == electionId &&
                    c.CountNumber == currentCount &&
                    c.Status == (int)CandidateStatus.Running &&
                    !excludedCandidateIds.Contains(c.CandidateId))
                .Select(c => c.CandidateId)
                .ToListAsync();
        }

        private sealed record ExcludedBallot(long RandomBallotId, int CurrentPreference);

        private static Task<List<ExcludedBallot>> GetBallotsHeldByExcludedCandidatesAsync(CountContext countDb,int electionId,int currentCount,HashSet<int> excludedCandidateIdsInt)
        {
            return countDb.BallotCurrentStates
                .AsNoTracking()
                .Where(b =>
                    b.ElectionId == electionId &&
                    b.CountNumber == currentCount &&
                    b.CurrentCandidateId != null &&
                    excludedCandidateIdsInt.Contains(b.CurrentCandidateId.Value) &&
                    !b.IsExhausted)
                .Select(b => new ExcludedBallot(b.RandomBallotId, b.CurrentPreference))
                .ToListAsync();
        }

        private sealed record BallotPref(long RandomBallotId, int CandidateId, int Preference);

        private static async Task<Dictionary<long, List<BallotPref>>> LoadPreferencesByBallotAsync(RawBallotDbContext rawDb,List<long> ballotIds,int electionId)
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

        private static List<BallotCurrentState> BuildNextBallotStateRows(int electionId,int nextCount,List<ExcludedBallot> excludedBallots,Dictionary<long, List<BallotPref>> prefsByBallot,HashSet<int> runningCandidateIds)
        {
            var nextRows = new List<BallotCurrentState>(excludedBallots.Count);

            foreach (var ballot in excludedBallots)
            {
                if (!prefsByBallot.TryGetValue(ballot.RandomBallotId, out var preferences))
                {
                    nextRows.Add(CreateExhaustedBallotRow(electionId, nextCount, ballot.RandomBallotId, ballot.CurrentPreference));
                    continue;
                }

                var nextValidPref = preferences.FirstOrDefault(p =>
                    p.Preference > ballot.CurrentPreference &&
                    runningCandidateIds.Contains(p.CandidateId));

                nextRows.Add(
                    nextValidPref is null
                        ? CreateExhaustedBallotRow(electionId, nextCount, ballot.RandomBallotId, ballot.CurrentPreference)
                        : CreateTransferredBallotRow(electionId, nextCount, ballot.RandomBallotId, nextValidPref.CandidateId, nextValidPref.Preference)
                );
            }

            return nextRows;
        }

        private static BallotCurrentState CreateExhaustedBallotRow(int electionId,int nextCount,long ballotId,int currentPreference)
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

        private static BallotCurrentState CreateTransferredBallotRow(int electionId,int nextCount,long ballotId,long nextCandidateId,int nextPreference)
        {
            return new BallotCurrentState
            {
                ElectionId = electionId,
                CountNumber = nextCount,
                RandomBallotId = ballotId,
                CurrentCandidateId = (int?)nextCandidateId,
                CurrentPreference = nextPreference,
                IsExhausted = false,
                LatestCountTransfered = nextCount
            };
        }

        // ---------------------------
        // SQL: readable & still fast
        // ---------------------------

        private static Task CarryForwardUntouchedBallotsAsync(CountContext countDb,int electionId,int currentCount,int nextCount)
        {
            // Insert all current-count ballots into next count except those already inserted (transferred ballots).
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

        private static Task InsertCandidateSnapshotAsync(CountContext countDb,int electionId,int currentCount,int nextCount,HashSet<int> excludedCandidateIds)
        {
            var excludedValuesSql = BuildExcludedValuesSql(excludedCandidateIds);
            // The SQL builds three CTEs: Excluded, Totals, Transfers and then inserts into CandidateCountStates by joining them. 
            var sql = $@"
            WITH Excluded(CandidateId) AS (
                {excludedValuesSql}
            ),
            Totals AS (
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
                CASE WHEN ex.CandidateId IS NOT NULL THEN {(int)CandidateStatus.Excluded} ELSE prev.Status END AS Status,
                prev.Surplus,
                @nextCount AS CountNumber,
                ISNULL(t.TotalVotes, 0) AS TotalVotes,
                NULLIF(ISNULL(tr.TransferredVotes, 0), 0) AS BallotsTransferredAmount
            FROM CandidateCountStates prev
            LEFT JOIN Totals    t  ON t.CandidateId  = prev.CandidateId
            LEFT JOIN Transfers tr ON tr.CandidateId = prev.CandidateId
            LEFT JOIN Excluded  ex ON ex.CandidateId = prev.CandidateId
            WHERE prev.ElectionId = @electionId
              AND prev.CountNumber = @currentCount;
            ";

            return countDb.Database.ExecuteSqlRawAsync(
                sql,
                new SqlParameter("@electionId", electionId),
                new SqlParameter("@nextCount", nextCount),
                new SqlParameter("@currentCount", currentCount)
            );
        }

        private static string BuildExcludedValuesSql(HashSet<int> excludedCandidateIds)
        {
            // Produces either:
            //   SELECT CAST(NULL AS BIGINT) WHERE 1=0
            // or:
            //   SELECT v.CandidateId FROM (VALUES (1),(2),(3)) AS v(CandidateId)

            if (excludedCandidateIds.Count == 0)
                return "SELECT CAST(NULL AS BIGINT) AS CandidateId WHERE 1=0";

            var values = string.Join(",", excludedCandidateIds.Select(id => $"({id})"));
            return $"SELECT v.CandidateId FROM (VALUES {values}) AS v(CandidateId)";
        }
    }
}
