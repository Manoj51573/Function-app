using DoT.Infrastructure.Interfaces;
using eforms_middleware.Constants;
using eforms_middleware.Constants.COI;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace eforms_middleware.MessageBuilders;

public abstract class CoiMessageBuilder : MessageBuilder
{
    protected abstract string FormTypeSubject { get; }
    protected abstract string FormTypeBody { get; }
    protected virtual string SuffixText => "Employee Services " +
                                          "on (08) 6551 6888 / " +
                                          "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a>";

    protected CoiMessageBuilder(
        IConfiguration configuration,
        IRequestingUserProvider requestingUserProvider, IPermissionManager permissionManager, IEmployeeService employeeService)
        : base(configuration, requestingUserProvider, permissionManager, employeeService)
    {
    }

    protected Tuple<string, string> GetSubmittedEmailFields(bool isManager)
    {
        return isManager
            ? new Tuple<string, string>(CoITemplates.SUBMITTED_TO_LINE_MANAGER_TEMPLATE, $"{FormTypeSubject} - Conflict of Interest declaration for your review")
            : new Tuple<string, string>(CoITemplates.SUBMITTED_WITHOUT_MANAGER_TEMPLATE, $"{FormTypeSubject} - Conflict of interest declaration - for approval");
    }

    protected async Task<List<MailMessage>> GetSubmittedMail()
    {
        var formOwner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var isFormOwnerReportToT1 = formOwner != null && formOwner.Managers.Any(x => x.EmployeeManagementTier is 1);
        var submittedMails = new List<MailMessage>();
        if (!isFormOwnerReportToT1)
        {
            var body = string.Format(CoITemplates.SUBMITTED_TO_EMPLOYEE_TEMPLATE, formOwner.EmployeePreferredFullName, FormTypeBody, SummaryHref, SuffixText);
            var ownerMessage = new MailMessage(FromEmail, DbModel.FormOwnerEmail,
                $"{FormTypeSubject} - Conflict of interest declaration submitted", body);
            // To manager or to ED here
            var isManager = CurrentApprovers.Single().PositionId!.Value == formOwner.ManagerPositionId;
            var managerDetails = GetSubmittedEmailFields(isManager);

            var toCollection = isManager ? formOwner.Managers : formOwner.ExecutiveDirectors;
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



    protected async Task<List<MailMessage>> GetReminderMail()
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

    protected async Task<List<MailMessage>> GetRecalledMail()
    {
        var formOwner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var ownerMessageBody = string.Format(CoITemplates.RECALL_OWNER_TEMPLATE, formOwner.EmployeePreferredFullName, EditHref, SuffixText);
        var ownerMail = new MailMessage(FromEmail, formOwner.EmployeeEmail,
            $"{FormTypeSubject} - Conflict of Interest declaration recalled", ownerMessageBody);
        var recallMessages = new List<MailMessage> { ownerMail };
        recallMessages.AddRange(
            from manager in formOwner.Managers.Where(m => Permissions.Any(p => p.PositionId == m.EmployeePositionId))
            let managerMessageBody =
                string.Format(CoITemplates.RECALL_TEMPLATE, manager.EmployeePreferredFullName, formOwner.EmployeePreferredFullName, SummaryHref,
                    SuffixText)
            select new MailMessage(FromEmail, manager.EmployeeEmail,
                $"{FormTypeSubject} - Conflict of Interest declaration recalled", managerMessageBody));
        recallMessages.AddRange(
            from executiveDirector in
                formOwner.ExecutiveDirectors.Where(m => Permissions.Any(p => p.PositionId == m.EmployeePositionId))
            let executiveDirectorBody =
                string.Format(CoITemplates.RECALL_TEMPLATE, executiveDirector.EmployeePreferredFullName, formOwner.EmployeePreferredFullName,
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
                                    string.Format(CoITemplates.RECALL_TEMPLATE, edPAndC.EmployeePreferredFullName, formOwner.EmployeePreferredFullName, SummaryHref,
                                        SuffixText)
                                select new MailMessage(FromEmail, edPAndC.EmployeeEmail,
                                    $"{FormTypeSubject} - Conflict of Interest declaration recalled", edPAndCBody));

        return recallMessages;
    }

    protected async Task<List<MailMessage>> GetEscalatedMail()
    {
        var formOwner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var escalationMail = (from executiveDirector in formOwner.ExecutiveDirectors
                              let emailBody =
                                  string.Format(CoITemplates.ESCALATION_TEMPLATE, executiveDirector.EmployeePreferredFullName,
                                      formOwner.EmployeePreferredFullName, FormTypeBody, SummaryHref, SuffixText)
                              select new MailMessage(FromEmail, executiveDirector.EmployeeEmail,
                                  $"{FormTypeSubject} - Conflict of interest declaration - for approval", emailBody)).ToList();
        escalationMail.AddRange(
            from manager in formOwner.Managers.Where(m => Permissions.Any(p => p.PositionId == m.EmployeePositionId))
            let body = string.Format(CoITemplates.ESCALATION_MANAGER_TEMPLATE, manager.EmployeePreferredFullName,
                formOwner.EmployeePreferredFullName, FormTypeBody, formOwner.ExecutiveDirectorName, SummaryHref, SuffixText)
            select new MailMessage(FromEmail, manager.EmployeeEmail,
                $"{FormTypeSubject} - Conflict of interest declaration - for approval", body));

        return escalationMail;
    }

    protected async Task<List<MailMessage>> GetApprovedMail(bool isReminder = false)
    {
        var mail = new List<MailMessage>();
        var owner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var usersInPosition = await EmployeeService.GetEmployeeByPositionNumberAsync(CurrentApprovers.Single().PositionId!.Value);
        foreach (var user in usersInPosition)
        {
            var body = string.Format(CoITemplates.APPROVED_TEMPLATE, user.EmployeePreferredFullName, FormTypeBody, owner.EmployeePreferredFullName,
                SummaryHref, SuffixText);
            var subject = $"{FormTypeSubject} - Conflict of interest declaration - for approval";
            subject = isReminder ? $"Reminder: {subject}" : subject;
            mail.Add(new MailMessage(FromEmail, user.EmployeeEmail, subject, body));
        }

        return mail;
    }

    protected virtual async Task<List<MailMessage>> GetEndorsedMail()
    {
        var mail = new List<MailMessage>();
        var owner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var usersInPosition = await EmployeeService.GetEmployeeByPositionNumberAsync(CurrentApprovers.Single().PositionId!.Value);
        var IsFormOwnerReportToT1 = owner != null && owner.Managers.Any(x => x.EmployeeManagementTier is 1);
        foreach (var user in usersInPosition)
        {
            var body = string.Format(CoITemplates.ENDORSED_TEMPLATE, user.EmployeePreferredFullName, owner.EmployeePreferredFullName, FormTypeBody,
                SummaryHref, SuffixText);
            mail.Add(new MailMessage(FromEmail, user.EmployeeEmail,
                $"{FormTypeSubject} - Conflict of interest declaration - for approval", body));
        }

        return mail;
    }

    protected virtual async Task<List<MailMessage>> GetCompletedMail(bool ccManager = true)
    {
        var formOwner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var isFormOwnerReportToT1 = formOwner != null && formOwner.Managers.Any(x => x.EmployeeManagementTier is 1);
        if (!isFormOwnerReportToT1)
        {
            var body = string.Format(CoITemplates.COMPLETED_TEMPLATE, formOwner.EmployeePreferredFullName, FormTypeBody,
                SummaryHref, SuffixText, RequestingUser.EmployeePreferredFullName, RequestingUser.Directorate);
            var mail = new MailMessage(FromEmail, formOwner.EmployeeEmail,
                $"{FormTypeSubject} - Conflict of interest declaration outcome", body);
            var completedMail = new List<MailMessage>() { mail };
            if (!ccManager) return completedMail;
            var managers = formOwner.Managers.Where(m => Permissions.Any(p => m.EmployeePositionId == p.PositionId));
            var emailList = (from manager in managers
                             let managerBody =
                                 string.Format(CoITemplates.COMPLETED_TEMPLATE_TO_MANAGER, manager.EmployeePreferredFullName,
                                     formOwner.EmployeePreferredFullName, FormTypeBody, SummaryHref, SuffixText,
                                     RequestingUser.EmployeePreferredFullName, RequestingUser.Directorate)
                             select new MailMessage(FromEmail, manager.EmployeeEmail,
                                 $"{FormTypeSubject} - Conflict of interest declaration outcome", managerBody)).ToList();
            emailList.Add(mail);
            completedMail.AddRange(emailList);
            return completedMail;
        }
        else
        {
            var body = string.Format(CoITemplates.APPROVED_DG_TEMPLATE, formOwner.EmployeePreferredFullName, SummaryHref);
            var mail = new MailMessage(FromEmail, formOwner.EmployeeEmail,
                    $"{FormTypeSubject} - Conflict of interest declaration outcome", body);
            var isEdOdgOwner = formOwner.EmployeePositionId == ConflictOfInterest.ED_ODG_POSITION_ID;
            var eDOdgUserList = await EmployeeService.GetEmployeeByPositionNumberAsync(ConflictOfInterest.ED_ODG_POSITION_ID);
            var ccEmail = eDOdgUserList.GetDescription();
            var completedMail = new List<MailMessage>() { mail };
            if (isEdOdgOwner)
            {
                ccEmail = ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL;
            }
            mail.CC.Add(ccEmail);
            completedMail.Add(mail);
            return completedMail;
        }

    }

    protected virtual async Task<List<MailMessage>> GetRejectedMail()
    {
        var formOwner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        // if it is not the manager that actioned this step it must be a not endorse instead
        // The requesting user has to have a position id because they are rejecting
        if (formOwner.ManagerPositionId != RequestingUser.EmployeePositionId)
        {
            var emailManagerPosition = Permissions.Any(p => p.PositionId.HasValue && p.PositionId == formOwner.ManagerPositionId);
            return await GetNotEndorsedMail(formOwner, emailManagerPosition);
        }
        var body = string.Format(CoITemplates.REJECTED_TEMPLATE, formOwner.EmployeePreferredFullName, FormTypeBody, EditHref, SuffixText);
        var mail = new MailMessage(FromEmail, formOwner.EmployeeEmail,
            $"{FormTypeSubject} - Conflict of interest declaration not approved by line manager", body);

        return new List<MailMessage> { mail };
    }

    protected async Task<List<MailMessage>> GetCancelledEmail()
    {
        var owningUser = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
        var body = string.Format(CoITemplates.CANCELLED_TEMPLATE, owningUser.EmployeePreferredFullName, FormTypeBody, EditHref, SuffixText);
        var mail = new MailMessage(FromEmail, owningUser.EmployeeEmail,
            $"{FormTypeSubject} - Conflict of Interest declaration has cancelled", body);
        var cancelledMail = new List<MailMessage> { mail };
        cancelledMail.AddRange(
            from manager in owningUser.Managers.Where(x => Permissions.Any(p => p.PositionId == x.EmployeePositionId))
            let managerBody =
                string.Format(CoITemplates.CANCELLED_TO_MANAGER_TEMPLATE, manager.EmployeePreferredFullName, owningUser.EmployeePreferredFullName,
                    FormTypeBody, SummaryHref, SuffixText)
            select new MailMessage(FromEmail, manager.EmployeeEmail,
                $"{FormTypeSubject} - Conflict of Interest declaration has cancelled", managerBody));

        return cancelledMail;
    }

    protected virtual Task<List<MailMessage>> GetNotEndorsedMail(IUserInfo formOwner, bool emailManagerPosition)
    {
        // Get manager/s and email for each
        var form = JsonConvert.DeserializeObject<CoIForm>(DbModel.Response);
        var body = string.Format(CoITemplates.NOT_ENDORSED_TEMPLATE, formOwner.EmployeePreferredFullName, FormTypeBody, EditHref, SuffixText);
        var mail = new MailMessage(FromEmail, formOwner.EmployeeEmail,
            $"{FormTypeSubject} - Conflict of interest declaration not approved by senior management", body);
        // Check permissions for the position number of the manager as we don't want to send an email to someone
        // who can't view the email
        var list = new List<MailMessage> { mail };
        if (!emailManagerPosition || form.ManagerForm == null) return Task.FromResult(list);
        list.AddRange(from ownerManager in formOwner.Managers
                      let managerBody =
                          string.Format(CoITemplates.NOT_ENDORSED_MANAGER_TEMPLATE, ownerManager.EmployeePreferredFullName,
                              formOwner.EmployeePreferredFullName, FormTypeBody, SummaryHref, SuffixText)
                      select new MailMessage(FromEmail, ownerManager.EmployeeEmail,
                          $"{FormTypeSubject} - Conflict of interest declaration not approved by senior management",
                          managerBody));

        return Task.FromResult(list);
    }

    protected override Task<List<MailMessage>> GetMessageInternalAsync()
    {
        throw new System.NotImplementedException();
    }
}