using System;

namespace eforms_middleware.Constants.OPR
{



    public static class WHSIREmailTemplate
    {
        public static Guid WHS_GROUP_ID = new("50DB6EE7-523F-4782-831D-F59B4B22FA40");
        public const string WHS_GROUP_EMAIL = "!DOTWHS@transport.wa.gov.au";

        public const string Group = "<div>Hi,</div>" +
                                       "<div><br>Please be advised that {0} has submitted Work Health and Safety Incident Report {2}. This form has been approved by management and now requires your final investigation and completion.</div>" +
                                       "<div><br>Please click <a href=\"{1}\">here</a> to review and action the eForm.</div>" +
                                       "<div><br>If you require further information, please contact the WHS team by email at  <a href=\"mailto:WHS@transport.wa.gov.au\">WHS@transport.wa.gov.au</a>. " +
                                       "<div><br>Work Health and Safety</div>" +
                                       "<div>People and Culture</div>";

        public const string Approved = "<div>Hi,</div>" +
                                       "<div><br>Please be advised that Work Health and Safety Incident Report {2} has been investigated at a local level and has now progressed to the WHS Team for final investigation and completion.</div>" +
                                       "<div><br>Please click <a href=\"{1}\">here</a> to review the eForm.</div>" +
                                       "<div><br>If you require further information, please contact the WHS team by email at  <a href=\"mailto:WHS@transport.wa.gov.au\">WHS@transport.wa.gov.au</a>. " +
                                       "<div><br>This email is FYI only for recipients CCed in this email.</div>" +
                                       "<div><br>Work Health and Safety</div>" +
                                       "<div>People and Culture</div>";

        public const string RequestorManager = "<div>Hi,</div>" +
                                       "<div><br>{0} has submitted a Work Health and Safety Incident Report for your review.</div>" +
                                       "<div><br>Please click <a href=\"{1}\">here</a> to review and action the eForm.</div>" +
                                       "<div><br>If you require further information, please contact the WHS team by email at  <a href=\"mailto:WHS@transport.wa.gov.au\">WHS@transport.wa.gov.au</a>. " +
                                       "<div><br>This email is FYI only for recipients CCed in this email.</div>"+
                                       "<div><br>Work Health and Safety</div>" +
                                       "<div>People and Culture</div>";

        public const string RequestorManagerReminder = "<div>Hi,</div>" +
                                       "<div><br>{0} has submitted a Work Health and Safety Incident Report for your review.</div>" +
                                       "<div><br>Please click <a href=\"{1}\">here</a> to review and action the eForm.</div>" +
                                       "<div><br>If you require further information, please contact the WHS team by email at  <a href=\"mailto:WHS@transport.wa.gov.au\">WHS@transport.wa.gov.au</a>. " +
                                       "<div><br>Work Health and Safety</div>" +
                                       "<div>People and Culture</div>";

        public const string OnBehalf = "<div>Hi,</div>" +
                                       "<div><br>{0} has submitted has submitted a Work Health and Safety Incident Report on your behalf.</div>" +
                                       "<div><br>Please click <a href=\"{1}\">here</a> if you wish to review the eForm.</div>" +
                                       "<div><br>If you require further information, please contact the WHS team by email at  <a href=\"mailto:WHS@transport.wa.gov.au\">WHS@transport.wa.gov.au</a>. " +
                                       "<div><br>Work Health and Safety</div>" +
                                       "<div>People and Culture</div>";

        public const string AdditionalApprovalLineManager = "<div>Hi,</div>" +
                                       "<div><br>{0} has submitted a Work Health and Safety Incident Report for your review.</div>" +
                                       "<div><br>Please click <a href=\"{1}\">here</a> to review and action the eForm.</div>" +
                                       "<div><br>If you require further information, please contact the WHS team by email at  <a href=\"mailto:WHS@transport.wa.gov.au\">WHS@transport.wa.gov.au</a>. " +
                                       "<div><br>Work Health and Safety</div>" +
                                       "<div>People and Culture</div>";

