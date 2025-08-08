namespace eforms_middleware.Constants.NSHA;

public static class NonStandardHardwareAcquisitionRequestTemplate
{
    public const string EXPECTED_REQUESTER_SUBMIT_EMAIL_BODY = "<div>Hi,{0}</div><br/>" +
                                                               "<div>Thank you for submitting the Non-Standard Hardware Acquisition Request eForm {1}.</div><br/>" +
                                                               "<div>Your request has been sent to the Technology Service Delivery Group for review and action.</div><br/>" +
                                                               "<div>Please contact Procurement and Fleet Management on 6551 6888 or by email at <a href=\"mailto:PFMSystems@transport.wa.gov.au\">PFMSystems@transport.wa.gov.au</a> if you need further assistance.</div><br/>" +
                                                               "<div>Procurement and Fleet Management</div>" +
                                                               "<div>Finance and Procurement Services</div>";

    public const string EXPECTED_TECHNOLOGY_SERVICE_DELIVERY_GROUP_REVIEW_EMAIL_BODY = "<div>Hi,</div><br/>" +
        "<div>{0} has submitted a Non-Standard Hardware Acquisition Request eForm {1} for your review.</div><br/>" +
        "<div>Please click {2} to review and action the eForm.</div><br/>" +
        "<div>Please contact Procurement and Fleet Management on 6551 6888 or by email at <a href=\"mailto:PFMSystems@transport.wa.gov.au\">PFMSystems@transport.wa.gov.au</a> if you need further assistance.</div><br/>" +
        "<div>Procurement and Fleet Management</div>" +
        "<div>Finance and Procurement Services</div>";

    public const string EXPECTED_LEASE_ADMIN_GROUP_REVIEW_EMAIL_BODY = "<div>Hi,</div><br/>" +
                                                                       "<div>{0} has submitted a Non-Standard Hardware Acquisition Request eForm {1} for your review.</div><br/>" +
                                                                       "<div>Please {2} to review and action the eForm.</div><br/>" +
                                                                       "<div>Please contact Procurement and Fleet Management on 6551 6888 or by email at <a href=\"mailto:PFMSystems@transport.wa.gov.au\">PFMSystems@transport.wa.gov.au</a> if you need further assistance.</div><br/>" +
                                                                       "<div>Procurement and Fleet Management</div>" +
                                                                       "<div>Finance and Procurement Services</div>";

    public const string EXPECTED_COMPLETED_EMAIL_BODY = "<div>Hi,<div><br/>" +
                                                        "<div>Your Non-Standard Hardware Acquisition Request eForm {0} has now been approved and completed.</div><br/>" +
                                                        "<div>Please {1} to review your eForm.</div><br/>" +
                                                        "<div>Please contact Procurement and Fleet Management on 6551 6888 or by email at <a href=\"mailto:PFMSystems@transport.wa.gov.au\">PFMSystems@transport.wa.gov.au</a> if you need further assistance.</div><br/>" +
                                                        "<div>Procurement and Fleet Management</div>" +
                                                        "<div>Finance and Procurement Services</div>";

    public const string EXPECTED_REJECTED_EMAIL_BODY = "<div>Hi {0},<div><br/>" +
                                                       "<div>Your Non-Standard Hardware Acquisition Request eForm {1} has been rejected by {2}. The following reasons were given: {3}</div><br/>" +
                                                       "<div>Please {4} if you wish to review and re-submit the eForm.</div><br/>" +
                                                       "<div>Please contact Procurement and Fleet Management on 6551 6888 or by email at <a href=\"mailto:PFMSystems@transport.wa.gov.au\">PFMSystems@transport.wa.gov.au</a> if you need further assistance.</div><br/>" +
                                                       "<div>Procurement and Fleet Management</div>" +
                                                       "<div>Finance and Procurement Services</div>";

    public const string EXPECTED_RECALL_OWNER_BODY =
        "<div>Hi {0},</div><br/>" +
        "<div>The {2} Request eForm #{3} has been recalled by {1}.</div><br/>" +
        "<div>Please {4} to review and action the eForm.</div><br/> " +
        "<div>Please contact Procurement and Fleet Management on {4} if you need further assistance.</div> <br/>" +
        "<div>Please contact Procurement and Fleet Management on 6551 6888 or by email at <a href=\"mailto:PFMSystems@transport.wa.gov.au\">PFMSystems@transport.wa.gov.au</a> if you need further assistance.</div><br/>" +
        "<div>Procurement and Fleet Management</div>" +
        "<div>Finance and Procurement Services</div>";
}