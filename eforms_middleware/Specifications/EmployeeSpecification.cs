using System;
using DoT.Infrastructure.DbModels.Entities;

namespace eforms_middleware.Specifications;

public class EmployeeSpecification : BaseQuerySpecification<AdfUser>
{
    public EmployeeSpecification(Guid azureId)
    : base(x => x.ActiveDirectoryId == azureId, x => x)
    {
        AddInclude(x => x.Position);
        AddInclude(x => x.Position.ReportsPosition.AdfUserPositions);
    }
}