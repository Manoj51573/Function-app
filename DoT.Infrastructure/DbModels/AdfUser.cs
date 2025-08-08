using System.Collections.Generic;
using System.Linq;
using DoT.Infrastructure.Interfaces;

namespace DoT.Infrastructure.DbModels.Entities;

public partial class AdfUser
{
    public string EmployeeFullName => string.Join(' ', EmployeePreferredName, EmployeeSurname);
    public IUserInfo ToUserInfo(AdfPosition managers = null, AdfPosition executiveDirectors = null)
    {
        return new EmployeeDetailsDto
        {
            ActiveDirectoryId = ActiveDirectoryId,
            EmployeeEmail = EmployeeEmail,
            EmployeeFirstName = EmployeeFirstName,
            EmployeePreferredName = EmployeePreferredName,
            EmployeeSurname = EmployeeSurname,
            EmployeeNumber = EmployeeNumber,
            EmployeePositionNumber = Position?.PositionNumber,
            EmployeePositionId = PositionId,
            EmployeePositionTitle = Position?.PositionTitle,
            Directorate = Position?.Directorate,
            Department = Position?.Department,
            Branch = Position?.Branch,
            Section = Position?.Section,
            Unit = Position?.Unit,
            EmployeeManagementTier = Position?.ManagementTier,
            Managers = managers?.ToUserInfo() ?? new List<IUserInfo>(),
            ExecutiveDirectors = executiveDirectors?.ToUserInfo() ?? new List<IUserInfo>()
        };
    }
}