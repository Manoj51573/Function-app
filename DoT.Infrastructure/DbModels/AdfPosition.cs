using System.Collections.Generic;
using System.Linq;
using DoT.Infrastructure.Interfaces;

namespace DoT.Infrastructure.DbModels.Entities;

public partial class AdfPosition
{
    public List<IUserInfo> ToUserInfo()
    {
        return AdfUserPositions.Where(x => x.ActiveRecord == true)
            .Select(x => new EmployeeDetailsDto
        {
            ActiveDirectoryId = x.ActiveDirectoryId,
            EmployeeEmail = x.EmployeeEmail,
            EmployeePreferredName = x.EmployeePreferredName,
            EmployeeFirstName = x.EmployeeFirstName,
            EmployeeSurname = x.EmployeeSurname,
            EmployeeNumber = x.EmployeeNumber,
            EmployeePositionId = Id,
            EmployeePositionTitle = PositionTitle,
            Directorate = Directorate,
            Department = Department,
            Branch = Branch,
            Section = Section,
            Unit = Unit,
            EmployeeManagementTier = ManagementTier
        } as IUserInfo).ToList();
    }
}