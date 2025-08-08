using DoT.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace eforms_middleware.Interfaces;

public interface IEmployeeService
{
    Task<IUserInfo> GetEmployeeDetailsAsync(string employeeEmail);
    Task<IList<IUserInfo>> GetEmployeeByPositionNumberAsync(int positionNumber);
    Task<IUserInfo> GetEmployeeByAzureIdAsync(Guid azureId);
    Task<IUserInfo> GetEmployeeByEmailAsync(string email);
    Task<IList<IUserInfo>> FindEmployeesAsync(IQueryCollection query);
    Task<bool> IsPodGroupUser(Guid azureId);
    Task<bool> IsOdgGroupUser(Guid azureId);
    Task<bool> IsPodUserGroupEmail(string email);
}