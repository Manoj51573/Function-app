using System;

namespace eforms_middleware.Constants
{
    public static partial class AllowanceAndClaims
    {
       
        public const string ED_PEOPLE_AND_CULTURE_POSITION_NUMBER = "00014488";
        public const int ED_PEOPLE_AND_CULTURE_POSITION_ID = 14488;
        public const int DIRECTOR_GENERAL_POSITION_NUMBER = 14489;
        public const string ED_ODG_POSITIONNUMBER = "00016395";
        public const int ED_ODG_POSITION_ID= 16395;
        public const string ODG_AUDIT_GROUP_EMAIL = "!DOTODGGovernanceandAudit@transport.wa.gov.au";
        public const string ODG_AUDIT_GROUP_NAME = "!DOT ODG GOVERNANCE AND AUDIT";
        public static Guid ODG_AUDIT_GROUP_ID = new("A194B07F-D06E-420D-863D-434E63E725D5");

        public const string POD_EFFORMS_BUSINESS_ADMIN_GROUP_EMAIL = "!DOTPODeFormsBusinessAdminsGroup@transport.wa.gov.au";
        public const string POD_EFFORMS_BUSINESS_ADMIN_GROUP_NAME = "!DOT PODeFormsBusinessAdminsGroup";
        public static Guid POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID = new("51BD86DA-DD85-49AC-AA48-D75109C8BBA1");

        public const string POD_ESO_GROUP_EMAIL = "!DOTEmployeeServicesOfficerGroup@transport.wa.gov.au";
        public const string POD_ESO_GROUP_NAME = "!DOT Employee Services Officer Group";
        public static Guid POD_ESO_GROUP_ID = new("B3B664D3-94A8-4285-87F6-C8F849E2600B");
    }
}