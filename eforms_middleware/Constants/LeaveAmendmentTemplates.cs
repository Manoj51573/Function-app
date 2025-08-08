using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.DataModel;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Graph.SecurityNamespace;
using Microsoft.Graph.TermStore;
using System.Security.Cryptography.Xml;

namespace eforms_middleware.Constants;

public static class LeaveAmendmentTemplates
{


    public const string SUBMITTED_TO_LINEMANAGER_TEMPLATE =
   "<div>Hi,</div><br/>" +
   "<div>{0} has submitted a {1} Request eForm #{2} for your review.<br/></div>" +
   "<div>Please {3} if you wish to review and action the eForm.</div><br/>" +
   "<div>Please contact {4} if you need further assistance.</div> <br/>" + "<br/>" +
   "<br/><div>Employee Services</div>" +
    "<div>People and Culture</div>";

 public const string SUBMITTED_TO_ED_PAndC_GROUP_TEMPLATE =
"<div>Hi,</div><br/>" +
"<div>{0} has submitted a {1} Request eForm #{2} for your review. This request has come to you for final approval because it exceeds 90 calendar days in length.</div>" + "<br/>" +
"<div>Please {3} if you wish to review and action the eForm.</div><br/>" +
"<div>Please contact {4} if you need further assistance.</div> <br/>" + "<br/>" +
"<br/><div>Employee Services</div>" +
 "<div>People and Culture</div>";



    public const string DELEGATE_TEMPLATE =
            "<div>Hi, </div><br/>" +
            "<div>{0} has submitted a {1} Request eForm #{2} that has now been delegated for your review.<br/></div>" +
            "<div>Please {3} if you wish to review and action the eForm.</div><br/>" +
            "<div>Please contact {4} if you need further assistance.</div> <br/>" + "<br/>" +
            "<br/><div>Employee Services</div>" +
             "<div>People and Culture</div>";


    

    public const string SUBMITTED_TO_EMPLOYEE_TEMPLATE =
    "<div>Hi {0},</div><br/>" +
    "<div>{1} has submitted a {2} Request eForm #{3} on your behalf.<br/></div>" +
    "<div>Please {4} if you wish to review and action the eForm.</div><br/>" +
    "<div>Please contact {5} if you need further assistance.</div> <br/>" + "<br/>" +
    "<br/><div>Employee Services</div>" +
     "<div>People and Culture</div>";



    public static string GetLACDotEmployeeGroupTemplate(string EmployeeFullName, int FormInfoId)
    {

        var result =
            $"<div>Hi Team,  </div><br/>" +
            $"<div>{EmployeeFullName} has submitted a Leave Amendment and Cancellation Request eForm #{FormInfoId} for your review.<br/></div>" +
            "<br/><div>Please contact Employee Services on 6551 6888 or by email at employeeservices@transport.wa.gov.au if you need further assistance.</div>" +
            "<br/><div>Please click  to review and action the eForm.</div>" +
            "<br/><div>Employee Services</div>" +
             "<div>People and Culture</div>";
        return result;
    }



  
    public const string COMPLETED_TEMPLATE_PURCHASED_LEAVE =
        "<div>Hi {0},  </div><br/>" +
        "Your {1} Request eForm #{2} has now been completed by Employee Services. The details of your Purchased Leave are as follows:</div><br/>" +
        "<div>Number of Weeks Purchased: {5}</div>" + "<br/>" +
        "<div>Number of Hours Purchased: {6}</div>" + "<br/>" +
        "<div>Cessation of Deductions: {7}</div>" + "<br/>" +
        "<div>Deduction per fortnight: {8}</div>" + "<br/>" +
        "<div><strong>Please note:</strong></div>" + "<br/>" +
        "<div><ul>" +
        "<li>The deduction per fortnight will be taken from your pre-tax salary and is based on your current substantive rate of pay. Your deduction may change if your substantive rate changes throughout the year.</li>" +
        "<li>Please be advised that the hours have been calculated according to your current roster. If you change your hours, the leave balance does not automatically adjust. Please contact Employee Services if you would like to discuss the hours change impact. </li>" +
        "<li>The Purchased Leave deduction increases in the pay of your Annualised Leave Loading in December.</li>" +

