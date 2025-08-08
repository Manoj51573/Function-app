using Newtonsoft.Json;

namespace eforms_middleware.DataModel
{
    public class EformsUser
    {
        

        [JsonProperty("EmployeeNumber")]
        public string EmployeeNumber { get; set; } = "";

        [JsonProperty("EmployeeSurname")]
        public string EmployeeSurname { get; set; } = "";

        [JsonProperty("EmployeeFirstName")]
        public string EmployeeFirstName { get; set; } = "";

        [JsonProperty("EmployeeMiddleName")]
        public string EmployeeMiddleName { get; set; } = "";

        [JsonProperty("EmployeePreferredName")]
        public string EmployeePreferredName { get; set; } = "";

        [JsonProperty("EmployeeTitle")]
        public string EmployeeTitle { get; set; } = "";

        [JsonProperty("EmployeeGender")]
        public string EmployeeGender { get; set; } = "";

        [JsonProperty("EmployeeStartDate")]
        public string EmployeeStartDate { get; set; } = "";

        [JsonProperty("EmployeeTerminationDate")]
        public string EmployeeTerminationDate { get; set; } = "";

        [JsonProperty("EmployeeEmail")]
        public string EmployeeEmail { get; set; } = "";

        [JsonProperty("EmployeePositionTitle")]
        public string EmployeePositionTitle { get; set; } = "";

        [JsonProperty("EmployeeCostCentre")]
        public string EmployeeCostCentre { get; set; } = "";

        [JsonProperty("EmployeePositionNumber")]
        public string EmployeePositionNumber { get; set; } = "";

        [JsonProperty("ManagerName")]
        public string ManagerName { get; set; } = "";

        [JsonProperty("EmployeeUserId")]
        public string EmployeeUserId { get; set; } = "";

        [JsonProperty("EmployeeLocationCode")]
        public string EmployeeLocationCode { get; set; } = "";

        [JsonProperty("EmployeeLeaveFlag")]
        public string EmployeeLeaveFlag { get; set; } = "";

        [JsonProperty("ReportsToPositionNumber")]
        public string ReportsToPositionNumber { get; set; } = "";

        [JsonProperty("EmployeeDepartment")]
        public string EmployeeDepartment { get; set; } = "";

        [JsonProperty("EmployeeDirectorate")]
        public string EmployeeDirectorate { get; set; } = "";


        [JsonProperty("EmployeeUnit")]
        public string EmployeeUnit { get; set; } = "";


        [JsonProperty("EmployeeOccupancyType")]
        public string EmployeeOccupancyType { get; set; } = "";

        [JsonProperty("EmployeeClassificationLevel")]
        public string EmployeeClassificationLevel { get; set; } = "";

        [JsonProperty("EmployeePositionStatus")]
        public string EmployeePositionStatus { get; set; } = "";

        [JsonProperty("EmployeeManagementTier")]
        public string EmployeeManagementTier { get; set; } = "";

        [JsonProperty("EmployeePositionFTE")]
        public string EmployeePositionFTE { get; set; } = "";

    }
}
