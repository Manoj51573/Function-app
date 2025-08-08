using DoT.Infrastructure.DbModels.Entities;

namespace eforms_middleware.Specifications;

public class EmployeesManagerAndEdSpecification : BaseQuerySpecification<AdfPosition>
{
    public EmployeesManagerAndEdSpecification(int positionId, string directorate, bool hasExecutiveDirector)
    : base(position => position.Id == positionId || (position.Directorate == directorate && position.ManagementTier == 3 && hasExecutiveDirector),
        position => position.Id)
    {
        AddInclude(x => x.AdfUserPositions);
    }
}