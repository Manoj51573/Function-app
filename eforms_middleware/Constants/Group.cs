using System;

namespace eforms_middleware.Constants;

public class Group
{
    public static Guid POD_FORMS_ADMIN_BUSINESS_ID = new("51BD86DA-DD85-49AC-AA48-D75109C8BBA1");
}

public static class RosterChangeRequest
{
    public static string Employee_Service_GroupEmail = "!DOTEmployeeServicesOfficerGroup@transport.wa.gov.au";

    public static Guid EMP_SERVICE_GROUP_ID = new("B3B664D3-94A8-4285-87F6-C8F849E2600B");
    public const string ED_ODG_POSITIONNUMBER = "00016395";
    public const int ED_ODG_POSITION_ID = 16395;

    public const string POD_EFFORMS_BUSINESS_ADMIN_GROUP_EMAIL = "!DOTPODeFormsBusinessAdminsGroup@transport.wa.gov.au";
    public const string POD_EFFORMS_BUSINESS_ADMIN_GROUP_NAME = "!DOT PODeFormsBusinessAdminsGroup";
    public static Guid POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID = new("51BD86DA-DD85-49AC-AA48-D75109C8BBA1");

    public const string DVS_EFFORMS_BUSINESS_ADMIN_GROUP_EMAIL = "!DOTDVSFixedTermAppointmentQAGroup@transport.wa.gov.au";
    public const string DVS_EFFORMS_BUSINESS_ADMIN_GROUP_NAME = "!DOT DVS Fixed Term Appointment QA Group";
    public const string DVS_EFFORMS_DIRECTORATE_NAME = "Driver and Vehicle Services";
    public static Guid DVS_EFFORMS_BUSINESS_ADMIN_GROUP_ID = new("E08666FE-5F69-4898-90AD-407BFC85211B");
}