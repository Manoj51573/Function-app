using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;

namespace eforms_middleware.Interfaces;

public interface IPermissionManager
{
    Task<IList<FormPermission>> GetRequestersFormPermissionsAsync(int formId);
    Task<int?> UpdateFormPermissionsAsync(int formId, List<FormPermission> formPermission);
    Task<IList<FormPermission>> GetPermissionsBySpecificationAsync(ISpecification<FormPermission> specification);
    Task RemoveAllPremissonOnReject(int formId, bool removeOwner = false);
    Task<int?> SetAdHocPermission(FormPermission permission);
    Task<List<FormPermission>> GetActionPermission(int formId);
    Task<List<FormPermission>> GetAllPermission(int formId);
}