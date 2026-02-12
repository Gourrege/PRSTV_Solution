using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace PRSTV_ClassLibrary
{
    [Table("BallotPreferenceTBL")]
    public class BallotPreference
    {
        [Key]
        [Column("id")]
        public long Id { get; set; } // int8

        [Column("candidate_name")]
        public string CandidateName { get; set; } = string.Empty;

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
