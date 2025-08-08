using DoT.Infrastructure.DbModels.Entities;

namespace eforms_middleware.Specifications;

public class PositionSpecification : BaseQuerySpecification<AdfPosition>
{
    public PositionSpecification(int id, bool includeUsers = false)
    : base(x => x.Id == id, x => x.Id)
    {
        if (includeUsers)
        {
            AddInclude(x => x.AdfUserPositions);
        }
    }
}