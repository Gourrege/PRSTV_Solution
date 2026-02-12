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
    [Table("CandidateTBL")]
    public class Candidate : BaseModel
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("first_name")]
        public string FirstName { get; set; }
        [Column("last_name")]
        public string LastName { get; set; }
        public List<BallotPreference> BallotPreference { get; set; }
    }
}
