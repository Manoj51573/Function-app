using DoT.Infrastructure.Interfaces;
using eforms_middleware.Constants;
using eforms_middleware.Constants.COI;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace eforms_middleware.MessageBuilders;

// TODO this should be fine as not setting next approver but will need the new Employee Service
public class CoIOtherMessageBuilder : CoiMessageBuilder
{
    private readonly ILogger<CoIOtherMessageBuilder> _logger;
    protected override string EditPath => "coi-other";
    protected override string SummaryPath => "coi-other/summary";
    protected override string FormTypeSubject => "Other";
    protected override string FormTypeBody => "other circumstances";
    protected override string SuffixText => "ODG Governance and Audit via " +
                                            "<a href=\"mailto:odggovernanceandaudit@transport.wa.gov.au\">odggovernanceandaudit@transport.wa.gov.au</a>";

    public CoIOtherMessageBuilder(IConfiguration configuration, ILogger<CoIOtherMessageBuilder> logger, IRequestingUserProvider requestingUserProvider, IPermissionManager permissionManager, IEmployeeService employeeService)
        : base(configuration, requestingUserProvider, permissionManager, employeeService)
    {
        _logger = logger;
    }

    private async Task<List<MailMessage>> GetEscalatedToOdgMail()
    {
        var body = string.Empty;
        var formOwner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var isFormOwnerReportToT1 = formOwner != null && formOwner.Managers.Any(x => x.EmployeeManagementTier is 1);
        var mail = new MailMessage();
        if (isFormOwnerReportToT1)
        {
            body += string.Format(CoITemplates.SUBMITTED_DG_TEMPLATE, formOwner.EmployeePreferredFullName, SummaryHref);
            var isEdOdgOwner = formOwner.EmployeePositionId == ConflictOfInterest.ED_ODG_POSITION_ID;
            var eDOdgUserList = await EmployeeService.GetEmployeeByPositionNumberAsync(ConflictOfInterest.ED_ODG_POSITION_ID);
            var toEmail = eDOdgUserList.GetDescription();
            if (isEdOdgOwner)
            {
                toEmail = ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL;
            }
            mail = new MailMessage(FromEmail, toEmail,
                    $"{FormTypeSubject} - Conflict of interest declaration - for approval", body);
            mail.CC.Add(formOwner.EmployeeEmail);
        }
        else
        {
            body = string.Format(CoIOtherTemplates.ESCALATION_TEMPLATE, ConflictOfInterest.ODG_AUDIT_GROUP_NAME,
               formOwner.EmployeePreferredFullName, FormTypeBody, SummaryHref, SuffixText);
            mail = new MailMessage(FromEmail, ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL,
                $"Reminder: {FormTypeSubject} - Conflict of interest declaration - for approval", body);

            foreach (var manager in formOwner.ExecutiveDirectors)
            {
                mail.CC.Add(manager.EmployeeEmail);
            }
        }


        return new List<MailMessage> { mail };
    }

    protected override async Task<List<MailMessage>> GetEndorsedMail()
    {
        var formOwner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var nextApproverPermission =
            CurrentApprovers.Single(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);
        if (!nextApproverPermission.GroupId.HasValue || nextApproverPermission.GroupId != ConflictOfInterest.ODG_AUDIT_GROUP_ID)
        {
            return await base.GetEndorsedMail();
        }

        var employeeDetails = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var body = string.Format(CoIOtherTemplates.APPROVED_BY_TIER_THREE_TEMPLATE,
            "ODG Governance & Audit", FormTypeBody, RequestingUser.EmployeePreferredFullName,
            SummaryHref, SuffixText, formOwner.EmployeePreferredFullName);
        var mail = new MailMessage(FromEmail, ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL,
            $"{FormTypeSubject} - Conflict of interest declaration - for QA review", body);

        return new List<MailMessage> { mail };
    }

