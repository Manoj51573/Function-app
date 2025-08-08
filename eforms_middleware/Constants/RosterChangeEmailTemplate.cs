namespace eforms_middleware.Constants
{
    public static class RosterChangeEmailTemplate
    {
        public const string SUBMITTED_TO_EMPLOYEE_TEMPLATE =
                   "<div>Hi {0},</div><br/>" +
                   "<div>{1} has submitted a Roster Change Request eForm #{2} on your behalf.</div><br/>" +
                   "<div>Please {3} here to review the eForm.</div><br/>" +
                   "<div>Please contact {4} if you need further assistance.</div>" +
                   "<br/><div>Employee Services</div>" +
                   "<div>People and Culture</div>";

        public const string SUBMITTED_TO_LINEMANAGER_TEMPLATE =
               "<div>Hi,</div><br/>" +
               "<div>{0} has submitted a {1} Request eForm #{2} for your review.<br/><br></div>" +
               "<div>Please {3} here to review and action the eForm.</div><br/>" +
               "<div>Please contact {4} if you need further assistance.</div>" +
               "<br/><div>Employee Services</div>" +
               "<div>People and Culture</div>";

        public const string RECALL_OWNER_TEMPLATE =
                 "<div>Dear {0},</div><br/>" +
                 "<div>This email confirms that you have successfully recalled your Roster Change Request eForm #{1} and the outstanding task with {2} has been cancelled.</div><br/>" +
                 "<div>Please {3} here if you wish to review and re-submit the eForm.</div><br/>" +
                 "<div>Please contact {4} if you need further assistance.</div>" +
                 "<br/><div>Employee Services</div>" +
                 "<div>People and Culture</div>";

        public const string REJECTED_TEMPLATE =
              "<div>Hi {0},</div><br/>" +
              "<div>{1} has rejected your Roster Change Request eForm #{2}</br><br/>" +
              "<div>The following reasons were given by {3} for taking that action: {4}</div><br/>" +
              "<div>Please {5} here if you wish to review and re-submit the eForm.</div><br/>" +
              "<div>Please contact {6} if you need further assistance.</div>" +
              "<br/><div>Employee Services</div>" +
              "<div>People and Culture</div>";

        public const string COMPLETED_TEMPLATE_TO_MANAGER =
            "<div>Hi {0},  </div><br/>" +
            "Your {1} Request eForm #{2} has now been completed by Employee Services. " +
            "Please note that there will be a delay of up to 24 hours for this change to correctly flow through to your timesheet.</div><br/>" +
            "<div>Please {3} to review the eForm.</div><br/>" +
            "<div>Please contact {4} if you need further assistance.</div>" +
            "<br/><div>Employee Services</div>" +
             "<div>People and Culture</div>";

        public const string DELEGATE_TEMPLATE =
            "<div>Hi, </div><br/>" +
            "<div>{0} has submitted a Roster Change Request eForm #{1} that has now been delegated for your review.<br/></div><br/>" +
            "<div>Please {2} to review and action the eForm.</div><br/><br/>" +
            "<div>Please contact {3} if you need further assistance.</div>" +
            "<br/><div>Employee Services</div>" +
             "<div>People and Culture</div>";

        public const string SUBMITTED_TO_LINE_MANAGER_TEMPLATE =
                 "<div>Hi, {0}</div><br/>" +
                "<div>{1} has submitted a Roster Change Request eForm #{2} your review.</div><br/><br/>" +
                "<div>Please click {3} to review and action the eForm.</div><br/>" +
                "<div>Please contact {4} if you need further assistance.</div>" +
                "<br/><div>Employee Services</div>" +
                 "<div>People and Culture</div>";

        public const string ESCALATION_MANAGER_TEMPLATE =
               "<div>Dear {0},</div><br/>" +
                "<div>{1} has submitted a {2} Request eForm #{3} that has now escalated for your review.</div><br/>" +
               "<div>Please {4} to review and action the eForm.</div><br/>" +
                "<div>Please contact {5} if you need further assistance.</div>" +
                "<br/><div>Employee Services</div>" +
                 "<div>People and Culture</div>";

        public const string ENDORSED_TEMPLATE =
                "<div>Dear {0},</div><br/>" +
                "<div>A Roster Change Request has been submitted by {1} in relation to {2}.</div><br/><br/>" +
                "<div>Please {3} to review and action the declaration.</div><br/>" +
                "<div>Further information about conflicts of interest is available from Transporta, or {4}</div>";

        public const string CANCELLED_TEMPLATE =
              "<div>Dear {0},</div><br/>" +
              "<div>Your Roster Change Request submitted in relation to {1} " +
              "has cancelled due to one or more of the reasons below.</div><br/><br/>" +
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
            "<div>The Roster Change Request submitted by {1} in relation to {2} " +
            "has cancelled due to one or more of the reasons below.</div><br/>" +
            "<div><ul>" +
            "<li>The Tier 3 position for your directorate is currently vacant.</li>" +
            "<li>You did not action the form in the required timeframe and the form escalated to the Tier " +
            "3 Executive Director, whose position is currently vacant.</li>" +
            "</ul></div><br/>" +
            "<div>Please {3} to view the declaration.</div><br/>" +
            "<div>If you have any further questions please contact the {4}." +
            "</div>";

        public const string ESCALATION_PODGROUP_TEMPLATE =
            "<div>Hi Team, </div><br/>" +
            "<div>{0} has submitted a {1} Request eForm #{2} that has now escalated for your review and requires your action.</div><br/><br/>" +
            "<div><br>Please click {3} to review and action the eForm.</div><br/>" +
            "<div>Please contact {4} if you need further assistance.</div>" +
            "<br/><div>Employee Services</div>" +
             "<div>People and Culture</div>";
    }
}
