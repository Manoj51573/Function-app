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

public class PurchasedLeaveMessageBuilder : LCOMessageBuilder
{
    private readonly ILogger<PurchasedLeaveMessageBuilder> _logger;
    protected override string EditPath => "leave-cash-out";
    protected override string SummaryPath => "leave-cash-out/summary";
    protected override string FormTypeSubject => "Purchased Leave";
    private string FormSubject = "Deferred Salary Arrangement";
    protected override string FormTypeBody => "";
    public PurchasedLeaveMessageBuilder(IEmployeeService employeeService, IRepository<TaskInfo> taskInfoRepo,
        ILogger<PurchasedLeaveMessageBuilder> logger, IConfiguration configuration, IRepository<FormPermission> formPermissionRepo,
        IFormHistoryService formHistoryService, IRequestingUserProvider requestingUserProvider, IPermissionManager permissionManager)
        : base(configuration, requestingUserProvider, permissionManager, employeeService)
    {
        _logger = logger;
    }

    protected async Task<List<MailMessage>> GetSubmittedMail()
    {
        FormSubject = DbModel.AllFormsId == 48 ? FormSubject : FormTypeSubject;
        var emailSubject = $"{FormSubject} Request eForm {DbModel.FormInfoId} has been submitted for your review";
        var submittedMails = new List<MailMessage> { };
        var IsRequestorDVS = false;
        var formOwner = await EmployeeService.GetEmployeeByEmailAsync(Permissions.Single(x => x.IsOwner).Email);
        if (formOwner != null)
        {
            IsRequestorDVS = formOwner.Directorate
                  .ToUpper().Equals(LeaveForms.DVS_EFFORMS_DIRECTORATE_NAME.ToUpper());
        }

        var leaveModel = JsonConvert.DeserializeObject<LeaveCashOutModel>(DbModel.Response);
        if (leaveModel.IsLeaveRequesterIsManager is "Yes")
        {
            var body = string.Format(LeaveAmendmentTemplates.SUBMITTED_TO_EMPLOYEE_TEMPLATE, leaveModel.allEmployees[0].EmployeeFullName, DbModel.FormOwnerName, FormSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
            var ownerMessage = new MailMessage(FromEmail, leaveModel.allEmployees[0].EmployeeEmail,
            emailSubject, body);
            submittedMails = new List<MailMessage> { ownerMessage };
            //Email to tier 3 
            emailSubject = $"{FormSubject} Request eForm {DbModel.FormInfoId} has been submitted for your review";
            body = string.Format(LeaveAmendmentTemplates.SUBMITTED_TO_LINEMANAGER_TEMPLATE, DbModel.FormOwnerName, FormSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
            ownerMessage = new MailMessage(FromEmail, !IsRequestorDVS ? RequestingUser.ExecutiveDirectorIdentifier : LeaveForms.DOT_DVS_FIXED_TERM_APPOINTMENT_QA_GROUP,
            emailSubject, body);
            submittedMails.Add(ownerMessage);
        }
        else
        {

            var approvers = await EmployeeService.GetEmployeeByPositionNumberAsync(CurrentApprovers.Single().PositionId.Value);
            foreach (var manager in approvers)
            {
                var managerMessageBody = string.Format(LeaveAmendmentTemplates.SUBMITTED_TO_LINEMANAGER_TEMPLATE, RequestingUser.EmployeePreferredFullName, FormSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
                var managerMessage = new MailMessage(FromEmail, manager.EmployeeEmail, emailSubject, managerMessageBody);
                submittedMails.Add(managerMessage);
            }
        }
        return submittedMails;
    }

    protected async Task<List<MailMessage>> GetCompletedEmail(bool ccManager = true)
    {
        FormSubject = DbModel.AllFormsId == 48 ? FormSubject : FormTypeSubject;
        var completedMail = new List<MailMessage> { };
        var emailSubject = $" {FormSubject} Request  {DbModel.FormInfoId} has been completed";
        var leaveModel = JsonConvert.DeserializeObject<PurchasedLeaveModel>(DbModel.Response);
        if (leaveModel.IsLeaveRequesterIsManager == "Yes")
        {
            var formOwner = await EmployeeService.GetEmployeeByPositionNumberAsync(FormOwnerPermission.PositionId.Value);
            foreach (var formOwnerUser in formOwner)
            {
                var ownerMessageBody = "";
                if (leaveModel.LeaveApplicationType == "Deferred Salary Arrangement")
                {
                    ownerMessageBody = string.Format(LeaveAmendmentTemplates.COMPLETED_TEMPLATE_DEFERRED_LEAVE,
               formOwnerUser.EmployeePreferredFullName, FormSubject, DbModel.FormInfoId, SummaryHref, SuffixText,
               leaveModel.CommencementDateValue, leaveModel.CommencementOfDeductionsValue, leaveModel.CessationOfDeductionsValue);

                }
                else
                {
                    ownerMessageBody = string.Format(LeaveAmendmentTemplates.COMPLETED_TEMPLATE_PURCHASED_LEAVE,
                formOwnerUser.EmployeePreferredFullName, FormSubject, DbModel.FormInfoId, SummaryHref, SuffixText
                , leaveModel.SelectedWeekValue, leaveModel.NumberOfHoursPurchasedValue, leaveModel.CessationDateOfDeductionsValue
                , leaveModel.DeductionAmountValue);
                }
                var messageBody = new MailMessage(FromEmail, formOwnerUser.EmployeeEmail, emailSubject, ownerMessageBody);
                messageBody.CC.Add(new MailAddress(leaveModel.allEmployees[0].EmployeeEmail));
                completedMail.Add(messageBody);
            }
        }
        else
        {
            var employeeDetails = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);
            var recipients = employeeDetails.Managers;
            var ownerMessageBody = "";
            if (leaveModel.LeaveApplicationType == "Deferred Salary Arrangement")
            {
                ownerMessageBody = string.Format(LeaveAmendmentTemplates.COMPLETED_TEMPLATE_DEFERRED_LEAVE,
                DbModel.FormOwnerName, FormSubject, DbModel.FormInfoId, SummaryHref, SuffixText,
                leaveModel.CommencementDateValue, leaveModel.CommencementOfDeductionsValue, leaveModel.CessationOfDeductionsValue);

            }
            else
            {
                ownerMessageBody = string.Format(LeaveAmendmentTemplates.COMPLETED_TEMPLATE_PURCHASED_LEAVE,
                DbModel.FormOwnerName, FormSubject, DbModel.FormInfoId, SummaryHref, SuffixText
                , leaveModel.SelectedWeekValue, leaveModel.NumberOfHoursPurchasedValue, leaveModel.CessationDateOfDeductionsValue
                , leaveModel.DeductionAmountValue);
            }
            var ownerMail = new MailMessage(FromEmail, employeeDetails.EmployeeEmail,
                emailSubject, ownerMessageBody);
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

        FormSubject = DbModel.AllFormsId == 48 ? FormSubject : FormTypeSubject;
        var emailSubject = $"{FormSubject}  Request eForm {DbModel.FormInfoId} has been rejected";
        var rejectedMails = new List<MailMessage> { };
        var leaveModel = JsonConvert.DeserializeObject<PurchasedLeaveModel>(DbModel.Response);

        if (leaveModel.IsLeaveRequesterIsManager == "Yes")
        {
            var count = 0;
            var formOwner = await EmployeeService.GetEmployeeByPositionNumberAsync(FormOwnerPermission.PositionId.Value);
            foreach (var userManagers in formOwner)
            {
                count++;
                var ownerMessageBody = string.Format(LeaveAmendmentTemplates.REJECTED_TEMPLATE, formOwner.First().EmployeePreferredFullName, RequestingUser.EmployeePreferredFullName, FormSubject, DbModel.FormInfoId, leaveModel.ReasonForDecision, SummaryHref, SuffixText);
                var ownerMail = new MailMessage(FromEmail, userManagers.EmployeeEmail,
                    emailSubject, ownerMessageBody);
                
                if (DbModel.NextApprovalLevel != "!DOT Employee Services Officer Group")
                {
                    ownerMail.CC.Add(new MailAddress(RequestingUser.EmployeeEmail));
                }
                if (count == 1)
                    ownerMail.CC.Add(new MailAddress(leaveModel.allEmployees[0].EmployeeEmail));

                rejectedMails.Add(ownerMail);
            }
        }
        else
        {
            var viewPermissions = Permissions.Where(x => x.PermissionFlag == (byte)PermissionFlag.View && !x.IsOwner && x.PositionId.HasValue);
            var managers = await EmployeeService.GetEmployeeByPositionNumberAsync(viewPermissions.First().PositionId.Value);
            var employeeDetails = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);
            var ownerMessageBody = string.Format(LeaveAmendmentTemplates.REJECTED_TEMPLATE, employeeDetails.EmployeePreferredFullName, RequestingUser.EmployeePreferredFullName, FormSubject, DbModel.FormInfoId, leaveModel.ReasonForDecision, SummaryHref, SuffixText);
            var messageBody = new MailMessage(FromEmail, employeeDetails.EmployeeEmail, emailSubject, ownerMessageBody);
            if (DbModel.NextApprovalLevel != "!DOT Employee Services Officer Group")
            {
                messageBody.CC.Add(new MailAddress(RequestingUser.EmployeeEmail));
            }
            rejectedMails.Add(messageBody);
        }
        return rejectedMails;
    }
    protected async Task<List<MailMessage>> GetApprovedMail(bool isReminder = false)
    {
        FormSubject = DbModel.AllFormsId == 48 ? FormSubject : FormTypeSubject;
        var approvedMail = new List<MailMessage> { };
        var emailSubject = $"{FormSubject} Request eForm {DbModel.FormInfoId} has been submitted for your review";
        var formOwner = await EmployeeService.GetEmployeeByEmailAsync(Permissions.Single(x => x.IsOwner).Email);
        if (DbModel.NextApprover is LeaveForms.TIER_THREE || DbModel.NextApprover is LeaveForms.DOT_DVS_FIXED_TERM_APPOINTMENT_QA_GROUP_NAME)
        {
            var body = string.Format(LeaveAmendmentTemplates.SUBMITTED_TO_LINEMANAGER_TEMPLATE, DbModel.FormOwnerName, FormSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
            var ownerMail = new MailMessage(FromEmail, DbModel.NextApprover is LeaveForms.TIER_THREE ? formOwner.ExecutiveDirectorIdentifier : LeaveForms.DOT_DVS_FIXED_TERM_APPOINTMENT_QA_GROUP,
                emailSubject, body);
            approvedMail = new List<MailMessage> { ownerMail };
        }
        return approvedMail;
    }

    protected async Task<List<MailMessage>> GetRecalledMail()
    {
        var recallMessages = new List<MailMessage> { };
        var leaveModel = JsonConvert.DeserializeObject<LeaveAmendmentModel>(DbModel.Response);
        FormSubject = DbModel.AllFormsId == 48 ? FormSubject : FormTypeSubject;
        var emailSubject = $" {FormSubject} Request eForm {DbModel.FormInfoId} has been recalled";
        var count = 0;
        if (leaveModel.isLeaveRequesterIsManager is "Yes")
        {
            //if onbehalf is true
            var formOwner = await EmployeeService.GetEmployeeByPositionNumberAsync(FormOwnerPermission.PositionId.Value);

            foreach (var userManagers in formOwner)
            {
                count++;
                var ownerMessageBody = string.Format(LeaveAmendmentTemplates.RECALL_OWNER_TEMPLATE, userManagers.EmployeePreferredFullName, FormSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
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
            var managers = await EmployeeService.GetEmployeeByPositionNumberAsync(viewPermissions.First().PositionId.Value);
            var ownerMessageBody = string.Format(LeaveAmendmentTemplates.RECALL_OWNER_TEMPLATE, RequestingUser.EmployeePreferredFullName, FormSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
            var messageBody = new MailMessage(FromEmail, RequestingUser.EmployeeEmail, emailSubject, ownerMessageBody);
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
    protected async Task<List<MailMessage>> GetDelegatedMail()
    {
        FormSubject = DbModel.AllFormsId == 48 ? FormSubject : FormTypeSubject;
        var formType = (FormType)DbModel.AllFormsId;
        var submittedMails = new List<MailMessage> { };
        var employeeDetails = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);
        var approvingUser = Permissions.Single(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);
        var emailSubject = $"{FormSubject} Request eForm {DbModel.FormInfoId} has been delegated for your review ";
        var body = string.Format(LeaveAmendmentTemplates.DELEGATE_TEMPLATE, DbModel.FormOwnerName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);//string.Format(LeaveAmendmentTemplates.SUBMITTED_TO_EMPLOYEE_TEMPLATE, owner.User.EmployeeFullName, FormTypeBody, SummaryHref, SuffixText);
        var ownerMessage = new MailMessage(FromEmail, DbModel.NextApprover,
        emailSubject, body);
        submittedMails = new List<MailMessage> { ownerMessage };
        return submittedMails;
    }

    protected async Task<List<MailMessage>> GetReminderMail()
    {
        FormSubject = DbModel.AllFormsId == 48 ? FormSubject : FormTypeSubject;
        var ownerUser = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);
        var currentApprover = Permissions.Single(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);
        var approvers = currentApprover.Position.AdfUserPositions.ToList();
        var emailSubject = $"Reminder: {FormSubject} Request eForm {DbModel.FormInfoId} has been submitted for your review ";
        var reminderMails = (from approver in approvers
                             let approverMessageBody =
                                 string.Format(LeaveAmendmentTemplates.SUBMITTED_TO_LINE_MANAGER_TEMPLATE, approver.EmployeeFullName, ownerUser.EmployeePreferredFullName, DbModel.FormInfoId,
                                     SummaryHref, SuffixText)

                             select new MailMessage(FromEmail, approver.EmployeeEmail, emailSubject,
                                 approverMessageBody)).ToList();
        return reminderMails;
    }
    protected async Task<List<MailMessage>> GetEscalatedMail()
    {
        FormSubject = DbModel.AllFormsId == 48 ? FormSubject : FormTypeSubject;
        var escalationMail = new List<MailMessage> { };
        var emailSubject = $"Escalation: {FormSubject} Request eForm {DbModel.FormInfoId} has been has escalated for your review";
        if (CurrentApprovers.Single().PositionId != null)
        {
            var approvers = await EmployeeService.GetEmployeeByPositionNumberAsync(CurrentApprovers.Single().PositionId.Value);
            var employeeDetails = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);
            var count = 0;
            foreach (var manager in approvers)
            {
                count++;
                var managerMessageBody = string.Format(LeaveAmendmentTemplates.ESCALATION_MANAGER_TEMPLATE, manager.EmployeePreferredFullName, employeeDetails.EmployeePreferredFullName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
                var managerMessage = new MailMessage(FromEmail, manager.EmployeeEmail, emailSubject, managerMessageBody);
                escalationMail.Add(managerMessage);
                if (LeaveForms.LAST_APPROVER_EMAILID != "" && count == 1)
                {
                    managerMessage.CC.Add(new MailAddress(HomeGaraging.lastApproverEmailid));
                }
            }
        }
        else
        {
            var groupemployeeDetails = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);
            var managerMessageBody = string.Format(LeaveAmendmentTemplates.ESCALATION_PODGROUP_TEMPLATE, groupemployeeDetails.EmployeePreferredFullName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
            var managerMessage = new MailMessage(FromEmail, LeaveForms.POD_EFFORMS_BUSINESS_ADMIN_GROUP_EMAIL, emailSubject, managerMessageBody);
            escalationMail.Add(managerMessage);
            if (LeaveForms.LAST_APPROVER_EMAILID != "")
            {
                managerMessage.CC.Add(new MailAddress(HomeGaraging.lastApproverEmailid));
            }
        }
        return escalationMail;
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