        public const string Recalled = "<div>Hi {0},</div>" +
                                       "<div><br> This email confirms that you have recalled your Work Health and Safety Incident Report {2} and as a result the workflow has been terminated.</div>" +
                                       "<div><br>Please click <a href=\"{1}\">here</a> to access this eForm if you wish to review and re-submit.</div>" +
                                       "<div><br>If you require further information, please contact the WHS team by email at  <a href=\"mailto:WHS@transport.wa.gov.au\">WHS@transport.wa.gov.au</a>. " +
                                       "<div><br>Work Health and Safety</div>" +
                                       "<div>People and Culture</div>";

        public const string Delegated = "<div>Hi,</div>" +
                                       "<div><br> {0} has submitted a Work Health and Safety Incident Report that has now been delegated for your action.</div>" +
                                       "<div><br>Please click <a href=\"{1}\">here</a> to review and action the eForm.</div>" +
                                       "<div><br>If you require further information, please contact the WHS team by email at  <a href=\"mailto:WHS@transport.wa.gov.au\">WHS@transport.wa.gov.au</a>. " +
                                       "<div><br>Work Health and Safety</div>" +
                                       "<div>People and Culture</div>";

        public const string DelegatedManagerReminder = "<div>Hi,</div>" +
                               "<div><br> {0} has submitted a Work Health and Safety Incident Report that has now been delegated for your action.</div>" +
                               "<div><br>Please click <a href=\"{1}\">here</a> to review and action the eForm.</div>" +
                               "<div><br>If you require further information, please contact the WHS team by email at  <a href=\"mailto:WHS@transport.wa.gov.au\">WHS@transport.wa.gov.au</a>. " +
                               "<div><br>Work Health and Safety</div>" +
                               "<div>People and Culture</div>";

        public const string Rejected = "<div>Hi {0},</div>" +
                                       "<div><br> {1}  has rejected your Work Health and Safety Incident Report eForm {2}" +
                                       "<div><br>The following reason/s were given by {1} for taking that action: </div>" +
                                       "<div><br>{3} </div>" +
                                       "<div><br>Please click <a href=\"{4}\">here</a> to access this eForm if you wish to review and re-submit.</div>" +
                                       "<div><br>If you require further information, please contact the WHS team by email at  <a href=\"mailto:WHS@transport.wa.gov.au\">WHS@transport.wa.gov.au</a>. " +
                                       "<div><br>Work Health and Safety</div>" +
                                       "<div>People and Culture</div>";

        public const string Completed = "<div>Hi,</div>" +
                                       "<div><br> Please be advised that your Work Health and Safety Incident Report {0} has now been completed by the WHS Team.</div>" +
                                       "<div><br>Please click <a href=\"{1}\">here</a> to view the incident information.</div>" +
                                       "<div><br>If you require further information, please contact the WHS team by email at  <a href=\"mailto:WHS@transport.wa.gov.au\">WHS@transport.wa.gov.au</a>. " +
                                       "<div><br>This email is FYI only for recipients CCed in this email.</div>"+
                                       "<div><br>Work Health and Safety</div>" +
                                       "<div>People and Culture</div>";
        
        public const string PODEscalate = "<div>Hi,</div>" +
                                       "<div><br>{0} has submitted a Work Health and Safety Incident Report that has now escalated for your action.</div>" +
                                       "<div><br>Please click <a href=\"{1}\">here</a> to review and delegate the form to the correct approver.</div>" +
                                       "<div><br>If you require further information, please contact the WHS team by email at  <a href=\"mailto:WHS@transport.wa.gov.au\">WHS@transport.wa.gov.au</a>. " +
                                       "<div><br>Work Health and Safety</div>" +
                                       "<div>People and Culture</div>";

        public const string Escalated = "<div>Hi,</div>" +
                                       "<div><br>{0} has submitted a Work Health and Safety Incident Report for your review.</div>" +
                                       "<div><br>Please click <a href=\"{1}\">here</a> to review and action the eForm.</div>" +
                                       "<div><br>If you require further information, please contact the WHS team by email at  <a href=\"mailto:WHS@transport.wa.gov.au\">WHS@transport.wa.gov.au</a>. " +
                                       "<div><br>This email is FYI only for recipients CCed in this email.</div>" +
                                       "<div><br>Work Health and Safety</div>" +
                                       "<div>People and Culture</div>";
    }
}
