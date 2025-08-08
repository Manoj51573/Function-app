using System.Collections.Generic;
using System.Linq;
using DoT.Infrastructure.Interfaces;

namespace eforms_middleware.DataModel;

public static class UserInfoListHelpers
{
    public static string GetDescription(this IList<IUserInfo> users)
    {
        return users.Count == 1 ? users[0].EmployeeEmail : users.FirstOrDefault()?.EmployeePositionTitle;
    }

    public static string GetSalutation(this IList<IUserInfo> users)
    {
        return users.Count == 1 ? users[0].EmployeePreferredFullName : users.FirstOrDefault()?.EmployeePositionTitle;
    }
}