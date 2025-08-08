using DoT.Infrastructure.DbModels.Entities;

namespace eforms_middleware.Specifications;

public class UserSpecification : BaseQuerySpecification<AdfUser>
{
    public UserSpecification(string username)
    : base(x => x.EmployeeEmailNormalized == username, x => x.IngestedAtUtc, orderDescending: true)
    {
        AddInclude(x => x.Position);
        AddInclude(x => x.Position.ReportsPosition.AdfUserPositions);
    }
}