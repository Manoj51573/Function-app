namespace eforms_middleware.Constants.COI
{
    public static class RecruitmentEmailTemplate
    {
        public static string PanelChair = "<div>Dear {0}</div>" +
                "<br/><div>A conflict of interest has been declared by one of the Panel Members for the above recruitment process and requires your review and action.</div>" +
                "<br/><div>Please {1} to review the declaration. </div>" +
                "<br/><div>Further information about conflicts of interest relating to recruitment is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or via your Recruitment Advisor.</div>" +
                "<br/><div>Thank You</div>" +
                "<br/><div>The Recruitment Team</div>";

        public static string RAMSAccess = "<div>Dear {0}</div>" +
                                    "<br/><div>Thank you for completing the conflict of interest eForm where you have confirmed that you do not have a conflict of interest regarding this recruitment process.<div>" +
                                    "<br/><div>You will shortly receive an email from the Recruitment Team containing information on how to log into the Recruitment Advertising Management System (RAMS) to access the applications.<div>" +
                                    "<br/><div>Thank You</div>" +
                                    "<br/><div>The Recruitment Team</div>";

        public static string PanelMember = "<div>Dear {0}</div>" +
                "<br/><div>A Conflict of Interest Declaration has been submitted by one of the panel members in relation to the above recruitment process and requires your review and action.</div>" +
                "<br/><div>Please contact the Chair Person and the other Panel Member/s to discuss this declaration and management strategies to be put in place.</div>" +
                "<br/><div>Should you require any clarification on this process, please contact your Recruitment Advisor on 6551 6888.</div>" +
                "<br/><div>Please {1} to review the declaration.</div>" +
                "<br/><div>Thank You</div>" +
                "<br/><div>The Recruitment Team</div>";

        public static string ExternalPanelMember = "<div>Dear {0}</div>" +
                                            "<br/><div>A conflict of interest declaration has been submitted by one of the panel members in relation to the above selection process.</div>" +
                                            "<br/><div>Please contact the Chair Person and the other Panel Member/s to discuss this declaration and the management strategies to be put in place.</div>" +
                                            "<br/><div>Should you require any clarification on this process, please contact your Recruitment Advisor on 6551 6888.</div>" +
                                            "<br/><div>Please {1} to review the declaration. </div>" +
                                            "<br/><div>Thank You</div>" +
                                            "<br/><div>The Recruitment Team</div>";

        public static string NotEndorsed = "<div>Dear {0}</div>" +
                                            "<br/><div>The conflict of interest declaration submitted in relation to the above recruitment process has not been endorsed by either the Chairperson or the Executive Director People and Culture.</div>" +
                                            "<br/><div>Please contact either the Chairperson or the Recruitment Advisor to discuss further.</div>" +
                                            "<br/><div>Thank You</div>" +
                                            "<br/><div>The Recruitment Team</div>";

        public static string RecruitmentAdvisor = "<div>Dear Recruitment Advisor<div>" +
                "<br/><div>A conflict of interest declaration eForm has been submitted for the above recruitment process, has been endorsed by the Panel Chair and now requires your review and action.</div>" +
                "<br/><div>Please {0} to view the form and relevant management strategy.</div>" +
                "<br/><div>Thank You</div>" +
                "<br/><div>The Recruitment Team</div>";

        public static string RecruitmentAdvisorNotify = "<div>Dear Recruitment Advisor</div>" +
                                                    "<br/><div>A conflict of interest declaration eForm task has been assigned to {0} but has not been actioned for 5 days. Please liaise with {0} to ensure that this task is actioned in a timely manner.</div>" +
                                                    "<br/><div>Thank you</div>" +
                                                    "<br/><div>The Recruitment Team</div>";

        public static string EDPeopleCulture = "<div>Dear {0}</div>" +
                                            "<br/><div>A conflict of interest declaration has been submitted for the above recruitment process and now requires your review and action.</div>" +
                                            "<br/><div>The Panel Members have discussed the declared conflict in consultation with the Recruitment Advisor Team and put in place relevant management strategies.</div>" +
                                            "<br/><div>Please {1} to view the form and approve accordingly.</div>" +
                                            "<br/><div>Thank You</div>" +
                                            "<br/><div>The Recruitment Team</div>";

        public static string Completed = "Dear Panel Members and Recruitment Advisor Team" +
                                        "<br/><div>The Executive Director People and Culture has approved the management strategies to be put in place in relation to the declared conflict of interest for the above recruitment process.</div>" +
                                        "<br/><div>The recruitment and selection process can now proceed with all Panel Members being given access to the applications.</div>";


        public static string NotificationToAdvisor = "Dear Recruitment Advisor" +
                                        "<br/><div>A conflict of interest declaration eForm task has been assigned to {0} but have not been actioned for 5 days.</div>" +                                        
                                        "<br/><div>Thank You</div>" +
                                        "<br/><div>The Recruitment Team</div>";
    }
}
