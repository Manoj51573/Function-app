using System.Linq;
using DoT.Infrastructure.DbModels.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;

namespace eforms_middleware.Specifications;

public class EmployeeSearchSpecification : BaseQuerySpecification<AdfUser>
{
    public EmployeeSearchSpecification(string searchQuery, bool includeContractors = false, int? take = null, int? skip = null)
    : base(x => (EF.Functions.Like(x.EmployeeEmail, $"%{searchQuery}%") ||
                EF.Functions.Like(x.EmployeeFirstName, $"%{searchQuery}%") ||
                EF.Functions.Like(x.EmployeeSurname, $"%{searchQuery}%")) &&
                (includeContractors || x.Position != null), x => x.EmployeeEmail, take: take, skip: skip)
    {
        AddInclude(x => x.Position);
    }
}