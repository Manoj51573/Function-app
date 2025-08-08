namespace eforms_middleware.Constants.COI;

public static class CprTemplates
{
    public const string SUBMITTED_TO_ED_TEMPLATE = 
        "<div>Dear {0},</div><br/>" +
        "<div>Thank you for your conflict of interest declaration in relation to Close personal relationship in the " + 
        "workplace. Your declaration has been sent to the Executive Director of People & Culture for review.</div><br/>" +
        "<div>Please {1} if you want to view, recall or make changes to your declaration form.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services " +
        "on (08) 6551 6888 / " + 
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div>";

    public const string DIRECT_TO_ED_P_AND_C_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>A conflict of interest declaration has been submitted by {1} in relation to Close personal " +
        "relationship in the workplace. This has escalated directly for your approval at the request of {1} due to the " +
        "sensitive nature of the declaration.</div><br/>" +
        "<div>Please {2} to review and action the declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services on " +
        "(08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div>";
    
    public const string APPROVED_AND_COMPLETED_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>Your conflict of interest declaration regarding Close personal relationship in the workplace has been " +
        "approved by senior management.</div><br/>" +
        "<div>Please {1} to view the form including any approved plan to manage the conflict.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services on " +
        "(08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>" +
        "<div>On behalf of {2}</div><br/>" +
        "<div>Executive Director</div><br/>" +
        "<div>People and Culture</div>";
    
    public const string APPROVED_AND_COMPLETED_TO_MANAGER_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>The conflict of interest declaration regarding Close personal relationship in the workplace for {1} has been " +
        "approved by senior management.</div><br/>" +
        "<div>Please {2} to view the form including any approved plan to manage the conflict.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services on " +
        "(08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>" +
        "<div>On behalf of {3}</div><br/>" +
        "<div>Executive Director</div><br/>" +
        "<div>People and Culture</div>";
}