using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;
namespace PRSTV_ClassLibrary.SupabaseTbls
{
    [Table("BallotPaperTBL")]
    public class BallotPaperEF
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }  // int8

        [Column("box_location")]
        public string BoxLocation { get; set; } = string.Empty;

        [Column("image_url")]
        public string? ImageUrl { get; set; }

        [Column("random_ballot_id")]
        public long RandomBallotId { get; set; } // int8

        [Column("ballot_state", TypeName = "ballot_state")]
        public BALLOT_STATE BallotState { get; set; }
        [Column("election_id")]
        public int ElectionId { get; set; }
        // navigation (one ballot paper -> many preferences)
        public List<BallotPreferenceEF> Preferences { get; set; } = new();
    }
    public enum BALLOT_STATE
    {
        [PgName("Valid")]
        Valid,
        [PgName("Doubtful")]
        Doubtful,
        [PgName("Spoiled")]
        Spoiled
        
    }

}
