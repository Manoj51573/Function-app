using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Specifications;
using System.Linq;

namespace eforms_middleware.Services;

public class AllowanceClaimsFormsEscalationServiceManager : EscalationManagerBase
{
    private readonly IFormInfoService _formInfoService;
    private readonly ITaskManager _taskManager;
    private readonly IEscalationFactoryService _escalationFactoryService;
    private readonly IFormHistoryService _formHistoryService;
    private readonly IPermissionManager _permissionManager;
    private readonly IWorkflowBtnService _workflowBtnService;
    private readonly IEmployeeService _employeeService;

    public AllowanceClaimsFormsEscalationServiceManager(IFormInfoService formInfoService, ITaskManager taskManager,
        IEscalationFactoryService escalationFactoryService, IFormHistoryService formHistoryService,
        IPermissionManager permissionManager, IEmployeeService employeeService, IWorkflowBtnService workflowBtnService)
    {
        _formInfoService = formInfoService;
        _taskManager = taskManager;
        _escalationFactoryService = escalationFactoryService;
        _formHistoryService = formHistoryService;
        _permissionManager = permissionManager;
        _workflowBtnService = workflowBtnService;
        _employeeService = employeeService;
    }

    /* public async Task<FormInfo> EscalateFormAsync(TaskInfo task)
     {
         var escalationManager = _escalationFactoryService.GetEscalationManager((FormType)task.FormInfo.AllFormsId);
         var result = await escalationManager.EscalateFormAsync(task.FormInfo);
         var request = new FormInfoUpdate
         {
             FormAction = null,
             FormDetails = new FormDetailsRequest { FormInfoId = task.FormInfo.FormInfoId },
             UserId = null
         };
         var newForm = await _formInfoService.SaveFormInfoAsync(request, result.UpdatedForm);
         TaskInfo taskInfo = null;
         if (newForm.FormStatusId != (int)FormStatus.Unsubmitted)
         {
             taskInfo = new()
             {
                 AllFormsId = task.FormInfo.AllFormsId,
                 FormInfoId = task.FormInfo.FormInfoId,
                 FormOwnerEmail = task.FormInfo.FormOwnerEmail,
                 AssignedTo = task.FormInfo.NextApprover,
                 TaskStatus = "Escalated",
                 TaskCreatedBy = "System",
                 TaskCreatedDate = DateTime.Now,
                 ActiveRecord = task.FormInfo.ActiveRecord,
                 SpecialReminderDate = DateTime.Today.AddDays(result.NotifyDays),
                 SpecialReminderTo = task.FormInfo.NextApprover,
                 EscalationDate = result.DoesEscalate ? DateTime.Today.AddDays(result.EscalationDays) : null,
                 Escalation = result.DoesEscalate,
                 SpecialReminder = true,
                 ReminderTo = task.FormInfo.NextApprover
             };
         }
         // These must succeed
         await _taskManager.AddFormTaskAsync(task, taskInfo);
         var permissionId = await _permissionManager.UpdateFormPermissionsAsync(task.FormInfo.FormInfoId,
               new List<FormPermission> { result.PermissionUpdate });
         await _workflowBtnService.SetNextApproverBtn(task.FormInfo.FormInfoId, result.StatusBtnData, permissionId);
         var action = newForm.FormStatusId != (int)FormStatus.Unsubmitted
             ? Enum.GetName(FormStatus.Escalated)
             : Enum.GetName(FormStatus.Cancelled);
         await _formHistoryService.AddFormHistoryAsync(new FormInfoUpdate { FormAction = action }, newForm, null,
             "Form actioned by system due to inactivity.");
         return newForm;
     }*/