        "</ul></div><br/>" +
        "<br/><div>The dates listed on your request for both Annual Leave and Purchased Leave have been entered into the HR System. Please click here if you wish to review these dates on the eForm.</div>" +
        "<br/><div>You are reminded to use all available Purchased Leave before 31 December of the calendar year. Any outstanding balance will be refunded to you in the last pay of February the following year. </div>" + "<br/>" +
        "<div>Please contact {4} if you need further assistance.</div> <br/>" + "<br/>" +
        "<br/><div>Employee Services</div>" +
        "<div>People and Culture</div>";

    public const string COMPLETED_TEMPLATE_DEFERRED_LEAVE =
        "<div>Hi {0},  </div><br/>" +
        "Your {1} Request eForm #{2} has now been completed by Employee Services.The details of your Deferred Salary Arrangement are as follows:</div><br/>" +
        "<div>Commencement of Deductions: {5}</div> " + "<br/>" +
        "<div>Cessation of Deductions: {6}</div> " + "<br/>" +
        "<div>Deduction per fortnight: 20% of Substantive Salary: {7}</div> " + "<br/>" +
        "<div><strong>Please note:</strong></div> <br/>" + "<br/>" +
        "<div><ul>" +
        "<li>The deduction per fortnight will be taken from your pre-tax salary and is based on your current substantive rate of pay. Your deduction may change if your substantive rate changes throughout the year.</li>" +
        "<li>Please be advised that the hours have been calculated according to your current roster. If you change your hours either upward or downward during the four year qualifying period, the entitlement in the fifth year of the arrangement will be affected. Leave balances will automatically be adjusted.</li>" +
        "<li>During the period of this arrangement, the Employer contribution to Superannuation will be adjusted accordingly.</li>" +
        "<li>The salary deduction will vary in line with changes to the Award, and in December when payment of your Annualised Leave Loading is processed.</li>" +
         "</ul></div><br/>" +
        "<div>Please {3} if you wish to review and action the eForm.</div><br/>" +
        "<div>Should you have any questions about the Deferred Salary Arrangement, please contact {4}. In addition, you may refer to Circular 3 / 2005 for further details.</div> <br/>" + "<br/>" +
        "<br/><div>Employee Services</div>" +
        "<div>People and Culture</div>";

    



    public const string COMPLETED_TEMPLATE_LEAVE_CASH_OUT =
       "<div>Hi {0},  </div><br/>" +
            "Your {1} Request eForm #{2} has now been completed by Employee Services. Please note that there will be a delay of up to 24 hours for this leave to be removed from your balance in myHRspace.</div><br/>" +
            "<div>Here is a summary of the request:</div><br/>" +
            "<div>Leave Cash Out Type: {5}</div><br/>" +
            "<div>Total Hours: {6}</div><br/>" +
            "<div>Gross Payment: {7}</div><br/>" +
            "<div>Tax Withheld: {8}</div><br/>" +
            "<div>Net Payment: {9}</div><br/>" +
            "<div>Payment Date: {10}</div><br/>" +
            "<div>Excised by Calendar Days: {11}</div><br/>" +
            "<div>Please {3} if you wish to review and action the eForm.</div><br/>" +
            "<div>Please contact {4} if you need further assistance.</div> <br/>" + "<br/>" +
            "<br/><div>Employee Services</div>" +
             "<div>People and Culture</div>";

    public const string COMPLETED_TEMPLATE_TO_MANAGER =
            "<div>Hi {0},  </div><br/>" +
            "Your {1} Request eForm #{2} has now been completed by Employee Services. Please note that there will be a delay of up to 24 hours for any leave booking or amendment to correctly flow through to your timesheet.</div><br/>" +
            "<div>Please {3} if you wish to review and action the eForm.</div><br/>" +
            "<div>Please contact {4} if you need further assistance.</div> <br/>" + "<br/>" +
            "<br/><div>Employee Services</div>" +
             "<div>People and Culture</div>";


