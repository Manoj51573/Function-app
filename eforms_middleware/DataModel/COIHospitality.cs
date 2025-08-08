using Newtonsoft.Json;
using eforms_middleware.InternalSystem;

namespace eforms_middleware.DataModel
{
    public class COIHospitality
    {
        [JsonProperty("RowID")]
        [IgnoreField]
        public int? RowID { get; set; } = null;

        [JsonProperty("FormID")]
        [IgnoreField]
        public int? FormID { get; set; } = null;

        [JsonProperty("conflictOfInterestRequestType")]
        public string conflictOfInterestRequestType { get; set; } = "";

        [JsonProperty("dateOfOffer")]
        public string dateOfOffer { get; set; } = "";

        [JsonProperty("orgName")]
        public string orgName { get; set; } = "";

        [JsonProperty("firstName")]
        public string firstName { get; set; } = "";

        [JsonProperty("lastName")]
        public string lastName { get; set; } = "";

        [JsonProperty("phone")]
        public string phone { get; set; } = "";

        [JsonProperty("email")]
        public string email { get; set; } = "";

        [JsonProperty("descriptionOfHospitality")]
        public string descriptionOfHospitality { get; set; } = "";

        [JsonProperty("relationship")]
        public string relationship { get; set; } = "";

        [JsonProperty("relationshipOther")]
        public string relationshipOther { get; set; } = "";

        [JsonProperty("offerAccepted")]
        public string offerAccepted { get; set; } = "";

        [JsonProperty("totalEstimatedValue")]
        public string totalEstimatedValue { get; set; } = "";

        [JsonProperty("typeOfConflict")]
        public string typeOfConflict { get; set; } = "";

        [JsonProperty("descriptionOfConflict")]
        public string descriptionOfConflict { get; set; } = "";

        [JsonProperty("additionalEmployees")]
        public string additionalEmployees { get; set; } = "";
    }
}
