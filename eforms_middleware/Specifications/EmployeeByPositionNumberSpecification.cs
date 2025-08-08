using DoT.Infrastructure.DbModels.Entities;

namespace eforms_middleware.Specifications;

public class EmployeeByPositionNumberSpecification : BaseQuerySpecification<AdfUser>
{
    public EmployeeByPositionNumberSpecification(int positionId)
    : base(u => u.PositionId == positionId,
        x => x)
    {
        AddInclude(x => x.Position);
    }
}