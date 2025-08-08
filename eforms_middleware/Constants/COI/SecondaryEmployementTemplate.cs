namespace eforms_middleware.Constants.COI
{
    public static class SecondaryEmployementTemplate
    {
        public static string RequestorManager = "<div>Dear {0}</div><br/>" +
                                                "<div>A conflict of interest declaration regarding secondary employment has been submitted by {1}</div><br/>" +
                                                "<div>Please {2} to review the declaration.</div><br/>" +
                                                "<div>If you have any further questions please contact the Employee Services Branch on (08) 6551 6888, or via email employeeservices@transport.wa.gov.au.</div>";

        public static string Tier3Escalation = "<div>Dear {0}</div><br/>" +
                                    "<div>A conflict of interest declaration regarding secondary employment has been submitted by {1}. This task was escalated as there was no action taken by {2}.</div></br/>" +
                                    "<div>Please {3} to review the declaration.</div><br/>" +
                                    "<div>Once your application has been assessed you will receive further email communications. If you have any further questions please contact the Employee Services Branch on (08) 6551 6888, or via email employeeservices@transport.wa.gov.au.</div>";

        public static string EndWorkFlow = "<div>Dear {0}</div><br/>" +
                                    "<div>No Tier 3 was found for approvel. Workflow has been ended.</div></br/>" +
                                    "<div>Please {1} to review the declaration.</div><br/>" +
                                    "<div>If you have any further questions please contact the Employee Services Branch on (08) 6551 6888, or via email employeeservices@transport.wa.gov.au.</div>";

        public static string Tier3 = "<div>Dear {0}</div><br/>" +
                                   "<div>A conflict of interest declaration regarding secondary employment has been submitted by {1}.</div></br/>" +
                                   "<div>Please {2} to review the declaration.</div><br/>" +
                                   "<div>Once your application has been assessed you will receive further email communications. If you have any further questions please contact the Employee Services Branch on (08) 6551 6888, or via email employeeservices@transport.wa.gov.au.</div>";


        public static string Reminder = "<div>Dear {0}</div><br/>" +
                                        "<div>Your declaration completed on {1} has now passed it's 12 month valid period.</div><br/>" +
                                        "<div>Please submit a new Secondary employment declaration if applicable or else no further action is required. </div><br/>" +
                                        "<div>Should you require any additional information in relation to this matter, please contact the Employee Services Branch on (08) 6551 6888, or via email employeeservices@transport.wa.gov.au</div>";

    }
}
