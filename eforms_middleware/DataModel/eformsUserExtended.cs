using Newtonsoft.Json;
using eforms_middleware.InternalSystem;

namespace eforms_middleware.DataModel
{
    public class eformsUserExtended : EformsUser
    {

        [JsonProperty("RowID")]
        [IgnoreField]
        public int? RowID { get; set; } = null;

        [JsonProperty("AzureADObjectID")]
        public string AzureADObjectID { get; set; } = "";
    }
}
