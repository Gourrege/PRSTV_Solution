using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRSTV_WPF
{
    [Table("app_identity")]
    public class AppIdentity : BaseModel
    {
        [PrimaryKey("id", false)]
        public int Id { get; set; }

        [Column("environment_name")]
        public string EnvironmentName { get; set; } = "";

        [Column("project_ref")]
        public string ProjectRef { get; set; } = "";
    }
}
