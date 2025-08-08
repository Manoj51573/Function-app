using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.Constants.COI;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Specifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace eforms_middleware.MessageBuilders;

public class MaternityLeaveMessageBuilder : LCOMessageBuilder
{
    private readonly ILogger<MaternityLeaveMessageBuilder> _logger;
    protected override string EditPath => "leave-cash-out";
    protected override string SummaryPath => "leave-cash-out/summary";
    protected override string FormTypeSubject => "Maternity Leave Application";
    protected override string FormTypeBody => "Close personal relationship in the workplace";



    public MaternityLeaveMessageBuilder(IEmployeeService employeeService,
        ILogger<MaternityLeaveMessageBuilder> logger, IConfiguration configuration, IRepository<FormPermission> formPermissionRepo,
        IFormHistoryService formHistoryService, IRequestingUserProvider requestingUserProvider, IPermissionManager permissionManager)
        : base(configuration, requestingUserProvider, permissionManager, employeeService)
    {
        _logger = logger;
    }
    protected async Task<List<MailMessage>> GetApprovedMail(bool isReminder = false)
    {
        var approvedMail = new List<MailMessage> { };
        var IsRequestorDVS = false;
        var leaveModel = JsonConvert.DeserializeObject<LeaveAmendmentModel>(DbModel.Response);
        var formOwner = await EmployeeService.GetEmployeeByEmailAsync(Permissions.Single(x => x.IsOwner).Email);
        if (formOwner != null)
        {
            IsRequestorDVS = formOwner.Directorate
                  .ToUpper().Equals(LeaveForms.DVS_EFFORMS_DIRECTORATE_NAME.ToUpper());
        }
        var emailSubject = $"{FormTypeSubject} Request eForm {DbModel.FormInfoId} has been submitted for your review";
        if (IsRequestorDVS && DbModel.NextApprover == LeaveForms.DOT_DVS_FIXED_TERM_APPOINTMENT_QA_GROUP_NAME)
            if (RequestingUser.EmployeeManagementTier is > 3 || IsRequestorDVS)
            {
                var body = string.Format(LeaveAmendmentTemplates.SUBMITTED_TO_LINEMANAGER_TEMPLATE, DbModel.FormOwnerName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
                var ownerMail = new MailMessage(FromEmail,
                    !IsRequestorDVS
                    ? RequestingUser.ExecutiveDirectorIdentifier
                    : LeaveForms.DOT_DVS_FIXED_TERM_APPOINTMENT_QA_GROUP,
                    emailSubject, body);
                    approvedMail = new List<MailMessage> { ownerMail };
            }
        return approvedMail;
    }
    protected async Task<List<MailMessage>> GetSubmittedMail()
    {
        var submittedMails = new List<MailMessage> { };
        var IsRequestorDVS = false;
        var leaveModel = JsonConvert.DeserializeObject<LeaveAmendmentModel>(DbModel.Response);
        var formOwner = await EmployeeService.GetEmployeeByEmailAsync(Permissions.Single(x => x.IsOwner).Email);
        if (formOwner != null)
        {
            IsRequestorDVS = formOwner.Directorate
                  .ToUpper().Equals(LeaveForms.DVS_EFFORMS_DIRECTORATE_NAME.ToUpper());
        }
        if (leaveModel.isLeaveRequesterIsManager is "Yes")
        {
            var emailSubject = $"{FormTypeSubject} Request eForm {DbModel.FormInfoId} has been submitted on your behalf";
            var body = string.Format(LeaveAmendmentTemplates.SUBMITTED_TO_EMPLOYEE_TEMPLATE, leaveModel.allEmployees[0].EmployeeFullName, DbModel.FormOwnerName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
            var ownerMessage = new MailMessage(FromEmail, leaveModel.allEmployees[0].EmployeeEmail,
            emailSubject, body);
            submittedMails = new List<MailMessage> { ownerMessage };
            if (IsRequestorDVS)
            {
                var groupMessageBody = string.Format(LeaveAmendmentTemplates.SUBMITTED_TO_LINEMANAGER_TEMPLATE, RequestingUser.EmployeePreferredFullName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
                var groupMessage = new MailMessage(FromEmail, LeaveForms.DOT_DVS_FIXED_TERM_APPOINTMENT_QA_GROUP, emailSubject, groupMessageBody);
                submittedMails.Add(groupMessage);
            }
        }
        else
        {
            var emailSubject = $"{FormTypeSubject} Request eForm {DbModel.FormInfoId} has been submitted for your review";
            var approvers = await EmployeeService.GetEmployeeByPositionNumberAsync(CurrentApprovers.Single().PositionId.Value);
            foreach (var manager in approvers)
            {
                var managerMessageBody = string.Format(LeaveAmendmentTemplates.SUBMITTED_TO_LINEMANAGER_TEMPLATE, RequestingUser.EmployeePreferredFullName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
                var managerMessage = new MailMessage(FromEmail, manager.EmployeeEmail, emailSubject, managerMessageBody);
                submittedMails.Add(managerMessage);
            }
        }
        return submittedMails;
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
                FormStatus.Unsubmitted => new List<MailMessage>(),
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