    protected async Task<List<MailMessage>> IndependentReviewCompleted()
    {
        var formOwner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var employeeDetails = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var body = string.Format(CoIOtherTemplates.INDEPENDENT_COMPLETE_TEMPLATE, "ODG Governance & Audit", formOwner.EmployeePreferredFullName, SummaryHref, SuffixText, RequestingUser.EmployeePreferredFullName);
        var mail = new MailMessage(FromEmail, ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL,
            $"{FormTypeSubject} - Conflict of interest declaration for completion", body);

        return new List<MailMessage> { mail };
    }

    private async Task<List<MailMessage>> GetSpecialReminder()
    {
        var formOwner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var body = string.Format(CoIOtherTemplates.SPECIAL_REMINDER_TEMPLATE, formOwner.EmployeePreferredFullName, DbModel.CompletedDate!.Value.ToString("dd/MM/yyyy"), SummaryHref);
        var mail = new MailMessage(FromEmail, ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL,
            "Conflict of Interest (Other) declaration reminder", body);
        return new List<MailMessage> { mail };
    }

    private async Task<List<MailMessage>> GetIndependentReviewMail()
    {
        var formOwner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var actioningPermission = Permissions.Single(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);
        var independentReviewer = await EmployeeService.GetEmployeeByAzureIdAsync(actioningPermission.UserId!.Value);
        var body = string.Format(CoIOtherTemplates.INDEPENDENT_REVIEW_TEMPLATE, independentReviewer.EmployeePreferredFullName, DbModel.FormOwnerName, SummaryHref, formOwner.EmployeePreferredFullName);
        var mail = new MailMessage(FromEmail, DbModel.NextApprover,
            "Other - Conflict of interest declaration for independent review", body);
        return new List<MailMessage> { mail };
    }

    private async Task<List<MailMessage>> GetEscalatedMail()
    {
        var formOwner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var escalationMail = (from executiveDirector in formOwner.ExecutiveDirectors
                              let emailBody =
                                  string.Format(CoIOtherTemplates.ESCALATION_TEMPLATE, executiveDirector.EmployeePreferredFullName,
                                      formOwner.EmployeePreferredFullName, FormTypeBody, SummaryHref, SuffixText)
                              select new MailMessage(FromEmail, executiveDirector.EmployeeEmail,
                                  $"{FormTypeSubject} - Conflict of interest declaration - for your review", emailBody)).ToList();
        escalationMail.AddRange(
            from manager in formOwner.Managers.Where(m => Permissions.Any(p => p.PositionId == m.EmployeePositionId))
            let body = string.Format(CoIOtherTemplates.ESCALATION_MANAGER_TEMPLATE, manager.EmployeePreferredFullName,
                formOwner.EmployeePreferredFullName, FormTypeBody, formOwner.ExecutiveDirectorName, SummaryHref, SuffixText)
            select new MailMessage(FromEmail, manager.EmployeeEmail,
                $"{FormTypeSubject} - Conflict of interest declaration - for approval", body));

        return escalationMail;
    }

