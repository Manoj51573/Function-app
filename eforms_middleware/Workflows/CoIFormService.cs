using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure.Interfaces;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.GetMasterData;
using eforms_middleware.Interfaces;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static eforms_middleware.Settings.Helper;

namespace eforms_middleware.Workflows;

public class CoIFormService<T> : FormServiceBase
{
    private readonly IFormSubtypeHelper<T> _formSubtypeHelper;

    public CoIFormService(
        ILogger<CoIFormService<T>> log, ITaskManager taskManager,
        IRepository<RefFormStatus> formStatusRepository,
        IFormHistoryService formHistoryService,
        IFormInfoService formInfoService, IMessageFactoryService messageFactoryService, IEmployeeService employeeService,
        IFormSubtypeHelper<T> formSubtypeHelper, IRequestingUserProvider requestingUserProvider, IPermissionManager permissionManager,
        IPositionService positionService) : base(log, taskManager,
        formStatusRepository, formHistoryService, formInfoService,
        messageFactoryService, employeeService, requestingUserProvider, permissionManager, positionService)
    {
        _formSubtypeHelper = formSubtypeHelper;
    }

    protected override string GetValidSerializedModel()
    {
        return _formSubtypeHelper.GetValidSerializedModel(DbRecord, Request);
    }

    protected override Task<ValidationResult> ValidateInternal()
    {
        return _formSubtypeHelper.ValidateInternal(Request);
    }

    protected override async Task SaveInternalAsync()
    {
        await base.SaveInternalAsync();
        var isDirectorGeneral = RequestingUser.EmployeePositionId == ConflictOfInterest.DIRECTOR_GENERAL_POSITION_NUMBER;
        if (Request.FormDetails.AllFormsId == (int)FormType.CoI_Other && !isDirectorGeneral)
        {
            PermissionsToAdd.Add(new FormPermission { GroupId = ConflictOfInterest.ODG_AUDIT_GROUP_ID, PermissionFlag = (byte)PermissionFlag.View });
        }
        PermissionsToAdd.Add(new FormPermission { GroupId = Group.POD_FORMS_ADMIN_BUSINESS_ID, PermissionFlag = (byte)PermissionFlag.View });
    }

    private async Task<IApprovalInfo> SubmitExceptionScenarios(bool isForOdg, IApprovalInfo manager)
    {
        // Get position as that will reveal how many users are in the role
        var edOdg = await PositionService.GetPositionByIdAsync(ConflictOfInterest.ED_ODG_POSITION_ID);
        var odgTitle = edOdg.AdfUserPositions.Count == 1 ? edOdg.AdfUserPositions.Single().EmployeeEmail : edOdg.PositionTitle;
        var nextApprover = isForOdg ? odgTitle : manager.NextApprover;
        DbRecord.FormSubStatus = Enum.GetName(FormStatus.Endorsed);
        DbRecord.FormStatusId = (int)FormStatus.Endorsed;
        var permission = new FormPermission
        {
            PositionId = isForOdg ? edOdg.Id : manager.PositionId,
            PermissionFlag = (byte)PermissionFlag.UserActionable
        };
        PermissionsToAdd.Add(permission);
        return new ApprovalInfo { NextApprover = nextApprover, PositionId = permission.PositionId };
    }

