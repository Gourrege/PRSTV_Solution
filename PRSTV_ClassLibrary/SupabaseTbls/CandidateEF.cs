using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace PRSTV_ClassLibrary.SupabaseTbls
{
    [Table("CandidateTBL")]
    public class CandidateEF
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("first_name")]
        public string? FirstName { get; set; } = string.Empty;
        [Column("last_name")]
        public string? LastName { get; set; } = string.Empty;
        [Column("election_id")]
        public int ElectionId { get; set; }
        public ICollection<BallotPreferenceEF> Preferences { get; set; }
             = new List<BallotPreferenceEF>();
    }
}
