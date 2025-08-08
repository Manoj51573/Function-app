using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;

namespace eforms_middleware.Services;

public class EscalationService : IEscalationService
{
    private readonly IFormInfoService _formInfoService;
    private readonly ITaskManager _taskManager;
    private readonly IEscalationFactoryService _escalationFactoryService;
    private readonly IFormHistoryService _formHistoryService;
    private readonly IPermissionManager _permissionManager;
    private readonly IWorkflowBtnService _workflowBtnService;

    public EscalationService(IFormInfoService formInfoService, ITaskManager taskManager,
        IEscalationFactoryService escalationFactoryService, IFormHistoryService formHistoryService,
        IPermissionManager permissionManager,
        IWorkflowBtnService workflowBtnService)
    {
        _formInfoService = formInfoService;
        _taskManager = taskManager;
        _escalationFactoryService = escalationFactoryService;
        _formHistoryService = formHistoryService;
        _permissionManager = permissionManager;
        _workflowBtnService = workflowBtnService;
    }

    public async Task<FormInfo> EscalateFormAsync(TaskInfo task)
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
    }

    public async Task NotifyFormAsync(FormInfo dbModel
        , int reminderDays = 2
        , int escalationDays = 5
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