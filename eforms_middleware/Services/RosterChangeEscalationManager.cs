using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Specifications;
using eforms_middleware.Workflows;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace eforms_middleware.Services;

public class RosterChangeEscalationManager : EscalationManagerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly IPermissionManager _permissionManager;
    private readonly ILogger<RosterChangeEscalationManager> _logger;
    private readonly BaseApprovalService _baseApprovalService;
    protected List<FormPermission> CurrentApprovers { get; private set; }
    public RosterChangeEscalationManager(IEmployeeService employeeService,
        IPermissionManager permissionManager,
        ILogger<RosterChangeEscalationManager> logger
        , BaseApprovalService baseApprovalService)
    {
        _employeeService = employeeService;
        _permissionManager = permissionManager;
        _logger = logger;
        _baseApprovalService = baseApprovalService;
    }
    public override async Task<EscalationResult> EscalateFormAsync(FormInfo originalForm)
    {
        try
        {
            FormPermission permission = null;
            var statusBtnData = new StatusBtnData();
            var specification = new FormPermissionSpecification(formId: originalForm.FormInfoId, addUserInfo: true);
            var permissions = await _permissionManager.GetPermissionsBySpecificationAsync(specification);
            var ownerPermission = permissions.Single(x => x.IsOwner);
            var approvalPermission = permissions.Single(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);
            var approver = await _employeeService.GetEmployeeByEmailAsync(approvalPermission.Form.NextApprover);
            var approverManager = approver.Managers.Any()
                                ? approver?.Managers.FirstOrDefault()
                                : approver?.ExecutiveDirectors.FirstOrDefault();

            if (approverManager.EmployeeManagementTier > 3)
            {
                permission = new FormPermission((byte)PermissionFlag.UserActionable, positionId: approverManager.EmployeePositionId);
                originalForm.NextApprover = approver.ManagerIdentifier;
                originalForm.NextApprovalLevel = approverManager.EmployeePositionTitle;
                statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                        {
                            _baseApprovalService.SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                            _baseApprovalService.SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                            _baseApprovalService.SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                        };
            }
            else
            {
                permission = new FormPermission((byte)PermissionFlag.UserActionable, groupId: RosterChangeRequest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID);
                originalForm.NextApprover = RosterChangeRequest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_EMAIL;
                originalForm.NextApprovalLevel = RosterChangeRequest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_NAME;
                statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                {
                    _baseApprovalService.SetStatusBtnData(FormStatus.Delegated, FormStatus.Delegate.ToString(), true),
                    _baseApprovalService.SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                };
            }

            return new EscalationResult
            {
                UpdatedForm = originalForm,
                DoesEscalate = false,
                PermissionUpdate = permission,
                StatusBtnData = statusBtnData
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                $"Error determining escalate path for {originalForm.FormInfoId} in {nameof(RosterChangeEscalationManager)}");
            throw;
        }
    }
}