    private async Task<List<MailMessage>> GetDelegateReviewMail()
    {
        var actioningPermission = Permissions.Single(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);

        var formOwner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var independentReviewer = await EmployeeService.GetEmployeeByAzureIdAsync(actioningPermission.UserId!.Value);
        var body = string.Format(CoIOtherTemplates.DELEGATE_REVIEW_TEMPLATE, independentReviewer.EmployeePreferredFullName, DbModel.FormOwnerName, SummaryHref, formOwner.EmployeePreferredFullName);
        var mail = new MailMessage(FromEmail, DbModel.NextApprover,
            "Other - Conflict of interest declaration for your review", body);
        return new List<MailMessage> { mail };
    }
    protected override async Task<List<MailMessage>> GetCompletedMail(bool ccManager = true)
    {
        var formOwner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var isFormOwnerReportToT1 = formOwner != null && formOwner.Managers.Any(x => x.EmployeeManagementTier is 1);
        var completedMail = new List<MailMessage>();
        var isFormOwnerEdODG = formOwner.EmployeePositionId == ConflictOfInterest.ED_ODG_POSITION_ID;
        var body = string.Empty;
        if (!isFormOwnerReportToT1)
        {
            if (!isFormOwnerEdODG)
            {
                body = string.Format(CoIOtherTemplates.COMPLETED_TEMPLATE, formOwner.EmployeePreferredFullName, FormTypeBody,
                    SummaryHref, SuffixText, RequestingUser.EmployeePreferredFullName, RequestingUser.Directorate);
            }
            else
            {
                body = string.Format(CoIOtherTemplates.COMPLETED_ED_ODG_TEMPLATE, formOwner.EmployeePreferredFullName, FormTypeBody,
                 SummaryHref, SuffixText, RequestingUser.EmployeePreferredFullName);
            }
            var mail = new MailMessage(FromEmail, formOwner.EmployeeEmail,
                $"{FormTypeSubject} - Conflict of interest declaration approved", body);
            completedMail.Add(mail);
            if (!ccManager) return completedMail;
            var managers = formOwner.Managers.Where(m => Permissions.Any(p => m.EmployeePositionId == p.PositionId));
            foreach (var userManagers in managers)
            {
                mail.CC.Add(new MailAddress(userManagers.EmployeeEmail));
            }
            mail.CC.Add(new MailAddress(ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL));
        }
        else
        {
            body += string.Format(CoITemplates.APPROVED_DG_TEMPLATE, formOwner.EmployeePreferredFullName, SummaryHref);
            var mail = new MailMessage(FromEmail, formOwner.EmployeeEmail,
                    "Conflict of interest declaration outcome", body);
            var isEdOdgOwner = formOwner.EmployeePositionId == ConflictOfInterest.ED_ODG_POSITION_ID;
            var eDOdgUserList = await EmployeeService.GetEmployeeByPositionNumberAsync(ConflictOfInterest.ED_ODG_POSITION_ID);
            var ccEmail = eDOdgUserList.GetDescription();
            if (isEdOdgOwner)
            {
                ccEmail = ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL;
            }
            mail.CC.Add(ccEmail);
            completedMail.Add(mail);
        }

        return completedMail;
    }
    private async Task<List<MailMessage>> GetRecalledMail()
    {
        var formOwner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var ownerMessageBody = string.Format(CoIOtherTemplates.RECALL_OWNER_TEMPLATE, formOwner.EmployeePreferredFullName, EditHref, SuffixText, FormTypeBody);
        var ownerMail = new MailMessage(FromEmail, formOwner.EmployeeEmail,
            $"{FormTypeSubject} - Conflict of Interest declaration recalled", ownerMessageBody);
        var recallMessages = new List<MailMessage> { ownerMail };
        recallMessages.AddRange(
            from manager in formOwner.Managers.Where(m => Permissions.Any(p => p.PositionId == m.EmployeePositionId))
            let managerMessageBody =
                string.Format(CoIOtherTemplates.RECALL_TEMPLATE, manager.EmployeePreferredFullName, formOwner.EmployeePreferredFullName, SummaryHref,
                    SuffixText)
            select new MailMessage(FromEmail, manager.EmployeeEmail,
                $"{FormTypeSubject} - Conflict of Interest declaration recalled", managerMessageBody));
        recallMessages.AddRange(
            from executiveDirector in
                formOwner.ExecutiveDirectors.Where(m => Permissions.Any(p => p.PositionId == m.EmployeePositionId))
            let executiveDirectorBody =
                string.Format(CoIOtherTemplates.RECALL_TEMPLATE, executiveDirector.EmployeePreferredFullName, formOwner.EmployeePreferredFullName,
                    SummaryHref, SuffixText)
            select new MailMessage(FromEmail, executiveDirector.EmployeeEmail,
                $"{FormTypeSubject} - Conflict of Interest declaration recalled", executiveDirectorBody));

        if (Permissions.All(x => x.PositionId != ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID))
            return recallMessages;
        var executiveDirectorsPAndC =
            await EmployeeService.GetEmployeeByPositionNumberAsync(ConflictOfInterest
                .ED_PEOPLE_AND_CULTURE_POSITION_ID);
        recallMessages.AddRange(from edPAndC in executiveDirectorsPAndC
                                let edPAndCBody =
                                    string.Format(CoIOtherTemplates.RECALL_TEMPLATE, edPAndC.EmployeePreferredFullName, formOwner.EmployeePreferredFullName, SummaryHref,
                                        SuffixText)
                                select new MailMessage(FromEmail, edPAndC.EmployeeEmail,
                                    $"{FormTypeSubject} - Conflict of Interest declaration recalled", edPAndCBody));

        return recallMessages;
    }
    private async Task<List<MailMessage>> GetRejectedMail()
    {
        var formOwner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        // if it is not the manager that actioned this step it must be a not endorse instead
        // The requesting user has to have a position id because they are rejecting
        if (formOwner.ManagerPositionId != RequestingUser.EmployeePositionId)
        {
            var emailManagerPosition = Permissions.Any(p => p.PositionId.HasValue && p.PositionId == formOwner.ManagerPositionId);
            return await GetNotEndorsedMail(formOwner, emailManagerPosition);
        }
        var body = string.Format(CoIOtherTemplates.REJECTED_TEMPLATE, formOwner.EmployeePreferredFullName, FormTypeBody, EditHref, SuffixText);
        var mail = new MailMessage(FromEmail, formOwner.EmployeeEmail,
            $"{FormTypeSubject} - Conflict of interest declaration not endorsed by line manager", body);

        return new List<MailMessage> { mail };
    }
    protected Task<List<MailMessage>> GetNotEndorsedMail(IUserInfo formOwner, bool emailManagerPosition)
    {
        // Get manager/s and email for each
        var form = JsonConvert.DeserializeObject<CoIForm>(DbModel.Response);
        var body = string.Format(CoIOtherTemplates.NOT_ENDORSED_TEMPLATE, formOwner.EmployeePreferredFullName, FormTypeBody, EditHref, SuffixText);
        var mail = new MailMessage(FromEmail, formOwner.EmployeeEmail,
            $"{FormTypeSubject} - Conflict of interest declaration not approved by senior management", body);
        // Check permissions for the position number of the manager as we don't want to send an email to someone
        // who can't view the email
        var list = new List<MailMessage> { mail };
        if (!emailManagerPosition || form.ManagerForm == null) return Task.FromResult(list);
        list.AddRange(from ownerManager in formOwner.Managers
                      let managerBody =
                          string.Format(CoIOtherTemplates.NOT_ENDORSED_MANAGER_TEMPLATE, ownerManager.EmployeePreferredFullName,
                              formOwner.EmployeePreferredFullName, FormTypeBody, SummaryHref, SuffixText)
                      select new MailMessage(FromEmail, ownerManager.EmployeeEmail,
                          $"{FormTypeSubject} - Conflict of interest declaration not approved by senior management",
                          managerBody));

        return Task.FromResult(list);
    }


