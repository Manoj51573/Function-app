using eforms_middleware.Constants;
using System;
using System.Text.RegularExpressions;

namespace eforms_middleware.Settings
{
    public static class Helper
    {
        public static string BaseEformsURL => Environment.GetEnvironmentVariable("BaseEformsUrl");
        public static DomainType CurrentDomain => Enum.Parse<DomainType>(Environment.GetEnvironmentVariable("environment", EnvironmentVariableTarget.Process).ToUpper());

        public static bool IsImpersonationAllowed => Convert.ToBoolean(Environment.GetEnvironmentVariable("IsImpersonationAllowed", EnvironmentVariableTarget.Process));

        public static string FromEmail =>
            Environment.GetEnvironmentVariable("FromEmail", EnvironmentVariableTarget.Process);

        public static string SMTPHost => Environment.GetEnvironmentVariable("SMTPHost", EnvironmentVariableTarget.Process);

        public static string TrelisAccessManagementEmail => Environment.GetEnvironmentVariable("TrelisAccessManagementEmail", EnvironmentVariableTarget.Process);

        public static string WAStandardTime => "W. Australia Standard Time";    

        public static bool IsTier3(this string managementTier) => managementTier.Trim() == "3";

        public static bool IsTier2(this string managementTier) => managementTier.Trim() == "2";

        public static bool IsTier1(this string managementTier) => managementTier.Trim() == "1";

        public static bool IsTier3OrAbove(this string managementTier) => managementTier.IsTier3() || managementTier.IsTier2() || managementTier.IsTier1();

        public static bool IsNullOrZero(this int? input) => input == null || input == 0;

        public static int GetNumberFromString(this string value)
        {
            int result = 0;
            try
            {
                result = Convert.ToInt32(Regex.Match(value, @"\d+").Value);
            }
            catch
            {

            }
            return result;
        }

        public enum DomainType
        {
            DEV = 1,
            TST, //SIT 
            UAT,
            PRD
        }

        public enum EmployeeTitle
        {
            Tier1 = 1,
            Tier2 = 2,
            Tier3 = 3,
            Requestor,
            RequestorManager,
            ManagingDirector,
            DirectorGeneral,
            ExecutiveDirector,
            GovAuditGroup,
            ExecutiveDirectorODG,
            Delegate,
            Delegated,
            IndependentReview,
            None = 0
        }

        public enum COIFormType
        {
            Recruitment,
            GiftBenefitHospitality,
            SecondaryEmployment,
            CPR,
            GovernmentBoard,
            Grants,
            Other,
            None,
            COI,
            E29,
            ACR,
            GBC
        }

        public static string GetFormStatusTitle(this FormStatus formStatus)
        {
            var result = string.Empty;
            switch (formStatus)
            {
                case FormStatus.IndependentReviewApproved:
                    result = "Independent Review Approved";
                    break;
                case FormStatus.DelegationApproved:
                    result = "DelegationApproved";
                    break;
                case FormStatus.SubmittedAndEndorsed:
                    result = "Submitted and Endorsed";
                    break;
            }
            return result;
        }

        public static string GetEmployeeTitle(this EmployeeTitle employeTitle)
        {
            var result = string.Empty;
            switch (employeTitle)
            {
                case EmployeeTitle.RequestorManager:
                    result = "Requestor Manager";
                    break;
                case EmployeeTitle.ManagingDirector:
                    result = "Managing Director";
                    break;
                case EmployeeTitle.DirectorGeneral:
                    result = "Director General";
                    break;
                case EmployeeTitle.ExecutiveDirector:
                    result = "Executive Director";
                    break;
                case EmployeeTitle.Tier3:
                    result = "Tier 3";
                    break;
                case EmployeeTitle.Tier2:
                    result = "Tier 2";
                    break;
                case EmployeeTitle.Tier1:
                    result = "Tier 1";
                    break;
                case EmployeeTitle.GovAuditGroup:
                    result = "Gov & Audit Group";
                    break;
                case EmployeeTitle.Requestor:
                    result = "Requestor";
                    break;
                case EmployeeTitle.ExecutiveDirectorODG:
                    result = "Executive Director Office of the Director General";
                    break;
                case EmployeeTitle.Delegate:
                    result = "Delegate";
                    break;
                case EmployeeTitle.Delegated:
                    result = "Delegated";
                    break;
                case EmployeeTitle.IndependentReview:
                    result = "Independent Review";
                    break;
                case EmployeeTitle.None:
                    result = string.Empty;
                    break;
            }

            return result;
        }

        public static T GetParseEnum<T>(this string value)
        {
            value = value.Replace(" ", "").Replace("&", "").Replace(",","").Trim();
            return (T)Enum.Parse(typeof(T), value, true);
        }
      
    }
}