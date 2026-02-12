using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NpgsqlTypes;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace PRSTV_WPF.Models
{
    [Table("BallotPaperTBL")]
    public class BallotPaper : BaseModel
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

        // Map DB enum column as STRING (this is what PostgREST wants)
        [Column("ballot_state")]
        public string BallotState { get; set; }
        
        public BALLOT_SATE BallotStateRaw {

            get => Enum.Parse<BALLOT_SATE>(BallotState, true);
            set => BallotState = value.ToString();
        
        }


        // navigation (one ballot paper -> many preferences)
        public List<BallotPreference> Preferences { get; set; } = new();
    }
    public enum BALLOT_SATE
    {
        [EnumMember(Value = "Valid")]
        Valid,
        [EnumMember(Value = "Spoiled")]
        Spoiled,
        [EnumMember(Value = "Doubtful")]
        Doubtful
    }

}
