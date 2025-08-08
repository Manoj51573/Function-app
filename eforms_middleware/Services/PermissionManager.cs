using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.Interfaces;
using eforms_middleware.Specifications;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace eforms_middleware.Services;

public class PermissionManager : IPermissionManager
{
    private readonly IRepository<FormPermission> _permissionRepo;
    private readonly IRequestingUserProvider _userProvider;
    private readonly ILogger<PermissionManager> _logger;

    public PermissionManager(IRepository<FormPermission> permissionRepo, IRequestingUserProvider userProvider, ILogger<PermissionManager> logger)
    {
        _permissionRepo = permissionRepo;
        _userProvider = userProvider;
        _logger = logger;
    }

    public async Task<IList<FormPermission>> GetRequestersFormPermissionsAsync(int formId)
    {
        var requester = await _userProvider.GetRequestingUser();
        var spec = new FormPermissionSpecification(formId: formId, permissionFlag: null, adId: requester.ActiveDirectoryId, addGroupMemberInfo: true, addPositionInfo: true);
        return await _permissionRepo.ListAsync(spec);
    }

    public async Task<int?> UpdateFormPermissionsAsync(int formId, List<FormPermission> permissions)
    {
        try
        {
            var updatedRecords = new List<FormPermission>();
            FormPermission result = null;
            var existingFormPermissionsSpec = new FormPermissionSpecification(formId);
            var existingFormPermissions = await _permissionRepo.ListAsync(existingFormPermissionsSpec);
            foreach (var permission in permissions)
            {
                var permissionsMatchingRequest = existingFormPermissions.Where(o => (!permission.PositionId.HasValue || o.PositionId == permission.PositionId) &&
                                                                        (!permission.GroupId.HasValue || o.GroupId == permission.GroupId) &&
                                                                        (!permission.UserId.HasValue || o.UserId == permission.UserId)).ToList();
                if (permissionsMatchingRequest.Any())
                {
                    UpdateExistingPermissions(permissionsMatchingRequest, (PermissionFlag)permission.PermissionFlag);
                    updatedRecords.AddRange(permissionsMatchingRequest);
                }
                else if (permission.UserId.HasValue || permission.PositionId.HasValue || permission.GroupId.HasValue)
                {
                    permission.FormId = formId;
                    result = await _permissionRepo.AddAsync(permission);
                    existingFormPermissions.Add(result);
                    updatedRecords.Add(result);
                }
            }
            // Set to view only
            UpdateExistingPermissions(existingFormPermissions.Except(updatedRecords));
            // permission exists not being passed to the updatedRecords
            await GiveRequesterPermanentViewPermissionAsync(formId, existingFormPermissions);
            return result?.Id;
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "Error thrown in {PermissionManagerName} for request with FormID {PermissionFormId}",
                nameof(PermissionManager), formId);
            throw;
        }
    }

    public async Task<IList<FormPermission>> GetPermissionsBySpecificationAsync(ISpecification<FormPermission> specification)
    {
        return await _permissionRepo.ListAsync(specification);
    }

    private void UpdateExistingPermissions(IEnumerable<FormPermission> oldPermissions, PermissionFlag permission = PermissionFlag.View)
    {
        foreach (var oldPermission in oldPermissions)
        {
            if (oldPermission.PermissionFlag == (byte)permission) continue;
            oldPermission.PermissionFlag = (byte)permission;
            _permissionRepo.Update(oldPermission);
        }
    }

    private async Task GiveRequesterPermanentViewPermissionAsync(int formId, IEnumerable<FormPermission> dbPermissions)
    {
        var requester = await _userProvider.GetRequestingUser();
        if (requester == null || dbPermissions.Any(p => p.UserId == requester.ActiveDirectoryId)) return;
        FormPermission userPermission = new()
        {
            FormId = formId,
            PermissionFlag = (byte)PermissionFlag.View,
            UserId = requester.ActiveDirectoryId
        };
        await _permissionRepo.AddAsync(userPermission);
    }

    public async Task RemoveAllPremissonOnReject(int formId
        ,bool removeOwner = false)
    {        
        try
        {
            var existingFormPermissionsSpec = new FormPermissionSpecification(formId);
            var dt = await _permissionRepo.ListAsync(existingFormPermissionsSpec);

            dt.ForEach(x =>
            { 
                if (!x.IsOwner || removeOwner)
                {
                    _permissionRepo.Delete(x);
                }
            });
        }
        catch (Exception ex)
        {

        }
    }

    public async Task<int?> SetAdHocPermission(FormPermission permission)
    {
        var data = await _permissionRepo.AddAsync(permission);
        return data.Id;
    }

    public async Task<List<FormPermission>> GetActionPermission(int formId)
    {
        return await _permissionRepo.FindByAsync(x => x.FormId == formId
            && x.PermissionFlag == (byte)PermissionFlag.UserActionable);
    }

    public async Task<List<FormPermission>> GetAllPermission(int formId)
    {
        return await _permissionRepo.FindByAsync(x => x.FormId == formId);
    }
}