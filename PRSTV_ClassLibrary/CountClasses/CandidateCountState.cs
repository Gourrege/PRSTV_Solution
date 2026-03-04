using PRSTV_ClassLibrary.CountClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRSTV_ConsoleApp.LocalDB
{
    public enum CandidateStatus
    {
        Running = 0,
        Elected = 1,
        Excluded = 2
    }
    public class CandidateCountState
    {
        public int CandidateCountStateId { get; set; }
        public int CandidateId { get; set; }

        public int Status { get; set; }
        public int Surplus { get; set; }
        public int TotalVotes { get; set; }

        public string? Name { get; set; } // optional convenience
        public int? BallotsTransferredAmount { get; set; }
        public int CountNumber { get; set; }
        public int ElectionId { get; set; }

        public Election? Election { get; set; }        // navigation (recommended)
    }
}
