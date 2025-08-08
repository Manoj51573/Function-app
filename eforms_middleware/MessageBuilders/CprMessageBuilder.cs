using eforms_middleware.Constants;
using eforms_middleware.Constants.COI;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace eforms_middleware.MessageBuilders;

public class CprMessageBuilder : CoiMessageBuilder
{
    private readonly ILogger<CprMessageBuilder> _logger;
    protected override string EditPath => "close-personal-relationship";
    protected override string SummaryPath => "close-personal-relationship/summary";
    protected override string FormTypeSubject => "Close personal relationship";
    protected override string FormTypeBody => "Close personal relationship in the workplace";

    public CprMessageBuilder(ILogger<CprMessageBuilder> logger, IConfiguration configuration, IRequestingUserProvider requestingUserProvider, IPermissionManager permissionManager, IEmployeeService employeeService)
        : base(configuration, requestingUserProvider, permissionManager, employeeService)
    {
        _logger = logger;
    }

    private async Task<List<MailMessage>> GetCompletedEmail(bool ccManager = true)
    {
        var formOwner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var isFormOwnerReportToT1 = formOwner != null && formOwner.Managers.Any(x => x.EmployeeManagementTier is 1);
        if (!isFormOwnerReportToT1)
        {
            var body = string.Format(CprTemplates.APPROVED_AND_COMPLETED_TEMPLATE, formOwner.EmployeePreferredFullName, SummaryHref, RequestingUser.EmployeePreferredFullName);
            var mail = new MailMessage(FromEmail, formOwner.EmployeeEmail,
                "Close personal relationship - Conflict of interest declaration outcome", body);
            var includeManager = ccManager && formOwner.Managers.Any();
            var completedMailList = new List<MailMessage>() { mail };
            if (!includeManager) return completedMailList;
            var emailList = (from ownerManager in formOwner.Managers
                             let managerBody =
                                 string.Format(CprTemplates.APPROVED_AND_COMPLETED_TO_MANAGER_TEMPLATE, ownerManager.EmployeePreferredFullName,
                                     formOwner.EmployeePreferredFullName, SummaryHref, RequestingUser.EmployeePreferredFullName)
                             select new MailMessage(FromEmail, ownerManager.EmployeeEmail,
                                 "Close personal relationship - Conflict of interest declaration outcome", managerBody)).ToList();
            completedMailList.AddRange(emailList);
            return completedMailList;
        }
        else
        {

            var body = string.Format(CoITemplates.APPROVED_DG_TEMPLATE, formOwner.EmployeePreferredFullName, SummaryHref);
            var mail = new MailMessage(FromEmail, formOwner.EmployeeEmail,
                    $"Close personal relationship - Conflict of interest declaration outcome", body);
            var completedMailList = new List<MailMessage>() { mail };
            var isEdOdgOwner = formOwner.EmployeePositionId == ConflictOfInterest.ED_ODG_POSITION_ID;
            var eDOdgUserList = await EmployeeService.GetEmployeeByPositionNumberAsync(ConflictOfInterest.ED_ODG_POSITION_ID);
            var ccEmail = eDOdgUserList.GetDescription();
            if (isEdOdgOwner)
            {
                ccEmail = ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL;
            }
            mail.CC.Add(ccEmail);
            completedMailList.Add(mail);
            return completedMailList;
        }

    }

    private async Task<List<MailMessage>> GetSubmittedToEdPAndCMail(bool isReminder = false)
    {
        var owner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var isFormOwnerReportToT1 = owner != null && owner.Managers.Any(x => x.EmployeeManagementTier is 1);
        var submittedMessages = new List<MailMessage>();
        if (!isFormOwnerReportToT1)
        {
            var ownerBody = string.Format(CprTemplates.SUBMITTED_TO_ED_TEMPLATE, owner.EmployeePreferredFullName, SummaryHref);
            var ownerMessage = new MailMessage(FromEmail, owner.EmployeeEmail,
                "Close personal relationship - Conflict of interest declaration submitted", ownerBody);
            var edPandCs = await EmployeeService.GetEmployeeByPositionNumberAsync(ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID);
            foreach (var edPandC in edPandCs)
            {
                var body = string.Format(CprTemplates.DIRECT_TO_ED_P_AND_C_TEMPLATE, edPandC.EmployeePreferredFullName,
                    owner.EmployeePreferredFullName, SummaryHref);
                var edPandCSubject = isReminder
                    ? $"Reminder: Close personal relationship - Conflict of interest declaration - for approval"
                    : "Close personal relationship - Conflict of interest declaration - for approval";
                var edPandCMail = new MailMessage(FromEmail, edPandC.EmployeeEmail, edPandCSubject, body);
                submittedMessages.Add(edPandCMail);
            }
            if (!isReminder) submittedMessages.Add(ownerMessage);
        }
        else
        {
            var body = string.Format(CoITemplates.SUBMITTED_DG_TEMPLATE, owner.EmployeePreferredFullName, SummaryHref);
            var isEdOdgOwner = owner.EmployeePositionId == ConflictOfInterest.ED_ODG_POSITION_ID;
            var eDOdgUserList = await EmployeeService.GetEmployeeByPositionNumberAsync(ConflictOfInterest.ED_ODG_POSITION_ID);
            var toEmail = eDOdgUserList.GetDescription();
            if (isEdOdgOwner)
            {
                toEmail = ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL;
            }
            var mail = new MailMessage(FromEmail, toEmail,
                    $"{FormTypeSubject} - Conflict of interest declaration - for approval", body);
            mail.CC.Add(owner.EmployeeEmail);
            submittedMessages.Add(mail);
        }
        return submittedMessages;
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
            var isEdPAndC = RequestingUser?.EmployeePositionId ==
                            ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID;
            messages = action switch
            {
                FormStatus.Approved when DbModel.Modified.HasValue && DbModel.Modified.Value.Date <= past =>
                    await GetApprovedMail(true),
                FormStatus.Submitted when DbModel.Modified.HasValue && DbModel.Modified.Value.Date <= past && isEdPAndC =>
                    await GetSubmittedToEdPAndCMail(true),
                FormStatus.Submitted when DbModel.Modified.HasValue && DbModel.Modified.Value.Date <= past =>
                    await GetReminderMail(),
                FormStatus.Escalated when DbModel.FormStatusId == (int)FormStatus.Unsubmitted =>
                    await GetCancelledEmail(),
                FormStatus.Submitted when DbModel.FormStatusId == (int)FormStatus.Unsubmitted =>
                    await GetCancelledEmail(),
                FormStatus.Submitted when Request.FormDetails.NextApprover == "ED" => await GetSubmittedToEdPAndCMail(),
                FormStatus.Submitted when DbModel.FormStatusId == (int)FormStatus.Submitted => await GetSubmittedMail(),
                FormStatus.Submitted when DbModel.FormStatusId == (int)FormStatus.Endorsed => await GetEndorsedMail(),
                FormStatus.Approved when DbModel.FormStatusId == (int)FormStatus.Endorsed => await GetEndorsedMail(),
                FormStatus.Endorsed when DbModel.FormStatusId == (int)FormStatus.Completed => await GetCompletedEmail(),
                FormStatus.Endorsed => await GetEndorsedMail(),
                FormStatus.Approved when DbModel.FormStatusId == (int)FormStatus.Completed => await GetCompletedEmail(
                    ccManager: false),
                FormStatus.Approved => await GetApprovedMail(),
                FormStatus.Rejected => await GetRejectedMail(),
                FormStatus.Completed => await GetCompletedEmail(),
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