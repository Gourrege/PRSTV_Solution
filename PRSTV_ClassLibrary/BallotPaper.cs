using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PRSTV_ClassLibrary
{
    [Table("BallotPaperTBL")]
    public class BallotPaper
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

        [Column("is_spoiled")]
        public bool IsSpoiled { get; set; }

        [Column("is_doubtful")]
        public bool IsDoubtful { get; set; }

        // navigation (one ballot paper -> many preferences)
        public List<BallotPreference> Preferences { get; set; } = new();
    }

}
