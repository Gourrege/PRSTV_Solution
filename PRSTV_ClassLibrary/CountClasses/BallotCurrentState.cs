using PRSTV_ClassLibrary.CountClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRSTV_ConsoleApp
{
    public class BallotCurrentState
    {
        public int BallotCurrentStateId { get; set; } 
        public int CountNumber { get; set; }
        public long RandomBallotId { get; set; }
        public int? CurrentCandidateId { get; set; } // null = exhausted/non-transferable
        public int CurrentPreference { get; set; }   // preference number currently sitting at
        public bool IsExhausted { get; set; }
        public int LatestCountTransfered { get; set; }
        public int ElectionId { get; set; }
        public Election? Election { get; set; }
    }
}
    