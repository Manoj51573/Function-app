using Microsoft.Graph.TermStore;
using System;
using System.Net;

namespace eforms_middleware.Constants
{
    public static partial class HomeGaraging
    {
        public const int DIRECTOR_PFM_POSITION_NUMBER = 00015194;
        public const string ED_PEOPLE_AND_CULTURE_POSITION_NUMBER = "00014488";
        public const int ED_PEOPLE_AND_CULTURE_POSITION_ID = 14488;
        public const int DIRECTOR_GENERAL_POSITION_NUMBER = 14489;
        public const int MANAGING_DIRECTOR_DOT = 14025;
        public const string ED_ODG_POSITIONNUMBER = "00016395";
        public const int ED_ODG_POSITION_ID= 16395;
        public const int ODG_POSITION_ID = 25981;
        public const int REGIONAL_ED_POSITION_ID = 21994;
        public const string ODG_AUDIT_GROUP_EMAIL = "!DOTODGGovernanceandAudit@transport.wa.gov.au";
        public const string ODG_AUDIT_GROUP_NAME = "!DOT ODG GOVERNANCE AND AUDIT";
        public static Guid ODG_AUDIT_GROUP_ID = new("A194B07F-D06E-420D-863D-434E63E725D5");

        public const string TALENT_TEAM_GROUP_EMAIL = "!DOTTalentTeamGroup@transport.wa.gov.au";
        public const string TALENT_TEAM_GROUP_NAME = "!DOT Talent Team Group";
        public static Guid TALENT_TEAM_GROUP_ID = new("57ED8B34-D66F-4ACE-87CC-322B3F288CD7");

        public const string POD_EFFORMS_BUSINESS_ADMIN_GROUP_EMAIL = "!DOTPODeFormsBusinessAdminsGroup@transport.wa.gov.au";
        public const string POD_EFFORMS_BUSINESS_ADMIN_GROUP_NAME = "!DOT PODeFormsBusinessAdminsGroup";
        public static Guid POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID = new("51BD86DA-DD85-49AC-AA48-D75109C8BBA1");

        public static Guid POD_ESO_GROUP_ID = new("B3B664D3-94A8-4285-87F6-C8F849E2600B");

        public static Guid OPR_GROUP_ID = new("F2297683-CCD9-4085-8BE2-0A96AF13540E");
        public const string OPR_GROUP_EMAIL = "!DOTSCARF@transport.wa.gov.au";

        public static Guid ASSETS_AND_TAX_GROUP_ID = new("4F4A32BF-CDD8-4C80-8FE6-A1A6084249A0");

        public const string DOT_DVS_FIXED_TERM_APPOINTMENT_QA_GROUP = "!DOTDVSFixedTermAppointmentQAGroup@transport.wa.gov.au";
        public const string DOT_DVS_FIXED_TERM_APPOINTMENT_QA_GROUP_NAME = "!DOT DVS Fixed Term Appointment QA Group";
        public static Guid  DOT_DVS_FIXED_TERM_APPOINTMENT_QA_GROUP_ID = new("E08666FE-5F69-4898-90AD-407BFC85211B");

        public const string DOT_EMPLOYEE_SERVICES_OFFICER_GROUP = "!DOT Employee Services Officer Group";
        public const string DVS_EFFORMS_DIRECTORATE_NAME = "Driver and Vehicle Services";
        public const string TIER_THREE = "Tier3";

        public const string LEASE_AND_FLEET_FORM_ADMIN_GROUP = "fleetmanagement@transport.wa.gov.au";
        public const string LEASE_AND_FLEET_FORM_ADMIN_GROUP_NAME = "!DOT Lease Fleet Group";
        public static Guid LEASE_AND_FLEET_FORM_ADMIN_GROUP_ID = new("65A7955E-E285-4D5D-A1F0-580125F40D42");
        public static string lastApproverEmailid = "";


    }
}