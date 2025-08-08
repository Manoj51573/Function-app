namespace eforms_middleware.Constants.COI;

public static class CoITemplates
{
    public const string TWELVE_MONTH_REMINDER =
        "<div>Dear {0}</div><br/>" +
        "<div>Your declaration completed on {1} has now passed it's 12 month valid period.</div><br/>" +
        "<div>Please submit a new {2} declaration if applicable or else no further action is required. </div><br/>" +
        "<div>Should you require any additional information in relation to this matter, or {3}.</div>";

    public const string SUBMITTED_TO_EMPLOYEE_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>Thank you for your conflict of interest declaration in relation to {1}. Your " +
        "declaration has been sent to your manager for review.</div><br/>" +
        "<div>Please {2} if you want to view, recall or make changes to your declaration form.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or {3}</div>";

    public const string SUBMITTED_TO_LINE_MANAGER_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>A conflict of interest declaration regarding {2} has been submitted by " +
        "{1}.</div><br/>" +
        "<div>Please {3} to review and action the declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or {4}</div>";

    public const string SUBMITTED_WITHOUT_MANAGER_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>A conflict of interest declaration has been submitted by {1} in relation to {2}. " +
        "This form has escalated for your approval due to inaction from the line manager or due " +
        "to {1} not having an acting manager.</div><br/>" +
        "<div>Please {3} to review and action the declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or {4}</div>";

    public const string RECALL_OWNER_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>Your conflict of interest declaration was successfully recalled.</div><br/>" +
        "<div>If you wish to re-submit your eForm, please {1} to review the declaration.</div><br/>" +
        "<div>Should you require any additional information in relation to this matter, please contact the {2} to discuss " +
        "further.</div><br/>" +
        "<div>Thank you</div>";

    public const string RECALL_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>The conflict of interest request has been recalled by {1} and does not require immediate " +
        "action.</div><br/>" +
        "<div>If you wish to review the form, please {2} to review the declaration.</div><br/>" +
        "<div>Should you require any additional information in relation to this matter, please contact the {3} to discuss " +
        "further.</div><br/>" +
        "<div>Thank you</div>";

    public const string ESCALATION_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>A conflict of interest declaration has been submitted by {1} in relation to {2}. " +
        "This form has escalated for your approval due to inaction from the line manager or due to " +
        "{1} not having an acting manager.</div><br/>" +
        "<div>Please {3} to review and action the declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or {4}</div>";

    public const string ESCALATION_MANAGER_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>A conflict of interest declaration has been submitted by {1} in relation to {2}. " +
        "This form has escalated to {3} for approval.</div><br/>" +
        "<div>Please {4} to view the declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or {5}</div>";

    public const string APPROVED_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>A conflict of interest declaration has been submitted in relation to {1} and " +
        "has been approved by the line manager for {2}.</div><br/>" +
        "<div>Please {3} to review and action the declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or {4}</div>";

    public const string ENDORSED_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>A conflict of interest declaration has been submitted by {1} in relation to {2}.</div><br/>" +
        "<div>Please {3} to review and action the declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or {4}</div>";

    public const string COMPLETED_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>Your conflict of interest declaration regarding {1} has been approved by senior management.</div><br/>" +
        "<div>Please {2} to view the form including any approved plan to manage the conflict.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or {3}</div><br/>" +
        "<div>On behalf of {4}</div><br/>" +
        "<div>Executive Director</div><br/>" +
        "<div>{5}</div>";

    public const string COMPLETED_TEMPLATE_TO_MANAGER =
        "<div>Dear {0},</div><br/>" +
        "<div>The conflict of interest declaration submitted by {1} regarding {2} has been approved by senior management.</div><br/>" +
        "<div>Please {3} to view the form including any approved plan to manage the conflict.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or {4}</div><br/>" +
        "<div>On behalf of {5}</div><br/>" +
        "<div>Executive Director</div><br/>" +
        "<div>{6}</div>";

    public const string COMPLETED_BY_INDEPENDENT_REVIEWER_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>Your Conflict of Interest (Other) declaration has been independently reviewed.</div><br/>" +
        "<div>Please {1} to view the form including any approved plan to manage the conflict.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or {2}</div>";

