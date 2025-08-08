using System.Collections.Generic;
using System.Threading.Tasks;
using DoT.Infrastructure.DbModels;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.DataModel;

namespace eforms_middleware.Interfaces;

public interface IFormInfoService
{
    Task<IEnumerable<UsersFormStoredProcedureDto>> GetUsersFormsAsync(string userId);
    IEnumerable<RefDirectorate> GetAllDirectorate();
    IEnumerable<AllForm> GetAllFormTypes();
    IEnumerable<RefFormStatus> GetAllFormStatuses();
    Task<FormDetails> GetFormByIdAsync(int formInfoId);
    Task<FormInfo> GetFormInfoByIdAsync(int formInfoId);
    Task<FormInfo> GetExistingOrNewFormInfoAsync(FormInfoUpdate request, bool addAttachments = false);
    Task<FormInfo> SaveFormInfoAsync(FormInfoUpdate request, FormInfo dbRecord);
    Task<FormInfo> SaveLeaveFormsInfoAsync(FormInfoUpdate request, FormInfo dbRecord);
}