    private async Task<List<MailMessage>> GetApprovedMail(bool isReminder = false)
    {
        var mail = new List<MailMessage>();
        var owner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var usersInPosition = await EmployeeService.GetEmployeeByPositionNumberAsync(CurrentApprovers.Single().PositionId!.Value);
        foreach (var user in usersInPosition)
        {
            var body = string.Format(CoIOtherTemplates.APPROVED_TEMPLATE,
                user.EmployeePreferredFullName, FormTypeBody, owner.EmployeePreferredFullName,
                SummaryHref, SuffixText, RequestingUser.EmployeePreferredFullName);
            var subject = $"{FormTypeSubject} - Conflict of interest declaration - for your review";
            subject = isReminder ? $"Reminder: {subject}" : subject;
            mail.Add(new MailMessage(FromEmail, user.EmployeeEmail, subject, body));
        }

        return mail;
    }
    protected async Task<List<MailMessage>> GetSubmittedMail()
    {
        var formOwner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var isFormOwnerReportToT1 = formOwner != null && formOwner.Managers.Any(x => x.EmployeeManagementTier is 1);
        // To manager or to ED here
        var isManager = CurrentApprovers.Single().PositionId!.Value == formOwner.ManagerPositionId;
        var managerDetails = GetSubmittedEmailFields(isManager);
        var submittedMails = new List<MailMessage>();
        var toCollection = isManager ? formOwner.Managers : formOwner.ExecutiveDirectors;
        if (!isFormOwnerReportToT1)
        {
            var body = string.Format(CoIOtherTemplates.SUBMITTED_TO_EMPLOYEE_TEMPLATE, formOwner.EmployeePreferredFullName, FormTypeBody, SummaryHref, SuffixText);
            var ownerMessage = new MailMessage(FromEmail, DbModel.FormOwnerEmail,
                $"{FormTypeSubject} - Conflict of interest declaration submitted", body);
            submittedMails.Add(ownerMessage);
            foreach (var addressee in toCollection)
            {
                var managerMessageBody = string.Format(managerDetails.Item1, addressee.EmployeePreferredFullName,
                    formOwner.EmployeePreferredFullName, FormTypeBody, SummaryHref, SuffixText);
                var managerMessage = new MailMessage(FromEmail, addressee.EmployeeEmail, managerDetails.Item2, managerMessageBody);
                submittedMails.Add(managerMessage);
            }
        }
        else
        {
            var body = string.Format(CoITemplates.SUBMITTED_DG_TEMPLATE, formOwner.EmployeePreferredFullName, SummaryHref);
            var isEdOdgOwner = formOwner.EmployeePositionId == ConflictOfInterest.ED_ODG_POSITION_ID;
            var eDOdgUserList = await EmployeeService.GetEmployeeByPositionNumberAsync(ConflictOfInterest.ED_ODG_POSITION_ID);
            var toEmail = eDOdgUserList.GetDescription();
            if (isEdOdgOwner)
            {
                toEmail = ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL;
            }
            var mail = new MailMessage(FromEmail, toEmail,
                    $"{FormTypeSubject} - Conflict of interest declaration - for approval", body);
            mail.CC.Add(formOwner.EmployeeEmail);

            submittedMails.Add(mail);
        }
        return submittedMails;
    }

