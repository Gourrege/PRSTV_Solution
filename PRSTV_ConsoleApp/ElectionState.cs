using Microsoft.EntityFrameworkCore;
using PRSTV_ClassLibrary.SupabaseTbls;
using PRSTV_ConsoleApp.Data;
using PRSTV_ConsoleApp.LocalDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PRSTV_ClassLibrary.CountClasses;
namespace PRSTV_ConsoleApp
{
    

    public class ElectionState
    {
        public Election Election { get; set; }
        private ElectionState() { }

        // WPF uses explicit New/Resume
        public static async Task<ElectionState> CreateNewAsync(CountContext dbCount, RawBallotDbContext rawDb, int seats)
        {
            var instance = new ElectionState();
            instance.Election = await CreateElectionAsync(dbCount, rawDb, seats);
            return instance;
        }

        /// <summary>
        /// Starts a new election count for a specific raw ballot election id.
        /// This is used by the Solution-master "Select Election" decision flow port:
        /// if the count has not started but there are no doubtful ballots remaining,
        /// we create the Elections row and seed Count 1 using the existing pipeline.
        /// </summary>
        public static async Task<ElectionState> CreateNewForElectionIdAsync(CountContext dbCount, RawBallotDbContext rawDb, int seats, int electionId)
        {
            var instance = new ElectionState();
            instance.Election = await CreateElectionAsync(dbCount, rawDb, seats, electionId);
            await EnsureCount1SeededAsync(dbCount, rawDb, instance.Election.ElectionId);
            return instance;
        }

        // NEW: returns null if none
        public static async Task<ElectionState?> ResumeLatestUnfinishedAsync(CountContext dbCount, RawBallotDbContext rawDb)
        {
            var existing = await dbCount.Elections
                .OrderByDescending(e => e.ElectionId)
                .FirstOrDefaultAsync(e => e.SeatsFilled < e.Seats);

            if (existing is null)
                return null;

            var instance = new ElectionState { Election = existing };

            // Safety: if resuming at count 1 ensure seeded
            await EnsureCountSeededAsync(dbCount, rawDb, existing.ElectionId, existing.CurrentCount);
            return instance;
        }
        public static async Task<ElectionState?> ResumeChosenAsync(CountContext dbCount, RawBallotDbContext rawDb, int ElectionID)
        {
            var existing = await dbCount.Elections
                .Where(e => e.ElectionId == ElectionID)
                .FirstOrDefaultAsync(e => e.SeatsFilled < e.Seats);

            if (existing is null)
                return null;

            var instance = new ElectionState { Election = existing };

            // Safety: if resuming at count 1 ensure seeded
            await EnsureCountSeededAsync(dbCount, rawDb, existing.ElectionId, existing.CurrentCount);
            return instance;
        }

        /// <summary>
        /// Loads an election by id regardless of whether it has finished.
        /// Used to allow viewing finished elections in the hub for enquiries.
        /// </summary>
        public static async Task<ElectionState?> LoadByIdAsync(CountContext dbCount, RawBallotDbContext rawDb, int electionId)
        {
            var existing = await dbCount.Elections
                .Where(e => e.ElectionId == electionId)
                .FirstOrDefaultAsync();

            if (existing is null)
                return null;

            var instance = new ElectionState { Election = existing };

            // Safety: if the election is at count 1 ensure seeded for enquiry pages.
            await EnsureCountSeededAsync(dbCount, rawDb, existing.ElectionId, existing.CurrentCount);
            return instance;
        }

        private static async Task EnsureCount1SeededAsync(CountContext dbCount, RawBallotDbContext rawDb, int electionId)
            => await EnsureCountSeededAsync(dbCount, rawDb, electionId, 1);