    public async override Task<EscalationResult> EscalateFormAsync(FormInfo originalForm)
    {
        try
        {
            var statusBtnData = new StatusBtnData();
            bool doesEscalate = true;
            FormPermission permission = null;
            var specification = new FormPermissionSpecification(formId: originalForm.FormInfoId
                , addPositionInfo: true
                , addUserInfo: true);
            var permissions = await _permissionManager.GetPermissionsBySpecificationAsync(specification);
            var approvalPermission = permissions.Single(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);
            var approver = await _employeeService.GetEmployeeByEmailAsync(approvalPermission.Email);

            var nextApprover = approver.Managers.Any()
                               ? approver?.Managers.FirstOrDefault()
                               : approver?.ExecutiveDirectors.FirstOrDefault();

            if (nextApprover.EmployeeManagementTier > 3)
            {
                //should not escalate beyond tier 4
                doesEscalate = nextApprover.EmployeeManagementTier > 3;

                permission = new()
                {
                    PositionId = nextApprover.EmployeePositionId,
                    PermissionFlag = (byte)PermissionFlag.UserActionable
                };

                statusBtnData.StatusBtnModel =
                    new List<StatusBtnModel>
                                        {
                                                new StatusBtnModel(){
                                                    StatusId = (int)FormStatus.Approved,
                                                    BtnText = FormStatus.Approve.ToString(),
                                                    FormSubStatus = FormStatus.Approved.ToString(),
                                                    HasDeclaration = true
                                                },
                                                new StatusBtnModel(){
                                                    StatusId = (int)FormStatus.Unsubmitted,
                                                    BtnText = FormStatus.Reject.ToString(),
                                                    FormSubStatus = FormStatus.Rejected.ToString(),
                                                    HasDeclaration = false
                                                }
                                        };

                originalForm.FormSubStatus = FormStatus.Escalated.ToString();

                originalForm.NextApprover = nextApprover.EmployeeEmail;
                originalForm.NextApprovalLevel = nextApprover.EmployeePositionTitle;


            }
            else
            {
                permission = new FormPermission((byte)PermissionFlag.UserActionable, groupId: AllowanceAndClaims.POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID);
                originalForm.NextApprover = AllowanceAndClaims.POD_EFFORMS_BUSINESS_ADMIN_GROUP_EMAIL;
                originalForm.NextApprovalLevel = AllowanceAndClaims.POD_EFFORMS_BUSINESS_ADMIN_GROUP_NAME;
                new List<StatusBtnModel>
                                            {
                                                new StatusBtnModel(){
                                                    StatusId = (int)FormStatus.Approved,
                                                    BtnText = FormStatus.Approve.ToString(),
                                                    FormSubStatus = FormStatus.Approved.ToString(),
                                                    HasDeclaration = true
                                                },
                                                new StatusBtnModel(){
                                                    StatusId = (int)FormStatus.Unsubmitted,
                                                    BtnText = FormStatus.Reject.ToString(),
                                                    FormSubStatus = FormStatus.Rejected.ToString(),
                                                    HasDeclaration = false
                                                }
                                            };
            }
            return new EscalationResult()
            {
                UpdatedForm = originalForm,
                DoesEscalate = doesEscalate,
                NotifyDays = 2,
                EscalationDays = 3,
                PermissionUpdate = permission,
                StatusBtnData = statusBtnData
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task NotifyFormAsync(FormInfo dbModel
        , int reminderDays = 2
        , int escalationDays = 3
        , bool allowEscalation = false)
    {
        TaskInfo taskInfo = new()
        {
            AllFormsId = dbModel.AllFormsId,
            FormInfoId = dbModel.FormInfoId,
            FormOwnerEmail = dbModel.FormOwnerEmail,
            AssignedTo = dbModel.NextApprover,
            TaskStatus = "Reminder Updated",
            TaskCreatedBy = "System",
            TaskCreatedDate = DateTime.Now,
            ActiveRecord = dbModel.ActiveRecord,
            SpecialReminderDate = DateTime.Today.AddDays(reminderDays),
            SpecialReminderTo = dbModel.NextApprover,
            EscalationDate = allowEscalation ? DateTime.Today.AddDays(escalationDays) : null,
            Escalation = allowEscalation,
            SpecialReminder = true,
            ReminderTo = dbModel.NextApprover
        };

        await _taskManager.AddFormTaskAsync(dbModel.FormInfoId, taskInfo);
    }

}