using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Utilities;
using System.IO;
using System.Collections;

namespace eforms_middleware.DataModel
{
    public class MobilePhoneAndSIMCardRequest
    {
        //-------------------start of JSON data to be stored in the JSON Response column in the Form_Info table----------------

        //DeviceType
        [JsonProperty("DeviceType")]
        public string DeviceType { get; set; } = "";
                
        //ExistingMobileNumber
        [JsonProperty("ExistingMobileNumber")]
        public string ExistingMobileNumber { get; set; } = "";

        //CurrentMobileNumber
        [JsonProperty("CurrentMobileNumber")]
        public string CurrentMobileNumber { get; set; } = "";

        //SpecialArrangements
        [JsonProperty("SpecialArrangements")]
        public string SpecialArrangements { get; set; } = "";

        //Justification
        [JsonProperty("Justification")]
        public string Justification { get; set; } = "";

        //Fund
        [JsonProperty("Fund")]
        public string Fund { get; set; } = "";

        //CostCentre
        [JsonProperty("CostCentre")]
        public string CostCentre { get; set; } = "";

        //Account
        [JsonProperty("Account")]
        public string Account { get; set; } = "";

        //Location
        [JsonProperty("Location")]
        public string Location { get; set; } = "";

        //Activity
        [JsonProperty("Activity")]
        public string Activity { get; set; } = "";

        //Project
        [JsonProperty("Project")]
        public string Project { get; set; } = "";

        //PhoneModel
        [JsonProperty("PhoneModel")]
        public string PhoneModel { get; set; } = "";

       
        //DeviceAmount
        [JsonProperty("DeviceAmount")]
        public string DeviceAmount { get; set; } = "";

        //AssetNumber
        [JsonProperty("AssetNumber")]
        public string AssetNumber { get; set; } = "";

        //IMEI
        [JsonProperty("IMEI")]
        public string IMEI { get; set; } = "";

        //OrderNumber
        [JsonProperty("OrderNumber")]
        public string OrderNumber { get; set; } = "";

        //Supplier
        [JsonProperty("Supplier")]
        public string Supplier { get; set; } = "";

        //InvoiceDate
        [JsonProperty("InvoiceDate")]
        public string InvoiceDate { get; set; } = "";

        //InvoiceNumber
        [JsonProperty("InvoiceNumber")]
        public string InvoiceNumber { get; set; } = "";


        //TeleTeamComments
        [JsonProperty("TeleTeamComments")]
        public string TeleTeamComments { get; set; } = "";

        //ServiceDeskTeamEmailTemplate - this value will be obtained from the Email_Info table
        [JsonProperty("ServiceDeskTeamEmailTemplate")]
        public string ServiceDeskTeamEmailTemplate { get; set; } = "";


        [JsonProperty("IsRaisedOnBehalfOfEmployee")]
        [JsonConverter(typeof(StringEnumConverter))]
        public bool? IsRaisedOnBehalfOfEmployee { get; set; }


        //-------------------end of JSON data to be stored in the JSON Response column in the Form_Info table----------------



        //-----------------Other values captured in the form amd stored in the corresponding columns in the Form_Info table-------------------------------

        //FormOwnerName
        [JsonProperty("FormOwnerName")]
        public string FormOwnerName { get; set; } = "";

        //FormOwnerEmployeeNo
        [JsonProperty("FormOwnerEmployeeNo")]
        public string FormOwnerEmployeeNo { get; set; } = "";

        //FormOwnerPositionTitle
        [JsonProperty("FormOwnerPositionTitle")]
        public string FormOwnerPositionTitle { get; set; } = "";

        //FormOwnerPositionNumber
        [JsonProperty("FormOwnerPositionNumber")]
        public string FormOwnerPositionNumber { get; set; } = "";

        //FormOwnerOrganisationUnit
        [JsonProperty("FormOwnerOrganisationUnit")]
        public string FormOwnerOrganisationUnit { get; set; } = "";

        //FormOwnerEmailAddress 
        [JsonProperty("FormOwnerEmailAddress")]
        public string FormOwnerEmailAddress { get; set; } = "";

        //Employee
        [JsonProperty("Employee")]
        public string Employee { get; set; } = "";

        //EmployeeDepartment
        [JsonProperty("EmployeeDepartment")]
        public string EmployeeDepartment { get; set; } = "";

        //EmployeeManagerPositionNumber
        [JsonProperty("EmployeeManagerPositionNumber")]
        public string EmployeeManagerPositionNumber { get; set; } = "";

        //EmployeeName
        [JsonProperty("EmployeeName")]
        public string EmployeeName { get; set; } = "";

        //EmployeeNumber
        [JsonProperty("EmployeeNumber")]
        public string EmployeeNumber { get; set; } = "";

        //EmployeeOrganisationUnit
        [JsonProperty("EmployeeOrganisationUnit")]
        public string EmployeeOrganisationUnit { get; set; } = "";

        //EmployeePositionNumber
        [JsonProperty("EmployeePositionNumber")]
        public string EmployeePositionNumber { get; set; } = "";

        //EmployeePositionTitle
        [JsonProperty("EmployeePositionTitle")]
        public string EmployeePositionTitle { get; set; } = "";


        //Approved Director
        [JsonProperty("ApprovedDirector")]
        public string ApprovedDirector { get; set; } = "";

        //Approved Manager
        [JsonProperty("ApprovedManager")]
        public string ApprovedManager { get; set; } = "";

        //CompletedDate
        [JsonProperty("CompletedDate")]
        public DateTime? CompletedDate { get; set; }

        //SubmittedDate
        [JsonProperty("SubmittedDate")]
        public DateTime? SubmittedDate { get; set; }

        //Created
        [JsonProperty("Created")]        
        public DateTime? Created { get; set; }

        //CreatedBy
        [JsonProperty("CreatedBy")]
        public string CreatedBy { get; set; } = "";

        [JsonProperty("Modified")]
        public DateTime? Modified { get; set; }

        //ModifiedBy
        [JsonProperty("ModifiedBy")]
        public string ModifiedBy { get; set; } = "";

        //FormHistory
        [JsonProperty("FormHistory")]
        public string FormHistory { get; set; } = "";

        //NextApprovalLevel
        [JsonProperty("NextApprovalLevel")]
        public string NextApprovalLevel { get; set; } = "";


        //FormStatusID
        [JsonProperty("FormStatusID")]
        public string FormStatusID { get; set; } = "";


        //FormItemID
        [JsonProperty("FormItemID")]
        public int? FormItemID { get; set; }
               
                
        public class LookupValueConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                JToken t = JToken.FromObject(value);
                var v = t.Value<int?>("LookupId") ?? 0;
                writer.WriteValue(v);
            }

            public override object ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return string.Empty;
                }
                else if (reader.TokenType == JsonToken.Integer)
                {
                    return serializer.Deserialize(reader, objectType);
                }
                else
                {
                    JObject jo = JObject.Load(reader);
                    var v = jo.Value<int?>("LookupId") ?? 0;
                    return v;
                }
            }

            public override bool CanConvert(Type objectType)
            {
                return true;
            }

            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanWrite
            {
                get { return false; }
            }

        }
    }
}
