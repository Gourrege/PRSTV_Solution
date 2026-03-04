using Microsoft.EntityFrameworkCore;
using PRSTV_ClassLibrary.CountClasses;
using PRSTV_ConsoleApp.Data;
using PRSTV_ConsoleApp.LocalDB;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PRSTV_ConsoleApp
{
    public class CountState
    {
        public int SeatsRemaining { get; private set; }

        public List<CandidateCountState> ElectedCandidatesWithSurplus { get; private set; } = new();
        public List<CandidateCountState> RunningCandidates { get; private set; } = new();
        public List<CandidateCountState> RunningCandidatesClosestToQuota { get; private set; } = new();
        public List<CandidateCountState> StateOfPole { get; private set; } = new();

        private CountState() { }

        public static async Task<CountState> CreateAsync(ElectionState electionState)
        {
            var state = new CountState();
            await state.InitializeAsync(electionState);
            return state;
        }

        private async Task InitializeAsync(ElectionState electionState)
        {
            using var dbCount = new CountContext();


            int electionId = electionState.Election.ElectionId;
            int currentCount = electionState.Election.CurrentCount;

            // Load running candidates for this election + count
            RunningCandidates = await GetRunningCandidatesAsync(dbCount, electionId, currentCount);

            // Seats remaining comes from persisted election row
            SeatsRemaining = electionState.Election.Seats - electionState.Election.SeatsFilled;

            RunningCandidatesClosestToQuota = GetRunningCandidatesClosestToQuota();

            // elect if quota reached -> persist changes
            await FindAndElectCandidatesMeetingQuotaAsync(dbCount, electionState, electionId, currentCount);

            // After elections, reload lists (or recompute from tracked entities)
            ElectedCandidatesWithSurplus = await GetElectedCandidatesWithSurplusAsync(dbCount, electionId, currentCount);

            StateOfPole = await GetStateOfPoleAsync(dbCount, electionId, currentCount);
        }

        private static async Task<List<CandidateCountState>> GetRunningCandidatesAsync(CountContext dbCount, int electionId, int currentCount)
        {
            return await dbCount.Candidates
                .Where(c => c.ElectionId == electionId
                         && c.CountNumber == currentCount
                         && c.Status == (int)CandidateStatus.Running)
                .ToListAsync();
        }

        private List<CandidateCountState> GetRunningCandidatesClosestToQuota()
        {
            return RunningCandidates
                .OrderByDescending(c => c.TotalVotes)
                .Take(SeatsRemaining)
                .ToList();
        }

        public async Task FindAndElectCandidatesMeetingQuotaAsync(CountContext dbCount,ElectionState electionState,int electionId,int currentCount)
        {
            // quota now comes from election row
            int quota = electionState.Election.Quota;
            // Refreshes the running candidates to ensure we have the latest values before checking for quota (in case of changes from previous loop)
            RunningCandidates = await GetRunningCandidatesAsync(dbCount, electionId, currentCount);
            RunningCandidatesClosestToQuota = GetRunningCandidatesClosestToQuota();

            var candidatesMeetingQuota = RunningCandidatesClosestToQuota
                .Where(c => c.TotalVotes >= quota)
                .OrderByDescending(c => c.TotalVotes)
                .ToList();

            if (candidatesMeetingQuota.Count == 0)
                return;

            // If too many meet quota, only elect up to seats remaining
            candidatesMeetingQuota = candidatesMeetingQuota
                .Take(SeatsRemaining)
                .ToList();

            foreach (var candidate in candidatesMeetingQuota)
            {
                candidate.Status = (int)CandidateStatus.Elected;
                candidate.Surplus = candidate.TotalVotes - quota;

                // Persist election seats filled
                
                electionState.Election.SeatsFilled++;
            }

            // Persist both candidate changes + election changes
            dbCount.Elections.Update(electionState.Election);
            await dbCount.SaveChangesAsync();
            
        }

        private static async Task<List<CandidateCountState>> GetElectedCandidatesWithSurplusAsync(CountContext dbCount, int electionId, int currentCount)
        {
            return await dbCount.Candidates
                .Where(c => c.ElectionId == electionId
                         && c.CountNumber == currentCount
                         && c.Status == (int)CandidateStatus.Elected
                         && c.Surplus > 0)
                .ToListAsync();
        }

        private static async Task<List<CandidateCountState>> GetStateOfPoleAsync(CountContext dbCount, int electionId, int currentCount)
        {
            return dbCount.Candidates
                .Where(c => c.ElectionId == electionId && c.CountNumber == currentCount)
                .OrderByDescending(c => c.TotalVotes)
                .ToList();
        }
    }
}
