using eforms_middleware.Constants;
using System;
using System.Collections.Generic;

namespace eforms_middleware.DataModel;

public class CoIOther : CoIForm, ICoiForm<CoiOtherGovOdgReview>
{
    public CoIOtherRequesterForm EmployeeForm { get; set; } = new();
    public CoiOtherGovOdgReview FinalApprovalForm { get; set; }
    public bool IsRequestOnBehalf { get; set; }
    public IList<UserIdentifier> requestOnBehalf { get; set; } = new List<UserIdentifier>();
}

public class CoIOtherRequesterForm
{
    public ConflictOfInterest.ConflictType? ConflictType { get; set; }
    public ConflictOfInterest.EngagementFrequency? PeriodOfConflict { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string DeclarationDetails { get; set; }
    public string ProposedPlan { get; set; }
    public string AdditionalInformation { get; set; }
    public bool? IsDeclarationAcknowledged { get; set; }
    public IList<AttachmentResult> Attachments { get; set; }
    public bool IsRequestOnBehalf { get; set; }
    public IList<UserIdentifier> requestOnBehalf { get; set; } = new List<UserIdentifier>();
}

public class CoiOtherGovOdgReview : CoiEndorsementForm
{
    public ConflictOfInterest.NatureOfConflict? NatureOfConflict { get; set; }
    public ConflictOfInterest.CoiArea? CoiArea { get; set; }
    public string AreaInformation { get; set; }
    public string ObjFileRef { get; set; }
    public DateTime? ReminderDate { get; set; }
    public IList<AttachmentResult> Attachments { get; set; }
}