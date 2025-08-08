namespace eforms_middleware.Constants.COI
{
    public static partial class GBHEmailTemplate
    {
        public static string Submitted = "{0}" +
                                        "<br/><div>A conflict of interest declaration regarding the offer of a gift, benefit or hospitality has been submitted by your employee.</div>" +
                                        "<br/><div>Please {1} to review the declaration.</div>" +
                                        "<br/><div>Further information about conflicts of interest relating to gifts, benefits and hospitality is available on <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or Governance and Audit via ODGGovernanceandAudit@transport.wa.gov.au or (08) 6551 7083.</div>";

        public static string RequestorManagerNotify = "<div>Dear {0}</div>" +
                                                        "<br/><div>A conflict of interest declaration regarding the offer of a gift, benefit or hospitality has been submitted and requires your review and action.</div>" +
                                                        "<br/><div>Please {1} to review the declaration.</div>" +
                                                        "<br/><div>Further information about conflicts of interest relating to gifts, benefits and hospitality is available on <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or Governance and Audit via ODGGovernanceandAudit@transport.wa.gov.au or (08) 6551 7083.</div>";


        public static string Tier3 = "<div>Dear {0}</div>" +
                                    "<br/><div>A conflict of interest declaration has been submitted in relation to the offer of a gift, benefit or hospitality and has been approved by the line manager.</div>" +
                                    "<br/><div>Please {1} to review the declaration.</div>" +
                                    "<br/><div>Further information about conflicts of interest relating to gifts, benefits and hospitality is available on <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or Governance and Audit via ODGGovernanceandAudit@transport.wa.gov.au or (08) 6551 7083.</div>";

        public const string TIER_3_ESCALATED = 
            "<div>Dear {0}</div>" +
            "<br/><div>A conflict of interest declaration regarding the offer of a gift, benefit or hospitality was " +
            "submitted by {1}. The declaration has been escalated to you as there was no action by line manager {2}.</div>" +
            "<br/><div>Please {3} to review the declaration.</div>" +
            "<br/><div>Further information about conflicts of interest relating to gifts, benefits and hospitality is " +
            "available on <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or Governance and Audit via " +
            "<a href=\"mailto:ODGGovernanceandAudit@transport.wa.gov.au\">ODGGovernanceandAudit@transport.wa.gov.au</a>" +
            " or (08) 6551 7083.</div>";

        public static string GovAudit = "<div>Dear Executive Director Office of the Director General</div>" +
                                        "<br/><div>A conflict of interest declaration has been received regarding the offer of a gift, benefit or hospitality.</div>" +
                                        "<br/><div>The declaration has been reviewed and was approved by {0}.</div>" +
                                        "<br/><div>Please {1} to review the form for quality assurance purposes.</div>";

        public static string GovAuditEscalated = "<div>Dear Executive Director Office of the Director General</div>" +
                                        "<br/><div>A conflict of interest declaration has been received regarding the offer of a gift, benefit or hospitality. This task notification was escalated as there was no action by {0}</div>" +                                        
                                        "<br/><div>Please {1} to review the form for quality assurance purposes.</div>";


        public static string Footer = "<br/><div>Further information about conflicts of interest relating to gifts, benefits and hospitality is available on <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or Governance and Audit via ODGGovernanceandAudit@transport.wa.gov.au or (08) 6551 7083.</div>";
    }
}