    public const string TWELVE_MONTH_REMINDER =
        "<div>Dear {0}</div><br/>" +
        "<div>Your declaration completed on {1} has now passed it's 12 month valid period.</div><br/>" +
        "<div>Please submit a new {2} declaration if applicable or else no further action is required. </div><br/>" +
        "<div>Should you require any additional information in relation to this matter, or {3}.</div>";



    public const string SUBMITTED_TO_LINE_MANAGER_TEMPLATE =
        "<div>Hi, {0}</div><br/>" +
        "<div>{1} has submitted a {5} Request eForm #{2} your review.</div>" +
        "<div>Please click {3} to review and action the eForm.</div><br/>" +
        "<div>Please contact {4} if you need further assistance.</div> <br/>" +
        "<br/><div>Employee Services</div>" +
         "<div>People and Culture</div>";


    public const string SUBMITTED_WITHOUT_MANAGER_TEMPLATE =
        "<div>Hi, {0}</div><br/>" +
        "<div>{1} has submitted a Leave Amendment and Cancellation Request eForm #{2} your review.</div>" +
        "<div>Please click {3} to review and action the eForm.</div><br/>" +
        "<div>Please contact {4} if you need further assistance.</div> <br/>" +
        "<br/><div>Employee Services</div>" +
         "<div>People and Culture</div>";

    public const string RECALL_OWNER_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>This email confirms that you have successfully recalled your {1} Request eForm #{2} and the outstanding task has been cancelled.</div><br/>" +
        "<div>Please {3} if you wish to review and action the eForm.</div><br/>" +
        "<div>Please contact {4} if you need further assistance.</div> <br/>" +
        "<br/><div>Employee Services</div>" +
         "<div>People and Culture</div>";


    public const string RECALL_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>The {4} request has been recalled by {1} and does not require immediate " +
        "action.</div><br/>" +
        "<div>If you wish to review the form, please {2} to review the declaration.</div><br/>" +
        "<div>Should you require any additional information in relation to this matter, please contact the {3} to discuss";



    public const string REJECTED_TEMPLATE =
        "<div>Hi {0},</div><br/>" +
        "<div>{1} has rejected your {2} Request eForm #{3}.</br> </br>" +
        "<div>The following reasons were given by {1} for taking that action: </br></br>{4}</div><br/>" +
        "<div>Please {5} if you wish to review and action the eForm.</div><br/>" +
        "<div>Please contact {6} if you need further assistance.</div> <br/>" +
        "<br/><div>Employee Services</div>" +
        "<div>People and Culture</div>";


    public const string ESCALATION_TEMPLATE =
        "<div>Hi, {0}</div><br/>" +
        "<div>{1} has submitted a {2} Request eForm #{3} that has now escalated for your review.</div>" +
        "<div>Please click {4} to review and action the eForm.</div><br/>" +
        "<div>Please contact {5} if you need further assistance.</div> <br/>" +
        "<br/><div>Employee Services</div>" +
         "<div>People and Culture</div>";

    public const string ESCALATION_MANAGER_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>{1} has submitted a {2} Request eForm #{3} that has now escalated for your review.</div>" +
       "<div>Please click {4} to review and action the eForm.</div><br/>" +
        "<div>Please contact {5} if you need further assistance.</div> <br/>" +
        "<br/><div>Employee Services</div>" +
         "<div>People and Culture</div>";

    public const string ESCALATION_PODGROUP_TEMPLATE =
    "<div>Hi Team, </div><br/>" +
    "<div>{0} has submitted a {1} Request eForm #{2} that has now escalated for your review.</div>" +
   "<div>Please click {3} to review and action the eForm.</div><br/>" +
    "<div>Please contact {4} if you need further assistance.</div> <br/>" +
    "<br/><div>Employee Services</div>" +
     "<div>People and Culture</div>";

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

  
    public const string COMPLETED_BY_INDEPENDENT_REVIEWER_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>Your Conflict of Interest (Other) declaration has been independently reviewed.</div><br/>" +
        "<div>Please {1} to view the form including any approved plan to manage the conflict.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or {2}</div>";

    public const string REJECTED_TEMPLATEppppp =
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
        "<div>Please {2} to review the declaration and action your declaration.</div><br/>" +
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
}