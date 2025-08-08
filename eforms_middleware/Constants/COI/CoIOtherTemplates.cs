namespace eforms_middleware.Constants.COI;

public static class CoIOtherTemplates
{

    public const string TWELVE_MONTH_REMINDER =
        "<div>Dear {0},</div><br/>" +
        "<div>Your conflict of interest other circumstances declaration completed on {1} has now passed a 12 month period.</div><br/>" +
        "<div>Please {2} here to review your declaration. You will need to complete a new declaration if circumstances are ongoing. If no longer applicable no further action required.</div><br/>" +
      "<div>Further information about conflicts of interest is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or {3}</div><br/>";



    public const string SUBMITTED_TO_EMPLOYEE_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>Thank you for your conflict of interest declaration in relation to {1}.<br/><br/>" +
        "Your declaration has been sent to your manager for review.</div><br/>" +
        "<div>Please {2} if you want to view, recall or make changes to your declaration form.</div><br/>" +
        "<div>Further information about conflicts of interest is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or {3}</div><br/>";


    public const string SUBMITTED_TO_LINE_MANAGER_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>A conflict of interest declaration regarding {2} has been submitted by " +
        "{1}.</div><br/>" +
        "<div>Please {3} to review and action the declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or {4}</div><br/>";


    public const string SUBMITTED_WITHOUT_MANAGER_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>A conflict of interest declaration has been submitted by {1} in relation to {2}. " +
        "This form has escalated for your approval due to inaction from the line manager or due " +
        "to {1} not having an acting manager.</div><br/>" +
        "<div>Please {3} to review and action the declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or {4}</div><br/>";

    public const string RECALL_OWNER_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>You have recalled your conflict of interest declaration.</div><br/>" +
        "<div>{1} to view, amend and resubmit your declaration as necessary.</div><br/>" +
         "<div>Further information about {3} is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or via {2}</div>";


    public const string RECALL_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>The conflict of interest request has been recalled by {1} and does not require immediate " +
        "action.</div><br/>" +
        "<div>If you wish to review the form, please {2} to review the declaration.</div><br/>" +
        "<div>Should you require any additional information in relation to this matter, please contact the {3} to discuss";

    public const string ESCALATION_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>A conflict of interest declaration has been submitted by {1} in relation to {2}. " +
        "This form has escalated for your approval due to inaction from the senior management.</div><br/>" +
        "<div>Please {3} to review the declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or {4}</div><br/>";

    public const string ESCALATION_MANAGER_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>A conflict of interest declaration has been submitted by {1} in relation to {2}. " +
        "This form has escalated to {3} for approval.</div><br/>" +
        "<div>Please {4} to view the declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or {5}</div><br/>";

    public const string APPROVED_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>A conflict of interest declaration has been endorsed by the line manager for {2}.</div><br/>" +
        "<div>Please {3} to review the declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or {4}</div><br/>";

    public const string APPROVED_BY_TIER_THREE_TEMPLATE =
       "<div>Dear {0},</div><br/>" +
       "<div>A conflict of interest declaration has been received from {5} regarding {1}.</div><br/>" +
       "<div> The declaration has been reviewed and was approved by {2}.</div><br/>" +
       "<div>Please {3} to review the form for quality assurance purposes.</div>";

    public const string COMPLETED_TEMPLATE =
     "<div>Dear {0},</div><br/>" +
     "<div>Your conflict of interest declaration regarding {1} has been approved by senior management.</div><br/>" +
     "<div>Please {2} to view the form including any approved plan to manage the conflict.</div><br/>" +
     "<div>Further information about conflicts of interest is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or {3}</div><br/>" +
     "<div>On behalf of <br/> Kate Wang</div>" +
     "<div>Executive Director</div>" +
     "<div>{5}</div>";

    public const string COMPLETED_ED_ODG_TEMPLATE =
     "<div>Dear {0},</div><br/>" +
     "<div>Your conflict of interest declaration regarding {1} has been approved by senior management.</div><br/>" +
     "<div>Please {2} to view the form including any approved plan to manage the conflict.</div><br/>" +
     "<div>Further information about conflicts of interest is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or {3}</div><br/>";

