using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Specifications;

namespace eforms_middleware.Services;

public class CoiOtherEscalationService : EscalationManagerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IPermissionManager _permissionManager;

    public CoiOtherEscalationService(IEmployeeService employeeService, IPermissionManager permissionManager)
    {
        _employeeService = employeeService;
        _permissionManager = permissionManager;
    }

    public override async Task<EscalationResult> EscalateFormAsync(FormInfo originalForm)
    {
        var specification = new FormPermissionSpecification(formId: originalForm.FormInfoId, addGroupMemberInfo: true, addPositionInfo: true);
        var permissions = await _permissionManager.GetPermissionsBySpecificationAsync(specification);
        var ownerPermission = permissions.Single(x => x.IsOwner);
        var currentApprover = permissions.Single(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);
        var employeeInfo = await _employeeService.GetEmployeeByAzureIdAsync(ownerPermission.UserId!.Value);
        FormPermission permission = null;
        // If the current approver is at ED level then we can only assign to the group as manager is missing
        if (currentApprover.PositionId.HasValue && currentApprover.Position.ManagementTier is < 4)
        {
            permission = new() { GroupId = ConflictOfInterest.ODG_AUDIT_GROUP_ID, PermissionFlag = (byte)PermissionFlag.UserActionable };
            originalForm.NextApprover = ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL;
            originalForm.NextApprovalLevel = "ODG Audit Group";
            originalForm.FormStatusId = 5;
            originalForm.FormSubStatus = "Endorsed";
        }
        else
        {
            permission = new() { PositionId = employeeInfo.ExecutiveDirectorPositionId, PermissionFlag = (byte)PermissionFlag.UserActionable };         
            originalForm.NextApprover = employeeInfo.ExecutiveDirectorIdentifier;
            originalForm.NextApprovalLevel = $"Executive Director {employeeInfo.Directorate}";
        }
        

        return new EscalationResult() { UpdatedForm = originalForm, PermissionUpdate = permission };
    }

    public async Task<bool> DoesEscalate(FormInfo form)
    {
        var specification = new FormPermissionSpecification(formId: form.FormInfoId,
            permissionFlag: (byte)PermissionFlag.UserActionable);
        var actioningPermission = await _permissionManager.GetPermissionsBySpecificationAsync(specification);
        return actioningPermission.Single().GroupId != ConflictOfInterest.ODG_AUDIT_GROUP_ID;
    }
}