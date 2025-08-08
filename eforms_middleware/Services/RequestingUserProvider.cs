using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure.Interfaces;
using eforms_middleware.Interfaces;
using eforms_middleware.Specifications;

namespace eforms_middleware.Services;

public class RequestingUserProvider : IRequestingUserProvider
{
    private readonly IEmployeeService _employeeService;
    private string _username;
    private IUserInfo _requestingUser;

    public RequestingUserProvider(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    public void SetRequestingUser(string user)
    {
        _username = user;
    }

    public async Task<IUserInfo> GetRequestingUser()
    {
        if (_requestingUser is not null) return _requestingUser;

        if (string.IsNullOrWhiteSpace(_username)) return null;
        var upperUserName = _username.ToUpper();
        _requestingUser = await _employeeService.GetEmployeeByEmailAsync(upperUserName);
        return _requestingUser;
    }
}