    protected override async Task SubmitInternalAsync()
    {
        await base.SubmitInternalAsync();
        var isDirectorGeneral = RequestingUser.EmployeePositionId == ConflictOfInterest.DIRECTOR_GENERAL_POSITION_NUMBER;
        if (Request.FormDetails.AllFormsId == (int)FormType.CoI_Other && !isDirectorGeneral)
        {
            PermissionsToAdd.Add(new FormPermission { GroupId = ConflictOfInterest.ODG_AUDIT_GROUP_ID, PermissionFlag = (byte)PermissionFlag.View });
        }
        PermissionsToAdd.Add(new FormPermission { GroupId = Group.POD_FORMS_ADMIN_BUSINESS_ID, PermissionFlag = (byte)PermissionFlag.View });
        var managerApprovalInfo = new ApprovalInfo
        { NextApprover = RequestingUser.ManagerIdentifier, PositionId = RequestingUser.ManagerPositionId };
        var finalApprovalInfo = await _formSubtypeHelper.GetFinalApprovers();
        IApprovalInfo approvalInfo = null;
        var isConfidentialSubmit = DbRecord.AllFormsId == (int)FormType.CoI_CPR &&
                                   Request.FormDetails.NextApprover == "ED" &&
                                   RequestingUser.EmployeePositionId is not ConflictOfInterest
                                       .ED_PEOPLE_AND_CULTURE_POSITION_ID;
        var employeeManager = isConfidentialSubmit ? finalApprovalInfo : managerApprovalInfo;
        // Not for ED P&C for other should be Gov & Audit Group
        if (RequestingUser.EmployeePositionId is ConflictOfInterest.DIRECTOR_GENERAL_POSITION_NUMBER ||
            RequestingUser.EmployeePositionId == finalApprovalInfo.PositionId)
        {
            var isForOdg = RequestingUser.EmployeePositionId == ConflictOfInterest.DIRECTOR_GENERAL_POSITION_NUMBER && !isConfidentialSubmit;
            approvalInfo = await SubmitExceptionScenarios(isForOdg, employeeManager);
        }
        else
        {
            var reportToTier1 = RequestingUser.Managers.Any(x => x.EmployeeManagementTier is 1);

            if (reportToTier1)
            {
                var edOdgUserList = await EmployeeService
                                          .GetEmployeeByPositionNumberAsync(positionNumber: ConflictOfInterest.ED_ODG_POSITION_ID);

                approvalInfo = new ApprovalInfo
                {
                    NextApprover = IsFormOwnerEdODG ? ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL : edOdgUserList.GetDescription(),
                    GroupId = IsFormOwnerEdODG ? ConflictOfInterest.ODG_AUDIT_GROUP_ID : null,
                    PositionId = !IsFormOwnerEdODG ? ConflictOfInterest.ED_ODG_POSITION_ID : null
                };

                PermissionsToAdd.Add(new FormPermission
                {
                    PermissionFlag = (byte)PermissionFlag.UserActionable,
                    GroupId = IsFormOwnerEdODG ? ConflictOfInterest.ODG_AUDIT_GROUP_ID : null,
                    PositionId = !IsFormOwnerEdODG ? ConflictOfInterest.ED_ODG_POSITION_ID : null
                });

                DbRecord.NextApprovalLevel = IsFormOwnerEdODG ? EmployeeTitle.GovAuditGroup.GetEmployeeTitle() : EmployeeTitle.ExecutiveDirectorODG.ToString();
            }
            else
            {
                if (isConfidentialSubmit)
                {
                    PermissionsToAdd.Add(new FormPermission
                    {
                        PermissionFlag = (byte)PermissionFlag.UserActionable,
                        PositionId = finalApprovalInfo.PositionId
                    });
                    approvalInfo = finalApprovalInfo;
                }
                else if (RequestingUser.Managers.Any())
                {
                    PermissionsToAdd.Add(new FormPermission
                    {
                        PermissionFlag = (byte)PermissionFlag.UserActionable,
                        PositionId = RequestingUser.ManagerPositionId
                    });
                    approvalInfo = new ApprovalInfo
                    { PositionId = RequestingUser.ManagerPositionId, NextApprover = RequestingUser.ManagerIdentifier };

                }
                else if (RequestingUser.ExecutiveDirectors.Any())
                {
                    PermissionsToAdd.Add(new FormPermission
                    {
                        PermissionFlag = (byte)PermissionFlag.UserActionable,
                        PositionId = RequestingUser.ExecutiveDirectorPositionId
                    });
                    approvalInfo = new ApprovalInfo
                    { PositionId = RequestingUser.ExecutiveDirectorPositionId, NextApprover = RequestingUser.ExecutiveDirectorIdentifier };
                }
                else
                {
                    PermissionsToAdd.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: finalApprovalInfo.PositionId, groupId: finalApprovalInfo.GroupId));
                    approvalInfo = finalApprovalInfo.GroupId.HasValue ? new ApprovalInfo() : null;
                }
            }
            // The only person who shouldn't have a manager is the DG but users on leave also are missing a manager but should
            // not be able to perform form actions.
            if (approvalInfo == null)
            {
                throw new Exception("You do not have a Tier 3 assigned currently. Save form and try again later.");
            }
        }

        if (RequestingUser.Managers.Any(x => x.EmployeePositionId == approvalInfo.PositionId) && RequestingUser.EmployeeManagementTier is > 3)
        {
            SetTaskInfo(approvalInfo.NextApprover, 1, 0, approvalInfo.NextApprover, RequestingUser.ExecutiveDirectorIdentifier, DateTime.Today.AddDays(3), DateTime.Today.AddDays(5));
        }
        else
        {
            SetTaskInfo(approvalInfo.NextApprover, 1, 0, approvalInfo.NextApprover, null, DateTime.Today.AddDays(3), null);
        }

        DbRecord.NextApprover = approvalInfo.NextApprover;
    }

    protected override async Task Approve()
    {
        // If approver is the ED P&C should Complete
        // If employee or manager is Tier3 or above should go straight to Endorsed
        await base.Approve();
        var actioningUser = await EmployeeService.GetEmployeeByAzureIdAsync(RequestingUser.ActiveDirectoryId);
        var finalApprovalInfo = await _formSubtypeHelper.GetFinalApprovers();
        if (!actioningUser.ExecutiveDirectorPositionId.HasValue && actioningUser.EmployeeManagementTier > 3 && actioningUser.EmployeePositionId != finalApprovalInfo?.PositionId)
        {
            throw new Exception("Employee does not have a Tier 3 assigned currently. Try again later.");
        }
        DbRecord.Response = GetValidSerializedModel();
        if (finalApprovalInfo.PositionId != null && actioningUser.EmployeePositionId == finalApprovalInfo.PositionId)
        {
            await CompletedAsync();
        }
        else if (actioningUser.EmployeeManagementTier is <= 3)
        {
            // Endorse instead
            PermissionsToAdd.Add(new FormPermission
            {
                PermissionFlag = (byte)PermissionFlag.UserActionable,
                PositionId = finalApprovalInfo.PositionId,
                GroupId = finalApprovalInfo.GroupId
            });
            DbRecord.NextApprover = finalApprovalInfo.NextApprover;
            DbRecord.FormSubStatus = Enum.GetName(FormStatus.Endorsed);
            DbRecord.FormStatusId = (int)FormStatus.Endorsed;
        }
        else
        {
            PermissionsToAdd.Add(new FormPermission
            {
                PermissionFlag = (byte)PermissionFlag.UserActionable,
                PositionId = actioningUser.ExecutiveDirectorPositionId
            });
            SetTaskInfo(actioningUser.ExecutiveDirectorIdentifier, 1, 0, actioningUser.ExecutiveDirectorIdentifier, null, DateTime.Today.AddDays(3), DateTime.Today.AddDays(5));
            DbRecord.NextApprover = actioningUser.ExecutiveDirectorIdentifier;
        }
    }

    protected override async Task EndorseAsync()
    {
        await base.EndorseAsync();
        DbRecord.Response = GetValidSerializedModel();
        var actioningUser = await EmployeeService.GetEmployeeByAzureIdAsync(RequestingUser.ActiveDirectoryId);
        var finalApprovalInfo = await _formSubtypeHelper.GetFinalApprovers();
        if (finalApprovalInfo.PositionId != null && actioningUser.EmployeePositionId == finalApprovalInfo.PositionId)
        {
            await CompletedAsync();
        }
        else
        {
            DbRecord.NextApprover = finalApprovalInfo.NextApprover;
            PermissionsToAdd.Add(new FormPermission
            {
                PermissionFlag = (byte)PermissionFlag.UserActionable,
                PositionId = finalApprovalInfo.PositionId,
                GroupId = finalApprovalInfo.GroupId
            });

            SetTaskInfo(finalApprovalInfo.NextApprover, 0, 0, null, null, null, null);
        }
    }

    protected override async Task CompletedAsync()
    {
        await base.CompletedAsync();
        DbRecord.Response = GetValidSerializedModel();
        var specialReminder = _formSubtypeHelper.GetCompletionReminderDate(Request);
        if (specialReminder != null)
        {
            SetTaskInfo(null, 0, 0, ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL,
                ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL, specialReminder, null);
        }
    }

    protected override string RejectionReason =>
        JsonConvert.DeserializeObject<CoiRejection>(Request.FormDetails.Response)!.RejectionReason;

    protected override async Task Reject()
    {
        var ownerPermission = DbRecord.FormPermissions.Single(x => x.IsOwner);
        DbRecord.Response = GetValidSerializedModel();
        DbRecord.FormSubStatus = nameof(FormStatus.Unsubmitted);
        DbRecord.FormStatusId = (int)FormStatus.Unsubmitted;
        PermissionsToAdd.Add(new FormPermission
        { UserId = ownerPermission.UserId, PermissionFlag = (byte)PermissionFlag.UserActionable });
        DbRecord.NextApprover = null;
    }

    protected override Task<bool> NextStateAvailable()
    {
        var result = false;
        var requestedStatus = Enum.Parse<FormStatus>(Request.FormAction);
        FormStatus? dbFormStatus = DbRecord != null ? (FormStatus)DbRecord.FormStatusId : null;
        var dbFormType = (FormType)DbRecord!.AllFormsId;
        switch (requestedStatus)
        {
            case FormStatus.Attachment when dbFormStatus is 0 or FormStatus.Unsubmitted:
            case FormStatus.Unsubmitted when dbFormStatus is 0 or FormStatus.Unsubmitted:
            case FormStatus.Submitted when dbFormStatus is 0 or FormStatus.Unsubmitted:
            case FormStatus.Recall when dbFormStatus is not 0 and not FormStatus.Unsubmitted and not FormStatus.Completed:
            case FormStatus.Approved when dbFormStatus is FormStatus.Submitted or FormStatus.IndependentReview or FormStatus.Delegated:
            case FormStatus.Rejected when dbFormStatus is FormStatus.Submitted or FormStatus.Approved or FormStatus.Endorsed or FormStatus.IndependentReview or FormStatus.Delegated:
            case FormStatus.Endorsed when dbFormStatus is FormStatus.Approved or FormStatus.IndependentReview or FormStatus.Delegate:
            case FormStatus.Completed when dbFormStatus is FormStatus.Endorsed or FormStatus.IndependentReview or FormStatus.Submitted:
            case FormStatus.IndependentReview when dbFormStatus is FormStatus.Endorsed && dbFormType is FormType.CoI_Other:
            case FormStatus.IndependentReviewCompleted when dbFormStatus is FormStatus.IndependentReview:
            case FormStatus.Delegated when dbFormStatus is FormStatus.Endorsed && dbFormType is FormType.CoI_Other:
                result = true;
                break;
        }

        return Task.FromResult(result);
    }

    protected override Task<bool> CanCreate()
    {
        return Task.FromResult(!string.IsNullOrWhiteSpace(RequestingUser.EmployeeNumber) && RequestingUser.EmployeePositionId.HasValue);
    }

    protected override IList<AttachmentResult> ReturnAttachments()
    {
        var requestedStatus = Enum.Parse<FormStatus>(Request.FormAction);
        if (Request.FormDetails.AllFormsId != (int)FormType.CoI_Other) return null;
        var dbRecordFormStatus = (FormStatus)DbRecord.FormStatusId;
        switch (requestedStatus)
        {
            case FormStatus.Unsubmitted when dbRecordFormStatus is FormStatus.Unsubmitted:
            case FormStatus.Submitted when dbRecordFormStatus is FormStatus.Unsubmitted:
            case FormStatus.Completed when dbRecordFormStatus is FormStatus.Endorsed or FormStatus.IndependentReview:
                var form = JsonConvert.DeserializeObject<ResponseAttachments>(Request.FormDetails.Response);
                return form.Attachments;
            default:
                return null;
        }
    }
}