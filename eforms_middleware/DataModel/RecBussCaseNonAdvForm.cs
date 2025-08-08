using System;
using System.Collections.Generic;

namespace eforms_middleware.DataModel
{
    public class BusinessCaseNonAdvHttpRequest
    {
        public FormDetails FormDetails { get; set; }
        public string FormAction { get; set; }
        public string AdditionalInfo { get; set; }
        public FormDetailsRequest ToFormDetailRequest() => new FormDetailsRequest
        {
            AllFormsId = FormDetails.AllFormsID,
            FormInfoId = FormDetails.FormInfoID,
            NextApprover = FormDetails.NextApprover,
            Response = FormDetails.Response
        };
    }

    public class BusCaseNonAdvForm
    {
        public BusCaseNonAdvPosition PositionDetail { get; set; }
        public BusCaseNonAdvJdfReview JdfReview { get; set; }
        public BusCaseNonAdvRationale BusinessCaseRationale { get; set; }
        public BusCaseNonAdvEngagement Engagement { get; set; }
        public BusCaseNonAdvContactPerson ContactPerson { get; set; }
        public BusCaseNonAdvAdditionalInfo AdditionalInfo { get; set; }
        public string ReasonForDecision { get; set; } = string.Empty;
    }


    public class BusCaseNonAdvPosition
    {
        public string PositionNo { get; set; }
        public string PositionTitle { get; set; }
        public string Branch { get; set; }
        public string PositionFTE { get; set; }
        public string Directorate { get; set; }
        public string PositionStatus { get; set; }
        public string Classification { get; set; }
        public DateTime? PositionCreated { get; set; }
        public DateTime? VacantSince { get; set; }
        public DateTime? PositionEndDate { get; set; }
        public string Location { get; set; }
        public string ReportsTo { get; set; }
    }

    public class BusCaseNonAdvJdfReview
    {
        public bool? IsPoolRequirement { get; set; }
        public bool? JobDescRequiredChanges { get; set; }
    }

    public class BusCaseNonAdvRationale
    {
        public string BusinessCase { get; set; }
        public string ContigencyPlanReason { get; set; }
        public string PositionFunded { get; set; }
    }

    public class BusCaseNonAdvEngagement
    {
        public string EngagementReason { get; set; }
        public string EngagementType { get; set; }
        public string AppointmentDuration { get; set; }
        public string AdvertisingType { get; set; }
        public string AdvertisingRequirement { get; set; }
    }
    public class BusCaseNonAdvContactPerson
    {
        public string HiringManager { get; set; }
        public bool? IsExternalContact { get; set; }
        public string InternalContactPerson { get; set; }
        public string ExternalContactEmail { get; set; }
        public string ExternalContactFullname { get; set; }
    }

    public class BusCaseNonAdvAdditionalInfo
    {
        public IList<UserIdentifier> AdditionalRecipients { get; set; } = new List<UserIdentifier>();
        public IList<AttachmentResult> Attachments { get; set; } = new List<AttachmentResult>();
        public string AdditionalComment { get; set; }
    }
}
