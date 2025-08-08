using System;
using DoT.Infrastructure.DbModels.Entities;

namespace DoT.Infrastructure.Interfaces;

public interface IApprovalInfo
{
    public string NextApprover { get; set; }
    public int? PositionId { get; set; }
    public Guid? GroupId { get; set; }
}