    protected Tuple<string, string> GetSubmittedEmailFields(bool isManager)
    {
        return isManager
            ? new Tuple<string, string>(CoIOtherTemplates.SUBMITTED_TO_LINE_MANAGER_TEMPLATE, $"{FormTypeSubject} - Conflict of Interest declaration for your review")
            : new Tuple<string, string>(CoIOtherTemplates.SUBMITTED_WITHOUT_MANAGER_TEMPLATE, $"{FormTypeSubject} - Conflict of interest declaration - for approval");
    }

    private async Task<List<MailMessage>> GetCancelledEmail()
    {
        var owningUser = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var body = string.Format(CoIOtherTemplates.CANCELLED_TEMPLATE, owningUser.EmployeePreferredFullName, FormTypeBody, EditHref, SuffixText);
        var mail = new MailMessage(FromEmail, owningUser.EmployeeEmail,
            $"{FormTypeSubject} - Conflict of Interest declaration has cancelled", body);
        var cancelledMail = new List<MailMessage> { mail };
        cancelledMail.AddRange(
            from manager in owningUser.Managers.Where(x => Permissions.Any(p => p.PositionId == x.EmployeePositionId))
            let managerBody =
                string.Format(CoIOtherTemplates.CANCELLED_TO_MANAGER_TEMPLATE, manager.EmployeePreferredFullName, owningUser.EmployeePreferredFullName,
                    FormTypeBody, SummaryHref, SuffixText)
            select new MailMessage(FromEmail, manager.EmployeeEmail,
                $"{FormTypeSubject} - Conflict of Interest declaration has cancelled", managerBody));

        return cancelledMail;
    }
    private async Task<List<MailMessage>> GetReminderMail()
    {
        var formOwner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var currentApprovers =
            await EmployeeService.GetEmployeeByPositionNumberAsync(CurrentApprovers.Single().PositionId!.Value);
        var managerInfo = GetSubmittedEmailFields(currentApprovers.First().EmployeeManagementTier is > 3);
        var reminderMails = (from approver in currentApprovers
                             let approverMessageBody =
                                 string.Format(managerInfo.Item1, approver.EmployeePreferredFullName, formOwner.EmployeePreferredFullName, FormTypeBody,
                                     SummaryHref, SuffixText)
                             select new MailMessage(FromEmail, approver.EmployeeEmail, $"Reminder: {managerInfo.Item2}",
                                 approverMessageBody)).ToList();
        // Reminder is after the 3 days. We will know the current approver but not if there is a manager
        if (DbModel.FormStatusId == (int)FormStatus.Submitted) return reminderMails;
        reminderMails.AddRange(
            from manager in formOwner.Managers.Where(m => Permissions.Any(p => p.PositionId == m.EmployeePositionId))
            let ownerManagerBody =
                string.Format(managerInfo.Item1, manager.EmployeePreferredFullName, formOwner.EmployeePreferredFullName, FormTypeBody,
                    SummaryHref, SuffixText)
            select new MailMessage(FromEmail, manager.EmployeeEmail, $"Reminder: {managerInfo.Item2}",
                ownerManagerBody));
        return reminderMails;
    }
    protected override async Task<List<MailMessage>> GetMessageInternalAsync()
    {
        try
        {
            _logger.LogInformation("Processing mail request for form {FormInfoId}", DbModel.FormInfoId);
            var threeDaysAgo = DateTime.Today.AddDays(-3);
            _logger.LogInformation("Date for reminders set as {ThreeDaysAgo}", threeDaysAgo);
            var messages = new List<MailMessage>();
            var action = Enum.Parse<FormStatus>(Request.FormAction);
            messages = action switch
            {
                // This implies that it should go straight to the ODG_Audit_Group on submission
                FormStatus.Submitted when CurrentApprovers.SingleOrDefault()?.GroupId == ConflictOfInterest.ODG_AUDIT_GROUP_ID => await GetEscalatedToOdgMail(),
                FormStatus.Submitted when DbModel.Modified.HasValue && DbModel.Modified.Value.Date <= threeDaysAgo =>
                    await GetReminderMail(),
                FormStatus.Approved when DbModel.Modified.HasValue && DbModel.Modified.Value.Date <= threeDaysAgo =>
                    await GetApprovedMail(true),
                FormStatus.Submitted when DbModel.FormStatusId == (int)FormStatus.Unsubmitted =>
                    await GetCancelledEmail(),
                FormStatus.Submitted when DbModel.FormStatusId == (int)FormStatus.Endorsed => await GetEndorsedMail(),
                FormStatus.Submitted when DbModel.FormStatusId == (int)FormStatus.Submitted => await GetSubmittedMail(),
                FormStatus.Approved when DbModel.FormStatusId == (int)FormStatus.Approved => await GetApprovedMail(),
                FormStatus.Approved when DbModel.FormStatusId == (int)FormStatus.Endorsed => await GetEndorsedMail(),
                FormStatus.Approved when DbModel.FormStatusId == (int)FormStatus.Completed => await GetCompletedMail(
                    ccManager: false),
                FormStatus.Endorsed when DbModel.FormStatusId == (int)FormStatus.Completed => await GetCompletedMail(),
                FormStatus.Endorsed => await GetEndorsedMail(),
                FormStatus.IndependentReviewCompleted => await IndependentReviewCompleted(),
                // This is based on a Form_Task that is created from a user set property. As long as the completed date
                // is in the past it is fine
                FormStatus.Completed when DbModel.CompletedDate < DateTime.Today => await GetSpecialReminder(),
                FormStatus.Completed => await GetCompletedMail(),
                FormStatus.Rejected => await GetRejectedMail(),
                FormStatus.Recall => await GetRecalledMail(),
                // This is definitely escalation
                FormStatus.Escalated when CurrentApprovers.SingleOrDefault()?.GroupId == ConflictOfInterest.ODG_AUDIT_GROUP_ID => await GetEscalatedToOdgMail(),
                FormStatus.Escalated => await GetEscalatedMail(),
                FormStatus.IndependentReview => await GetIndependentReviewMail(),
                FormStatus.Delegated => await GetDelegateReviewMail(),
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