    public const string REJECTED_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>The conflict of interest declaration you submitted in relation to {1} has not " +
        "been approved by your line manager.</div><br/>" +
        "<div>Please {2} to view their comments and amend your declaration if required.  You may also want to " +
        "contact your line manager to discuss further.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or {3}</div>";

    public const string CANCELLED_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>Your conflict of interest declaration submitted in relation to {1} " +
        "has cancelled due to one or more of the reasons below.</div><br/>" +
        "<div><ul>" +
        "<li>The Tier 3 position for your directorate is currently vacant.</li>" +
        "<li>Both your line manager and Tier 3 Executive Director positions are currently vacant.</li>" +
        "<li>Your line manager did not action the form in the required timeframe and the form escalated to the Tier " +
        "3 Executive Director, whose position is currently vacant.</li>" +
        "</ul></div><br/>" +
        "<div>Please {2} to review the declaration and re-submit your declaration.</div><br/>" +
        "<div>If you have any further questions please contact the {3}." +
        "</div>";

    public const string CANCELLED_TO_MANAGER_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>The conflict of interest declaration submitted by {1} in relation to {2} " +
        "has cancelled due to one or more of the reasons below.</div><br/>" +
        "<div><ul>" +
        "<li>The Tier 3 position for your directorate is currently vacant.</li>" +
        "<li>You did not action the form in the required timeframe and the form escalated to the Tier " +
        "3 Executive Director, whose position is currently vacant.</li>" +
        "</ul></div><br/>" +
        "<div>Please {3} to view the declaration.</div><br/>" +
        "<div>If you have any further questions please contact the {4}." +
        "</div>";

    public const string NOT_ENDORSED_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>The conflict of interest declaration you submitted in relation to {1} has not " +
        "been approved.</div><br/>" +
        "<div>Please {2} to view the decision maker’s comments and make any necessary amendments to your initial " +
        "declaration. You may also want to contact your line manager to discuss further.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or {3}</div>";

    public const string NOT_ENDORSED_MANAGER_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>The conflict of interest declaration submitted by {1} in relation to {2} has not " +
        "been approved.</div><br/>" +
        "<div>Please {3} to view the decision maker’s comments.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or {4}</div>";

    public const string SPECIAL_REMINDER_TEMPLATE =
        "<div>Dear ODG Governance & Audit,</div><br/>" +
        "<div>This notification is a reminder of the Conflict of Interest (Other) declaration submitted by {0} and " +
        "approved by senior management on {1}.</div><br/>" +
        "<div>Please {2} to review the declaration and perform the necessary actions.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or ODG Governance and Audit on (08) 6552 7083 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div>";

    public const string INDEPENDENT_REVIEW_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>A request for independent review of a Conflict of Interest (Other) decision has been received from " +
        "{1}.</div><br/>" +
        "<div>Please {2} to conduct an independent review and action.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or ODG Governance and Audit on (08) 6552 7083 / " +
        "<a href=\"mailto:odggovernanceandaudit@transport.wa.gov.au\">odggovernanceandaudit@transport.wa.gov.au</a></div>";

    public const string DELEGATE_REVIEW_TEMPLATE =
       "<div>Dear {0},</div><br/>" +
       "<div>A request for Conflict of Interest (Other) has now been delegated for your review. " +
       "<div>Please click {2} here to review and action the eForm.</div><br/>" +
       "<div>Further information about conflicts of interest is available from Transporta, or ODG Governance and Audit on (08) 6552 7083 / " +
       "<a href=\"mailto:odggovernanceandaudit@transport.wa.gov.au\">odggovernanceandaudit@transport.wa.gov.au</a></div>";

    public const string SUBMITTED_DG_TEMPLATE =
        "<div>Dear Admin,</div><br/>" +
       "<div>A Conflict of Interest declaration has been submitted by {0}. As they report directly to the Director-General, this form has come to you for action.</div><br/>" +
       "<div>Please {1} to review and action the declaration.</div><br/>";

    public const string APPROVED_DG_TEMPLATE =
       "<div>Dear {0},</div><br/>" +
       "<div>The Conflict of Interest declaration you submitted has been actioned by the relevant admin team.</div><br/>" +
       "<div>Please {1} to view the form including any approved plan to manage the conflict.</div><br/>";
}