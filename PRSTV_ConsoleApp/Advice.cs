using PRSTV_ConsoleApp.Data;
using PRSTV_ConsoleApp.LocalDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PRSTV_ConsoleApp
{
   
    public class Advice
    {
        
        public ElectionState? ElectionState { get; }

        public CandidateCountState? CandidateWithHighestSurplus { get; private set; }


        public CandidateCountState? LowestRunningCandidate { get; private set; }

        // The “low group” (can be 1 or more candidates) computed safely
        public List<CandidateCountState> LowestRunningCandidates { get; private set; } = new();

        
        //public Advice(CountState countState)
        //    : this(countState, electionState: null)
        //{
        //}

        // New constructor used by the updated console logic
        public Advice(CountState countState, ElectionState? electionState)
        {
            if (countState == null) throw new ArgumentNullException(nameof(countState));
            ElectionState = electionState;

            // Highest surplus elected candidate (if any)
            if (countState.ElectedCandidatesWithSurplus != null &&
                countState.ElectedCandidatesWithSurplus.Count > 0)
            {
                CandidateWithHighestSurplus = countState.ElectedCandidatesWithSurplus
                    .OrderByDescending(c => c.Surplus)
                    .First();
            }

            // Lowest running candidate + low group
            if (countState.RunningCandidates != null && countState.RunningCandidates.Count > 0)
            {
                var orderedRunning = countState.RunningCandidates
                    .OrderBy(c => c.TotalVotes)
                    .ToList();

                LowestRunningCandidate = orderedRunning.First();

                // If we don't have election state, we can't resolve ties using earlier counts.
                // In that case, fall back to your low group logic.
                if (ElectionState == null || ElectionState.Election == null)
                {
                    LowestRunningCandidates = GetLowGroupCandidates(countState);
                    LowestRunningCandidate = LowestRunningCandidates.OrderBy(c => c.TotalVotes).FirstOrDefault();
                    return;
                }

                // Tie handling for lowest
                var tiedCandidates = GetTiedCandidates(LowestRunningCandidate, orderedRunning);

                if (tiedCandidates.Count > 1)
                {
                    using var dbCount = new CountContext();

                    if (ElectionState.Election.CurrentCount <= 1)
                    {
                        // First count: random tie-break (your current rule)
                        HandleTieForLowestCandidate(tiedCandidates);
                    }
                    else
                    {
                        tiedCandidates = ResolveTieUsingEarlierCounts(dbCount, tiedCandidates);
                    }

                    // Ensure both are set consistently after tie logic
                    LowestRunningCandidates = tiedCandidates;
                    LowestRunningCandidate = tiedCandidates
                        .OrderBy(c => c.TotalVotes)
                        .FirstOrDefault();
                }
                else
                {
                    LowestRunningCandidates = GetLowGroupCandidates(countState);
                    LowestRunningCandidate = LowestRunningCandidates
                        .OrderBy(c => c.TotalVotes)
                        .FirstOrDefault();
                }
            }
        }

        private List<CandidateCountState> ResolveTieUsingEarlierCounts(
            CountContext dbCount,
            List<CandidateCountState> tiedCandidates)
        {
            int earliestCountNumber = 1;

            while (ElectionState != null &&
                   ElectionState.Election != null &&
                   earliestCountNumber < ElectionState.Election.CurrentCount)
            {
                var candidatesVotesInEarliestCount = dbCount.Candidates
                    .Where(x => x.CountNumber == earliestCountNumber)
                    .ToList();

                var tiedVotes = candidatesVotesInEarliestCount
                    .Where(c => tiedCandidates.Select(t => t.CandidateId).Contains(c.CandidateId))
                    .ToList();

                if (tiedVotes.Count == 0)
                {
                    // No data for that count; move forward.
                    earliestCountNumber++;
                    continue;
                }

                int minVotes = tiedVotes.Min(tc => tc.TotalVotes);
                tiedCandidates = tiedVotes.Where(c => c.TotalVotes == minVotes).ToList();

                if (tiedCandidates.Count == 1)
                {
                    // Tie broken
                    return tiedCandidates;
                }

                // Still tied -> check next count
                earliestCountNumber++;
            }

            // Still tied after checking earlier counts -> random draw (fallback)
            return HandleTieForLowestCandidate(tiedCandidates);
        }

        // Returns the "remaining low group" after excluding one randomly (your original approach),
        // and returns that group so the caller can set properties consistently.
        private List<CandidateCountState> HandleTieForLowestCandidate(List<CandidateCountState> tiedCandidates)
        {
            if (tiedCandidates == null || tiedCandidates.Count == 0)
                return new List<CandidateCountState>();

            if (tiedCandidates.Count == 1)
                return tiedCandidates.ToList();

            var random = new Random();
            int indexToExclude = random.Next(tiedCandidates.Count);

            return tiedCandidates
                .Where((c, index) => index != indexToExclude)
                .ToList();
        }

        private List<CandidateCountState> GetTiedCandidates(
            CandidateCountState candidate,
            List<CandidateCountState> runningCandidates)
        {
            return runningCandidates
                .Where(c => c.TotalVotes == candidate.TotalVotes)
                .ToList();
        }

        /// <summary>
        /// Returns true if the best-placed running candidate could reach quota
        /// after distributing *all* currently available surplus votes (upper bound check).
        /// </summary>
        public bool CanCandidateBeElectedWithSurplus(CountState countState, int quota)
        {
            if (countState == null) throw new ArgumentNullException(nameof(countState));
            if (quota <= 0) throw new ArgumentOutOfRangeException(nameof(quota), "Quota must be > 0.");

            var running = countState.RunningCandidates ?? new List<CandidateCountState>();
            if (running.Count == 0) return false;

            int highestTotalVotes = running.Max(c => c.TotalVotes);

            int totalSurplusVotes = (countState.ElectedCandidatesWithSurplus ?? new List<CandidateCountState>())
                .Sum(c => c.Surplus);

            return (highestTotalVotes + totalSurplusVotes) >= quota;
        }

        /// <summary>
        /// Attempts an "escape exclusion" upper-bound test:
        /// Can the current lowest *group* potentially overtake the next candidate above them,
        /// assuming they received all surplus?
        /// </summary>
        public bool CanLowGroupEscapeBySurplus(CountState countState)
        {
            if (countState == null) throw new ArgumentNullException(nameof(countState));

            var running = countState.RunningCandidates ?? new List<CandidateCountState>();
            if (running.Count < 2) return false;

            var ordered = running.OrderBy(c => c.TotalVotes).ToList();

            int totalSurplusVotes = (countState.ElectedCandidatesWithSurplus ?? new List<CandidateCountState>())
                .Sum(c => c.Surplus);

            var lowGroup = GetLowGroupCandidates(countState);
            if (lowGroup.Count == 0) return false;

            int nextAboveIndex = lowGroup.Count;
            if (nextAboveIndex >= ordered.Count) return false;

            int highestVoteInLowGroupVotes = lowGroup.Max(c => c.TotalVotes);

            return (highestVoteInLowGroupVotes + totalSurplusVotes) > ordered[nextAboveIndex].TotalVotes;
        }

        /// <summary>
        /// Builds a set of candidates starting from the lowest, adding more low candidates
        /// while the low-side (sum of included lows + next low + surplus) is still < next-above candidate.
        /// </summary>
        private List<CandidateCountState> GetLowGroupCandidates(CountState countState)
        {
            var running = countState.RunningCandidates ?? new List<CandidateCountState>();
            if (running.Count == 0) return new List<CandidateCountState>();

            var ordered = running.OrderBy(c => c.TotalVotes).ToList();

            int totalSurplusVotes = (countState.ElectedCandidatesWithSurplus ?? new List<CandidateCountState>())
                .Sum(c => c.Surplus);

            var lowGroup = new List<CandidateCountState> { ordered[0] };

            int nextLowIndex = 1;
            int nextAboveIndex = 2;

            while (nextAboveIndex < ordered.Count)
            {
                int sumIfWeIncludeNextLow =
                    lowGroup.Sum(c => c.TotalVotes) +
                    ordered[nextLowIndex].TotalVotes +
                    totalSurplusVotes;

                int nextAboveVotes = ordered[nextAboveIndex].TotalVotes;

                if (sumIfWeIncludeNextLow < nextAboveVotes)
                {
                    lowGroup.Add(ordered[nextLowIndex]);
                    nextLowIndex++;
                    nextAboveIndex++;

                    if (nextLowIndex >= ordered.Count) break;
                }
                else
                {
                    break;
                }
            }

            return lowGroup;
        }
    }
}