    public const string COMPLETED_TEMPLATE_TO_MANAGER =
      "<div>Dear {0},</div><br/>" +
      "<div>The conflict of interest declaration submitted by {1} regarding {2} has been approved by senior management.</div><br/>" +
      "<div>Please {3} to view the form including any approved plan to manage the conflict.</div><br/>" +
      "<div>Further information about conflicts of interest is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or {4}</div><br/>" +
      "<div>On behalf of <br/> Kate Wang</div>" +
      "<div>Executive Director</div>" +
      "<div>{6}</div>";

    public const string INDEPENDENT_COMPLETE_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>A conflict of interest declaration regarding other circumstances has been received from {1}. </div><br/>" +
        "<div>The declaration has been independently reviewed and approved by {4}. </div><br/>" +
        "<div>Please {2} to review the form for quality assurance purposes.</div><br/>" +
        "<div>Further information about conflicts of interest is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or {3}</div><br/>";


    public const string ENDORSED_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>A conflict of interest declaration has been submitted by {1} in relation to {2}.</div><br/>" +
        "<div>Please {3} to review and action the declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or {4}</div><br/>";




    public const string COMPLETED_BY_INDEPENDENT_REVIEWER_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>A Conflict of Interest other circumstances declaration from {2} has been independently reviewed.</div><br/>" +
        "<div>Please {1} to view the form including any approved plan to manage the conflict.</div><br/>" +
        "<div>Further information about conflicts of interest is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>.</div><br/>";

    public const string REJECTED_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>The conflict of interest declaration you submitted in relation to {1} has not " +
        "been endorsed by your line manager.</div><br/>" +
        "<div>Please {2} to view their comments and amend your declaration if required.  You may also want to " +
        "contact your line manager to discuss further.</div><br/>" +
        "<div>Further information about conflicts of interest is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or {3}</div><br/>";

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
        "</div><br/>";

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
        "</div><br/>";


    public const string NOT_ENDORSED_TEMPLATE =
    "<div>Dear {0},</div><br/>" +
    "<div>A conflict of interest declaration you submitted in relation to other circumstances has not been approved.</div><br/>" +
    "<div>Please {2} to view decision maker comments and amend your declaration. <br/><br/>" +
    "You may also want to contact your line manager to discuss further. </div><br/>" +
    "<div>Further information about conflicts of interest is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or {3}</div><br/>";


    public const string NOT_ENDORSED_MANAGER_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>The conflict of interest declaration submitted by {1} in relation to {2} has not " +
        "been approved.</div><br/>" +
        "<div>Please {3} to view the decision maker’s comments.</div><br/>" +
        "<div>Further information about conflicts of interest is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or {4}</div><br/>";

    public const string SPECIAL_REMINDER_TEMPLATE =
        "<div>Dear ODG Governance & Audit,</div><br/>" +
        "<div>This notification is a reminder of the Conflict of Interest other circumstances declaration submitted by {0} and " +
        "approved by senior management on {1}.</div><br/>" +
        "<div>Please {2} to review the declaration and perform the necessary actions.</div><br/>" +
        "<div>Further information about conflicts of interest is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or ODG Governance and Audit via " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div>";

    public const string INDEPENDENT_REVIEW_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>You have been selected to undertake an independent review of a decision regarding a conflict of interest other circumstances declaration by {3}.</div><br/>" +
        "<div>Please {2} to complete the review.</div><br/>" +
        "<div>Further information about conflicts of interest is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or ODG Governance and Audit via " +
        "<a href=\"mailto:odggovernanceandaudit@transport.wa.gov.au\">odggovernanceandaudit@transport.wa.gov.au</a></div>";

    public const string DELEGATE_REVIEW_TEMPLATE =
       "<div>Dear {0},</div><br/>" +
       "<div>A conflict of interest declaration regarding other circumstances submitted by {3} has been delegated to you by ODG Governance and Audit for review. <br/>  <br/>" +
       "<div>Please {2} complete.</div><br/>" +
       "<div>Further information about conflicts of interest is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or ODG Governance and Audit via " +
       "<a href=\"mailto:odggovernanceandaudit@transport.wa.gov.au\">odggovernanceandaudit@transport.wa.gov.au</a></div><br/>";
}