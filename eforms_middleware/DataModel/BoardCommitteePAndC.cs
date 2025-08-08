using eforms_middleware.Constants;
using System;
using System.Collections.Generic;

namespace eforms_middleware.DataModel;

public class BoardCommitteePAndC : CoIForm
{
    public GbcEmployeeForm EmployeeForm { get; set; } = new();
}

public class GbcEmployeeForm
{
    public ConflictOfInterest.EngagementFrequency? EngagementFrequency { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string GovernmentBoardCommitteeName { get; set; }
    public ConflictOfInterest.EngagementRenumeration? PaidOrVoluntary { get; set; }
    public ConflictOfInterest.ConflictType? ConflictType { get; set; }
    public string DescriptionOfConflict { get; set; }
    public string ReasonsToSupport { get; set; }
    public string ProposedPlan { get; set; }
    public bool? IsDeclarationAcknowledged { get; set; }
    public string AdditionalComments { get; set; }
    public bool IsRequestOnBehalf { get; set; }
    public IList<UserIdentifier> requestOnBehalf { get; set; } = new List<UserIdentifier>();
}