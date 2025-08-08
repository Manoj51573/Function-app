using System;
using System.Linq;
using System.Threading.Tasks;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Specifications;
using Microsoft.Extensions.Logging;

namespace eforms_middleware.Services;

public class CprEscalationManager : EscalationManagerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IPermissionManager _permissionManager;
    private readonly ILogger<CprEscalationManager> _logger;
    private readonly IPositionService _positionService;

    public CprEscalationManager(IEmployeeService employeeService, IPermissionManager permissionManager,
        ILogger<CprEscalationManager> logger, IPositionService positionService)
    {
        _employeeService = employeeService;
        _permissionManager = permissionManager;
        _logger = logger;
        _positionService = positionService;
    }
    
    public override async Task<EscalationResult> EscalateFormAsync(FormInfo originalForm)
    {
        try
        {
            FormPermission permission = null;
            var specification = new FormPermissionSpecification(formId: originalForm.FormInfoId, addUserInfo: true);
            var permissions = await _permissionManager.GetPermissionsBySpecificationAsync(specification);
            var ownerPermission = permissions.Single(x => x.IsOwner);
            var approvalPermission = permissions.Single(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);
            var employeeInfo = await _employeeService.GetEmployeeByAzureIdAsync(ownerPermission.UserId!.Value);
            if (approvalPermission.PositionId.HasValue && approvalPermission.PositionId == employeeInfo.ExecutiveDirectorPositionId)
            {
                var position =
                    await _positionService.GetPositionByIdAsync(ConflictOfInterest
                        .ED_PEOPLE_AND_CULTURE_POSITION_ID);
                permission = new()
                {
                    PositionId = ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID,
                    PermissionFlag = (byte)PermissionFlag.UserActionable
                };
                originalForm.NextApprover = position.AdfUserPositions.Count == 1 ? position.AdfUserPositions.Single().EmployeeEmail : position.PositionTitle;
                originalForm.NextApprovalLevel = position.PositionTitle;
            }
            else if (!employeeInfo.ExecutiveDirectorPositionId.HasValue)
            {
                permission = new()
                    { UserId = ownerPermission.UserId, PermissionFlag = (byte)PermissionFlag.UserActionable };
                originalForm.NextApprover = null;
                originalForm.NextApprovalLevel = null;
                originalForm.FormStatusId = (int)FormStatus.Unsubmitted;
                originalForm.FormSubStatus = Enum.GetName(FormStatus.Unsubmitted);
            }
            else
            {
                permission = new()
                    { PositionId = employeeInfo.ExecutiveDirectorPositionId, PermissionFlag = (byte)PermissionFlag.UserActionable };
                originalForm.NextApprover = employeeInfo.ExecutiveDirectorIdentifier;
                originalForm.NextApprovalLevel = $"Executive Director {employeeInfo.Directorate}";
            }

            return new EscalationResult
                { UpdatedForm = originalForm, DoesEscalate = false, PermissionUpdate = permission };
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "Error determining escalate path for {OriginalFormFormInfoId} in {CprEscalationManagerName}",
                originalForm.FormInfoId, nameof(CprEscalationManager));
            throw;
        }
    }
}