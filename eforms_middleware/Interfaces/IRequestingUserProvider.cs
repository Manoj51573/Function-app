using System.Threading.Tasks;
using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure.Interfaces;

namespace eforms_middleware.Interfaces;

public interface IRequestingUserProvider
{
    void SetRequestingUser(string user);
    Task<IUserInfo> GetRequestingUser();
}