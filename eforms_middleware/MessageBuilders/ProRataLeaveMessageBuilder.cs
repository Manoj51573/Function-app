using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Services;
using eforms_middleware.Specifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace eforms_middleware.MessageBuilders;

public class ProRataLeaveMessageBuilder : LCOMessageBuilder
{
    private readonly ILogger<ProRataLeaveMessageBuilder> _logger;
    protected override string EditPath => "leave-cash-out";
    protected override string SummaryPath => "leave-cash-out/summary";
    protected override string FormTypeSubject => "Pro-rata leave";
    protected override string FormTypeBody => "";
    public ProRataLeaveMessageBuilder(IEmployeeService employeeService, IRepository<TaskInfo> taskInfoRepo,
        ILogger<ProRataLeaveMessageBuilder> logger, IConfiguration configuration, IRepository<FormPermission> formPermissionRepo,
        IFormHistoryService formHistoryService, IRequestingUserProvider requestingUserProvider, IPermissionManager permissionManager)
        : base(configuration, requestingUserProvider, permissionManager, employeeService)
    {
        _logger = logger;
    }

    protected async Task<List<MailMessage>> GetSubmittedMail()
    {
        var submittedMails = new List<MailMessage> { };
        var leaveModel = JsonConvert.DeserializeObject<ProRataModel>(DbModel.Response);
        if (leaveModel.IsLeaveRequesterIsManager is "Yes")
        {
            var emailSubject = $"{FormTypeSubject} Request eForm {DbModel.FormInfoId} has been submitted on your behalf";
            var body = string.Format(LeaveAmendmentTemplates.SUBMITTED_TO_EMPLOYEE_TEMPLATE, leaveModel.allEmployees[0].EmployeeFullName, DbModel.FormOwnerName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
            var ownerMessage = new MailMessage(FromEmail, leaveModel.allEmployees[0].EmployeeEmail,
            emailSubject, body);
            submittedMails = new List<MailMessage> { ownerMessage };
        }
        return submittedMails;
    }

    protected async Task<List<MailMessage>> GetCompletedEmail(bool ccManager = true)
    {
        var completedMail = new List<MailMessage> { };
        var emailSubject = $" {FormTypeSubject} Request  {DbModel.FormInfoId} has been completed";
        var leaveModel = JsonConvert.DeserializeObject<LeaveAmendmentModel>(DbModel.Response);
        if (leaveModel.isLeaveRequesterIsManager == "Yes")
        {
            var formOwner = await EmployeeService.GetEmployeeByPositionNumberAsync(FormOwnerPermission.PositionId.Value);
            foreach (var formOwnerUser in formOwner)
            {
                var ownerMessageBody = string.Format(LeaveAmendmentTemplates.COMPLETED_TEMPLATE_TO_MANAGER, formOwnerUser.EmployeePreferredFullName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);

                var messageBody = new MailMessage(FromEmail, formOwnerUser.EmployeeEmail, emailSubject, ownerMessageBody);
                messageBody.CC.Add(new MailAddress(leaveModel.allEmployees[0].EmployeeEmail));
                completedMail.Add(messageBody);
            }
        }
        else
        {
            var employeeDetails = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);
            var recipients = employeeDetails.Managers;
            var body = string.Format(LeaveAmendmentTemplates.COMPLETED_TEMPLATE_TO_MANAGER, DbModel.FormOwnerName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
            var ownerMail = new MailMessage(FromEmail, employeeDetails.EmployeeEmail,
                emailSubject, body);
            if (recipients.Count != 0)
            {
                foreach (var userManagers in recipients)
                {
                    ownerMail.CC.Add(new MailAddress(userManagers.EmployeeEmail));
                }
            }
            else
            {
                ownerMail.CC.Add(new MailAddress(employeeDetails.ExecutiveDirectorIdentifier));
            }
            completedMail = new List<MailMessage> { ownerMail };
        }
        return completedMail;
    }
    protected async Task<List<MailMessage>> GetRejectedMail()
    {

        var emailSubject = $"{FormTypeSubject}  Request eForm {DbModel.FormInfoId} has been rejected";
        var rejectedMails = new List<MailMessage> { };
        var leaveModel = JsonConvert.DeserializeObject<ProRataModel>(DbModel.Response);

        if (leaveModel.IsLeaveRequesterIsManager == "Yes")
        {
            var formOwner = await EmployeeService.GetEmployeeByPositionNumberAsync(FormOwnerPermission.PositionId.Value);
            var count = 0;
            foreach (var userManagers in formOwner)
            {
                count++;
                var ownerMessageBody = string.Format(LeaveAmendmentTemplates.REJECTED_TEMPLATE, formOwner.First().EmployeePreferredFullName, RequestingUser.EmployeePreferredFullName, FormTypeSubject, DbModel.FormInfoId, leaveModel.ReasonForDecision, SummaryHref, SuffixText);
                var ownerMail = new MailMessage(FromEmail, userManagers.EmployeeEmail,
                    emailSubject, ownerMessageBody);
            
                if (DbModel.NextApprovalLevel != "!DOT Employee Services Officer Group")
                {
                    ownerMail.CC.Add(new MailAddress(RequestingUser.EmployeeEmail));
                }
                if (count == 1)
                {
                    ownerMail.CC.Add(new MailAddress(leaveModel.allEmployees[0].EmployeeEmail));
                }

                rejectedMails.Add(ownerMail);
            }
        }
        else
        {
            var formOwner = await EmployeeService.GetEmployeeByAzureIdAsync(Permissions.Single(x => x.IsOwner).UserId!.Value);
            var ownerMessageBody = string.Format(LeaveAmendmentTemplates.REJECTED_TEMPLATE, formOwner.EmployeePreferredFullName, RequestingUser.EmployeePreferredFullName, FormTypeSubject, DbModel.FormInfoId, leaveModel.ReasonForDecision, SummaryHref, SuffixText);
            var viewPermissions = Permissions.Where(x => x.PermissionFlag == (byte)PermissionFlag.View && !x.IsOwner && x.PositionId.HasValue);
            var managers = await EmployeeService.GetEmployeeByPositionNumberAsync(viewPermissions.First().PositionId.Value);
            var messageBody = new MailMessage(FromEmail, formOwner.EmployeeEmail, emailSubject, ownerMessageBody);
            if (DbModel.NextApprovalLevel != "!DOT Employee Services Officer Group")
            {
                messageBody.CC.Add(new MailAddress(RequestingUser.EmployeeEmail));
            }
            rejectedMails.Add(messageBody);
        }
        return rejectedMails;
    }
    protected async Task<List<MailMessage>> GetApprovedMail()
    {
        var formType = (FormType)DbModel.AllFormsId;
        var approvedMail = new List<MailMessage> { };
        var formOwner = await EmployeeService.GetEmployeeByAzureIdAsync(Permissions.Single(x => x.IsOwner).UserId!.Value);

        if (DbModel.NextApprover != "!DOT Employee Services Officer Group")
        {
            var viewPermissions = Permissions.Where(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable && !x.IsOwner && x.PositionId.HasValue);
            var managers = await EmployeeService.GetEmployeeByPositionNumberAsync(viewPermissions.First().PositionId.Value);
            var emailSubject = $"{FormTypeSubject} Request eForm {DbModel.FormInfoId} has been submitted for your review";
            foreach (var manager in managers)
            {
                var managerMessageBody = string.Format(LeaveAmendmentTemplates.SUBMITTED_TO_LINEMANAGER_TEMPLATE, formOwner.EmployeePreferredFullName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
                var managerMessage = new MailMessage(FromEmail, manager.EmployeeEmail, emailSubject, managerMessageBody);
                approvedMail.Add(managerMessage);
            }
        }
        return approvedMail;
    }

    protected async Task<List<MailMessage>> GetRecalledMail()
    {
        var emailSubject = $" {FormTypeSubject} Request eForm {DbModel.FormInfoId} has been recalled";
        var recallMessages = new List<MailMessage> { };
        var leaveModel = JsonConvert.DeserializeObject<LeaveAmendmentModel>(DbModel.Response);
        var count = 0;
        if (leaveModel.isLeaveRequesterIsManager is "Yes")
        {
            //if onbehalf is true
            var formOwner = await EmployeeService.GetEmployeeByPositionNumberAsync(FormOwnerPermission.PositionId.Value);
            foreach (var userManagers in formOwner)
            {
                count++;
                var ownerMessageBody = string.Format(LeaveAmendmentTemplates.RECALL_OWNER_TEMPLATE, userManagers.EmployeePreferredFullName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
                var ownerMail = new MailMessage(FromEmail, userManagers.EmployeeEmail, ///it should always requesting user not formOwner
                    emailSubject, ownerMessageBody);
                if (count == 1)
                {
                    ownerMail.CC.Add(new MailAddress(leaveModel.allEmployees[0].EmployeeEmail));
                    if (userManagers.EmployeeEmail != LeaveForms.LAST_APPROVER_EMAILID)
                    {
                        if (DbModel.NextApprovalLevel != "!DOT Employee Services Officer Group")
                        {
                            ownerMail.CC.Add(new MailAddress(LeaveForms.LAST_APPROVER_EMAILID));
                        }
                    }
                }
                recallMessages.Add(ownerMail);
            }
        }
        else
        {
            var viewPermissions = Permissions.Where(x => x.PermissionFlag == (byte)PermissionFlag.View && !x.IsOwner && x.PositionId.HasValue);
            var ownerMessageBody = string.Format(LeaveAmendmentTemplates.RECALL_OWNER_TEMPLATE, RequestingUser.EmployeePreferredFullName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
            var messageBody = new MailMessage(FromEmail, RequestingUser.EmployeeEmail, emailSubject, ownerMessageBody);
            var managers = await EmployeeService.GetEmployeeByPositionNumberAsync(viewPermissions.First().PositionId.Value);
            foreach (var manager in managers)
            {
                count++;
                messageBody.CC.Add(new MailAddress(manager.EmployeeEmail));

                if (manager.EmployeeEmail != LeaveForms.LAST_APPROVER_EMAILID && count == 1)
                {
                    if (DbModel.NextApprovalLevel != "!DOT Employee Services Officer Group")
                    {
                        messageBody.CC.Add(new MailAddress(LeaveForms.LAST_APPROVER_EMAILID));
                    }
                }
            }
            recallMessages.Add(messageBody);
        }

        return recallMessages;
    }
    protected override async Task<List<MailMessage>> GetMessageInternalAsync()
    {
        try
        {
            _logger.LogInformation("Processing mail request for form {0}", DbModel.FormInfoId);
            var past = DateTime.Today.AddDays(-3);
            _logger.LogInformation("Date for reminders set as {0}", past);
            var messages = new List<MailMessage>();
            var action = Enum.Parse<FormStatus>(Request.FormAction);

            messages = action switch
            {
                FormStatus.Approved when DbModel.Modified.HasValue && DbModel.Modified.Value.Date <= past =>
                    await GetReminderMail(),
                FormStatus.Submitted when DbModel.Modified.HasValue && DbModel.Modified.Value.Date <= past =>
                    await GetReminderMail(),
                FormStatus.Escalated when DbModel.FormStatusId == (int)FormStatus.Unsubmitted =>
                    await GetCancelledEmail(),
                FormStatus.Submitted when DbModel.FormStatusId == (int)FormStatus.Unsubmitted =>
                    await GetCancelledEmail(),
                FormStatus.Submitted when DbModel.FormStatusId == (int)FormStatus.Submitted => await GetSubmittedMail(),
                FormStatus.Delegated when DbModel.FormStatusId == (int)FormStatus.Delegated => await GetDelegatedMail(),
                FormStatus.Approved => await GetApprovedMail(),
                FormStatus.Rejected => await GetRejectedMail(),
                FormStatus.Completed when DbModel.Modified.Value.Date == DateTime.Today => await GetCompletedEmail(),
                FormStatus.Recall => await GetRecalledMail(),
                FormStatus.Escalated => await GetEscalatedMail(),
                _ => throw new ArgumentOutOfRangeException()
            };

            return messages;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Exception from {nameof(GetMessageInternalAsync)}");
            return new List<MailMessage>();
        }
    }
}