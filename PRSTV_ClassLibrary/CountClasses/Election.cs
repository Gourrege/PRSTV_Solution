using PRSTV_ConsoleApp;
using PRSTV_ConsoleApp.LocalDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRSTV_ClassLibrary.CountClasses
{
    public class Election
    {
        public int ElectionId { get; set; }
        public int Seats { get; set; }
        public int Quota { get; set; }
        public int TotalValidPoll { get; set; }
        public int CurrentCount { get; set; }
        public int SeatsFilled { get; set; }
        public ICollection<CandidateCountState> CandidateStates { get; set; } = new List<CandidateCountState>();
        public ICollection<BallotCurrentState> BallotStates { get; set; } = new List<BallotCurrentState>();
    }
}
