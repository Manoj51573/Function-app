namespace eforms_middleware.Constants.OPR
{ 

    public static class OPREmailTemplate
    {

        public const string SCARFGroup = "<div>Hi DigiComms Team,</div>" +
                                       "<div>SCARF - {0} has submitted. View the <a href=\"{1}\">request</a>.</div>";

        public const string SCARFRequestor = "<div>Hi {0},</div>" +
                                       "<div><br>Thank you for submitting a SCARF - {1}.</div>" +
                                       "<div><br>Your request has been sent to the Digital Communications Team and placed in the work queue. You can view the status of your request via the <a href=\"{3}\">Forms home</a>  or go to the <a href=\"{2}\">form</a>  to withdraw your request.</div>" +
                                       "<div><br>Time-critical requests are given priority. For any queries, please email <a href=\"mailto:scarf@transport.wa.gov.au\">SCARF</a>  (including the SCARF request number) or contact the Manager, Digital Communications, {5} at <a href=\"mailto:{4}\">{4}</a>.</div>" +
                                       "<div><br>Further information on the SCARF process and the Digital Content Publishing Policy and Procedure is available on <a href=\"https://transporta/\">Transporta</a>.</div>" +
                                       "<div><br>This email is FYI only for recipients CCed in this email.</div>" +
                                       "<div><br><br>Regards,</div>" +
                                       "<div><strong>Digital Communications Team</strong></div>" +
                                       "<div>Office of the Director General</div>";

        public const string SCARFRecalled = "<div>Hi {0},</div>" +
                                      "<div><br>This email confirms that you have recalled  your SCARF - {1} <a href=\"{2}\">#{3}</a>.</div>" +
                                      "<div><br>You can view your <a href=\"{2}\">recalled  request</a> but you will not be able to edit or reopen the request. A new SCARF can be submitted via the <a href=\"{4}\">form</a>.</div>" +
                                      "<div><br>For any queries, please email <a href=\"mailto:scarf@transport.wa.gov.au\">SCARF</a> (including the SCARF request number) or contact the Manager, Digital Communications, {6} at <a href=\"mailto:{5}\">{5}</a>.</div>" +
                                      "<div><br>Further information on the SCARF process and the Digital Content Publishing Policy and Procedure is available on <a href='https://transporta/'>Transporta</a>.</div>" +
                                      "<div><br>This email is FYI only for recipients CCed in this email.</div>" +
                                      "<div><br>Regards,</div>" +
                                      "<div><strong>Digital Communications Team</strong></div>" +
                                      "<div>Office of the Director General</div>";
        
        public const string SCARFRejected = "<div>Hi {0},</div>" +
                                      "<div><br>Your SCARF - {1} <a href=\"{2}\">#{3}</a> has been cancelled. Please see below for further information about the cancellation:</div>" +
                                      "<div><br>{5}</div>" +
                                      "<div><br>You can view your <a href=\"{2}\">cancelled request</a> but you will not be able to edit or reopen the request. A new SCARF can be submitted via the <a href=\"{4}\">form</a>.</div>" +
                                      "<div><br>For any queries, please email <a href=\"mailto:scarf@transport.wa.gov.au\">SCARF</a> (including the SCARF request number) or contact the Manager, Digital Communications, {7} at <a href=\"mailto:{6}\">{6}</a>.</div>" +
                                      "<div><br>Further information on the SCARF process and the Digital Content Publishing Policy and Procedure is available on <a href='https://transporta/'>Transporta</a>.</div>" +
                                      "<div><br>This email is FYI only for recipients CCed in this email.</div>" +
                                      "<div><br>Regards,</div>" +
                                      "<div><strong>Digital Communications Team</strong></div>" +
                                      "<div>Office of the Director General</div>";
        
        public const string SCARFCompleted = "<div>Hi {0},</div>" +
                                      "<div><br>Your SCARF - {1} <a href=\"{2}\">#{3}</a> has been completed. Please see below for further information about your closed request:</div>" +
                                      "<div><br>{5}</div>" +
                                      "<div><br>You can <a href=\"{2}\">view your completed request</a> or visit the <a href=\"{4}\">form homepage</a>  to see your submitted forms.</div>" +
                                      "<div><br>For any queries, please email <a href=\"mailto:scarf@transport.wa.gov.au\">SCARF</a> (including the SCARF request number) or contact the Manager, Digital Communications, {7} at <a href=\"mailto:{6}\">{6}</a>.</div>" +
                                      "<div><br>Further information on the SCARF process and the Digital Content Publishing Policy and Procedure is available on <a href='https://transporta/'>Transporta</a>.</div>" +
                                      "<div><br>This email is FYI only for recipients CCed in this email.</div>" +
                                      "<div><br>Regards,</div>" +
                                      "<div><strong>Digital Communications Team</strong></div>" +
                                      "<div>Office of the Director General</div>";
    }
}
