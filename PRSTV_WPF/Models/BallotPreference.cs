using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRSTV_WPF.Models
{
    [Table("BallotPreferenceTBL")]
    public class BallotPreference : BaseModel
    {
        [Key]
        [Column("id")]
        public long Id { get; set; } // int8

        [Column("candidate_id")]
        public int CandidateId { get; set; }

        // Your column is text in the screenshot, but the values are numeric ("1","2","3"...).
        // Map it as int so ordering is easy.
        [Column("preference")]
        public int Preference { get; set; }

        [Column("random_ballot_id")]
        public long RandomBallotId { get; set; } // int8

        // navigation back to ballot paper (linked by random_ballot_id)
        public BallotPaper? BallotPaper { get; set; }

    }
}
