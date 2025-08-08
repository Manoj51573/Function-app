using eforms_middleware.DataModel;
using System.Threading.Tasks;

namespace eforms_middleware.Interfaces
{
    public interface IRecruitmentEformService
    {
        Task<RequestResult> BusinessCaseNonAdvCreateOrUpdate(BusinessCaseNonAdvHttpRequest businessCaseNonAdvHttpRequest);
    }
}