        private static async Task EnsureCountSeededAsync(CountContext dbCount, RawBallotDbContext rawDb, int electionId, int countNumber)
        {
            if (countNumber != 1) return;

            bool count1Exists = await dbCount.BallotCurrentStates
                .AsNoTracking()
                .AnyAsync(b => b.ElectionId == electionId && b.CountNumber == 1);

            if (!count1Exists)
                await SeedFirstPrefAsync(rawDb, dbCount, electionId);
        }

        // CHANGED: seats passed in, no Console prompt
        private static async Task<Election> CreateElectionAsync(CountContext dbCount, RawBallotDbContext rawDb, int seats)
        {
            
            var election = new Election
            {
                Seats = seats,
                CurrentCount = 1,
                SeatsFilled = 0,

                // Quota is calculated when the user explicitly starts the election.
                TotalValidPoll = 0,
                Quota = 0
            };

            dbCount.Elections.Add(election);
            await dbCount.SaveChangesAsync();

            return election;
        }

        /// <summary>
        /// Creates an Elections row using an explicit election id that matches the raw ballot store.
        /// If an Elections row already exists, it is returned.
        /// </summary>
        private static async Task<Election> CreateElectionAsync(CountContext dbCount, RawBallotDbContext rawDb, int seats, int electionId)
        {
            var existing = await dbCount.Elections.FirstOrDefaultAsync(e => e.ElectionId == electionId);
            if (existing is not null)
                return existing;

            var election = new Election
            {
                ElectionId = electionId,
                Seats = seats,
                // Quota is calculated when the user explicitly starts the election.
                TotalValidPoll = 0,
                Quota = 0,
                CurrentCount = 1,
                SeatsFilled = 0
            };

            dbCount.Elections.Add(election);
            await dbCount.SaveChangesAsync();
            return election;
        }

        private static async Task SeedFirstPrefAsync(RawBallotDbContext db, CountContext dbCount, int electionId)
        {
            var candidateStates = await (
                from p in db.BallotPreferences.AsNoTracking()
                join bp in db.BallotPapers.AsNoTracking() on p.RandomBallotId equals bp.RandomBallotId
                join c in db.Candidates.AsNoTracking() on p.CandidateId equals c.Id
                where p.Preference == 1 && bp.ElectionId == electionId && bp.BallotState == BALLOT_STATE.Valid
                group p by new { p.CandidateId, c.FirstName, c.LastName } into g
                select new CandidateCountState
                {
                    ElectionId = electionId,
                    CountNumber = 1,
                    CandidateId = g.Key.CandidateId,
                    Status = (int)CandidateStatus.Running,
                    Surplus = 0,
                    TotalVotes = g.Count(),
                    Name = g.Key.FirstName + " " + g.Key.LastName
                }
            ).ToListAsync();

            var ballotStates = await (
                from p in db.BallotPreferences.AsNoTracking()
                join bp in db.BallotPapers.AsNoTracking()
                    on p.RandomBallotId equals bp.RandomBallotId
                where p.Preference == 1 && bp.BallotState == BALLOT_STATE.Valid
                   && bp.ElectionId == electionId
                select new BallotCurrentState
                {
                    ElectionId = electionId,
                    CountNumber = 1,
                    RandomBallotId = p.RandomBallotId,
                    CurrentCandidateId = p.CandidateId,
                    CurrentPreference = 1,
                    LatestCountTransfered = 1,
                    IsExhausted = false
                }
            ).ToListAsync();

            await dbCount.Candidates.AddRangeAsync(candidateStates);
            await dbCount.BallotCurrentStates.AddRangeAsync(ballotStates);
            await dbCount.SaveChangesAsync();
        }

        private static async Task<int> GetTotalValidPollAsync(RawBallotDbContext db, int? electionId)
        {
            var q = db.BallotPapers.AsNoTracking().Where(b => b.BallotState == BALLOT_STATE.Valid);
            if (electionId.HasValue)
                q = q.Where(b => b.ElectionId == electionId.Value);
            return await q.CountAsync();
        }
    }

}
