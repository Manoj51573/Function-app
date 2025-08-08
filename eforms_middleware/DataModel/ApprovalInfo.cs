using System;
using DoT.Infrastructure.Interfaces;

namespace eforms_middleware.DataModel;

public class ApprovalInfo : IApprovalInfo
{
    public string NextApprover { get; set; }
    public int? PositionId { get; set; }
    public Guid? GroupId { get; set; }
}