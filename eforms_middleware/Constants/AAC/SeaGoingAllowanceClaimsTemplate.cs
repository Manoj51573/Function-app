namespace eforms_middleware.Constants.AAC
{

    public static class SeaGoingAllowanceClaimsTemplate
    {
        //2.0 Manager Review - Task Notification to Requestor's Manager	- NotificationToRequestorsManager
        public const string NotificationToRequestorsManager = "<div>Hi,</div><br/>" +
                                             "<div>{0} has submitted a Sea Going Allowance Claim eForm #{1} for your review. Here is a summary of the Sea Going Allowance Claim request.</div><br/>" +
                                            "<table style=\"border: 1px solid\">" +
                                                "<tr style=\"border: 1px solid\">" +
                                                    "<td style=\"border: 1px solid\">Allowance claim type:</td>" +
                                                    "<td style=\"border: 1px solid\">Sea Going Allowance Claim</td>" +
                                                "</tr>" +
                                                "<tr>" +
                                                    "<td style=\"border: 1px solid\">Allowance and claim date:</td>" +
                                                    "<td style=\"border: 1px solid\">{2}</td>" +
                                                "</tr>" +
                                                "<tr>" +
                                                    "<td style=\"border: 1px solid\">From date:</td>" +
                                                    "<td style=\"border: 1px solid\">{3}</td>" +
                                                "</tr>" +
                                                "<tr>" +
                                                    "<td style=\"border: 1px solid\">To date:</td>" +
                                                    "<td style=\"border: 1px solid\">{4}</td>" +
                                                "</tr>" +
                                                "<tr>" +
                                                    "<td style=\"border: 1px solid\">Total days:</td>" +
                                                    "<td style=\"border: 1px solid\">{5}</td>" +
                                                "</tr>" +
                                                "<tr>" +
                                                    "<td style=\"border: 1px solid\">Allowance type:</td>" +
                                                    "<td style=\"border: 1px solid\">{6}</td>" +
                                                "</tr>" +
                                            "</table><br/>" +
                                            "<div>Please click <a href=\"{7}\">here</a> and action the eForm.</div><br/>" +
                                            "<div>Please contact Employee Services on 6551 6888 or by email at employeeservices@transport.wa.gov.au if you need further assistance.</div><br/>" +
                                            "<div>Employee Services</div>" +
                                            "<div>People and Culture</div>";

        

        
        

        //2.0.2 Form Recalled - Task Notification to Requestor - RecalledNotificationToRequestor
        public const string Recalled = "<div>Hi {0}</div><br/>" +
                                        "<div>This email confirms that you have recently recalled Sea Going Allowance Claim eForm {1} and the outstanding task with your manager has been cancelled.</div><br/>" +
                                        "<div>Please click <a href=\"{2}\">here</a> to access this eForm if you wish to review and re-submit.</div><br/>" +
                                       "<div>Please contact Employee Services on 6551 6888 or by email at employeeservices@transport.wa.gov.au if you need further assistance.</div><br/>" +
                                        "<div>Employee Services</div>" +
                                        "<div>People and Culture</div>";

        //2.2 Form Rejected - Manager Reject Notification to Requestor
        public const string Rejected = "<div>Hi {0}</div><br/>" +
                                       "<div>{1} has rejected your Sea Going Allowance Claim eForm #{2}.</div><br/>" +
                                       "<div>The following reason/s were given by {1} for taking that action: {3}</div><br/>" +
                                       "<div>Please click <a href=\"{4}\">here</a> if you wish to review and re-submit your eForm.</div><br/>" +
                                       "<div>Please contact Employee Services on 6551 6888 or by email at employeeservices@transport.wa.gov.au if you need further assistance.</div><br/>" +
                                       "<div>Employee Services</div>" +
                                       "<div>People and Culture</div>";

        //2.3 Task Escalation - Task Notification to Requestor's Line Manager's Manager
        public const string Escalated = "<div>Hi,</div><br/>" +
                                "<div>{0} has submitted a Sea Going Allowance Claim eForm #{1} that has now escalated for your approval.</div><br/>" +
                                "<div>Please review the sea going allowance claim details by clicking <a href=\"{2}\">here</a> and approve if accurate. If any details are inaccurate, please follow up with {0} to correct where necessary.</div><br/>" +
                                "<div>Please contact Employee Services on 6551 6888 or by email at employeeservices@transport.wa.gov.au if you need further assistance.</div><br/>" +
                                "<div>Employee Services</div>" +
                                "<div>People and Culture</div>";

        public const string ReminderToApprover = "<div>Hi,</div><br/>" +
                                            "<div>{0} has submitted a Sea Going Allowance Claim eForm #{1} for your review.</div><br/>" +
                                           "<table style=\"border: 1px solid\">" +
                                               "<tr style=\"border: 1px solid\">" +
                                                   "<td style=\"border: 1px solid\">Allowance claim type:</td>" +
                                                   "<td style=\"border: 1px solid\">Sea Going Allowance Claim</td>" +
                                               "</tr>" +
                                           "</table><br/>" +
                                           "<div>Please click <a href=\"{2}\">here</a> and action the eForm.</div><br/>" +
                                           "<div>Please contact Employee Services on 6551 6888 or by email at employeeservices@transport.wa.gov.au if you need further assistance.</div><br/>" +
                                           "<div>Employee Services</div>" +
                                           "<div>People and Culture</div>";



        //2.5 Task Escalation - Task Notification to PODeFormsBusinessAdmins Group
        public const string EscalationNotificationToPODeFormsBusinessAdminsGroup = "<div>HR Systems,</div>" +
                                      "<div>{0} has submitted a Sea Going Allowance Claim eForm #{1} that has escalated to your team for review and re-direction.</div>" +
                                      "<div>Please click <a href=\"{2}\">here</a> to review and re-assign the eForm.</div>" +
                                      "<div>Please contact Employee Services on 6551 6888 or by email at employeeservices@transport.wa.gov.au if you need further assistance.</div>" +
                                      "<div>Employee Services</div>" +
                                      "<div>People and Culture</div>";

        //2.6 Task Delegation - Task Notification to Delegated Manager
        public const string Delegated = "<div>Hi,</div><br/>" +
                                "<div>{0} has submitted a Sea Going Allowance Claim eForm #{1} that has now been delegated for your approval.</div><br/>" +
                                "<div>Please review the sea going allowance claim details by clicking <a href=\"{2}\">here</a> and approve if accurate. If any details are inaccurate, please follow up with {0} to correct where necessary.</div><br/>" +
                                "<div>Please contact Employee Services on 6551 6888 or by email at employeeservices@transport.wa.gov.au if you need further assistance.</div><br/>" +
                                "<div>Employee Services</div>" +
                                "<div>People and Culture</div>";


        //3.0 Form Approved - Manager Approve Notification to Requestor
        public const string ManagerApproveNotificationToRequestor = "<div>Hi {0}</div><br/>" +
                                     "<div>{1} has approved your Sea Going Allowance Claim eForm #{2}.</div><br/>" +
                                     "<div>Your form is now awaiting processing by Employee Services. You will receive a final notification once your form has been completed.</div><br/>" +
                                     "<div>Please click <a href=\"{3}\">here</a> if you wish to review your eForm.</div><br/>" +
                                     "<div>Please contact Employee Services on 6551 6888 or by email at employeeservices@transport.wa.gov.au if you need further assistance.</div><br/>" +
                                     "<div>Employee Services</div>" +
                                     "<div>People and Culture</div>";

        /*//3.2 Reject Email - ESO Reject Notification to Requestor
        public const string ESORejectedNotificationToRequestor = "<div>Hi {0}</div><br/>" +
                                      "<div>Employee Services have assessed your Sea Going Allowance Claim eForm #{2} and have rejected the form.</div><br/>" +
                                      "<div>The following reason/s were given by {1} for taking that action: {3}</div><br/>" +
                                      "<div>Please click <a href=\"{4}\">here</a> if you wish to review and re-submit your eForm.</div><br/>" +
                                      "<div>Please contact Employee Services on 6551 6888 or by email at employeeservices@transport.wa.gov.au if you need further assistance.</div><br/>" +
                                      "<div>Employee Services</div>" +
                                      "<div>People and Culture</div>";*/


        //4.0 Completion Notification - Completion Notification to Requestor
        public const string CompletionNotificationToRequestor = "<div>Hi {0},<div><br/>" +
                                        "<div>Employee Services have processed your Sea Going Allowance Claim eForm #{1}. Here is a summary of the request:</div><br/>" +
                                            "<table style=\"border: 1px solid\">" +
                                                "<tr style=\"border: 1px solid\">" +
                                                    "<td style=\"border: 1px solid\">Allowance claim type:</td>" +
                                                    "<td style=\"border: 1px solid\">Sea Going Allowance Claim</td>" +
                                                "</tr>" +
                                                "<tr>" +
                                                    "<td style=\"border: 1px solid\">Allowance and claim date:</td>" +
                                                    "<td style=\"border: 1px solid\">{2}</td>" +
                                                "</tr>" +
                                                "<tr>" +
                                                    "<td style=\"border: 1px solid\">From date:</td>" +
                                                    "<td style=\"border: 1px solid\">{3}</td>" +
                                                "</tr>" +
                                                "<tr>" +
                                                    "<td style=\"border: 1px solid\">To date:</td>" +
                                                    "<td style=\"border: 1px solid\">{4}</td>" +
                                                "</tr>" +
                                                "<tr>" +
                                                    "<td style=\"border: 1px solid\">Total days:</td>" +
                                                    "<td style=\"border: 1px solid\">{5}</td>" +
                                                "</tr>" +
                                                "<tr>" +
                                                    "<td style=\"border: 1px solid\">Allowance type:</td>" +
                                                    "<td style=\"border: 1px solid\">{6}</td>" +
                                                "</tr>" +
                                            "</table><br/>" +
                                        "<div>Your claim will be paid in the next available pay-run. You can click <a href=\"{7}\">here</a> to review your eForm.</div><br/>" +
                                        "<div>Please contact Employee Services on 6551 6888 or by email at employeeservices@transport.wa.gov.au if you need further assistance.</div><br/>" +
                                        "<div>Employee Services</div>" +
                                        "<div>People and Culture</div>";

        

    }
}
