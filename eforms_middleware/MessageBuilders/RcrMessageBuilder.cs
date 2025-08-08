using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Specifications;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace eforms_middleware.MessageBuilders;
public abstract class RcrMessageBuilder : MessageBuilder
{
    protected readonly IRepository<FormPermission> frmPermissionRepo;
    protected IRepository<TaskInfo> TaskInfoRepo { get; }
    protected abstract string FormTypeSubject { get; }
    protected abstract string FormTypeBody { get; }
    protected virtual string SuffixText => "Employee Services " +
                                          "on 6551 6888 or by email at " +
                                          "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a>";

    public string ESOGroupName = "!DOT Employee Services Officer Group";

    protected RcrMessageBuilder(IConfiguration configuration,
        IRequestingUserProvider requestingUserProvider,
        IPermissionManager permissionManager, IEmployeeService employeeService)
        : base(configuration, requestingUserProvider, permissionManager, employeeService)
    {
    }
    protected async Task<List<MailMessage>> GetSubmittedMail(bool IsReminder = false)
    {
        var submittedMails = new List<MailMessage> { };
        var rosterChangeModel = JsonConvert.DeserializeObject<RosterChangeModel>(DbModel.Response);
        if (!string.IsNullOrEmpty(rosterChangeModel.RosterChangeRequestGroup.OtherRequestorFlag)
            && bool.Parse(rosterChangeModel.RosterChangeRequestGroup.OtherRequestorFlag)
            && rosterChangeModel.RosterChangeRequestGroup.otherRequestor.Any())
        {
            var emailSubject = $"{FormTypeSubject} Request eForm {DbModel.FormInfoId} has been submitted on your behalf";
            emailSubject = IsReminder ? $"Reminder: {emailSubject}" : emailSubject;
            var recipient = rosterChangeModel.RosterChangeRequestGroup.otherRequestor.First();
            var body = string.Format(RosterChangeEmailTemplate.SUBMITTED_TO_EMPLOYEE_TEMPLATE,
                recipient.EmployeeFullName, DbModel.FormOwnerName, DbModel.FormInfoId, SummaryHref, SuffixText);
            var ownerMessage = new MailMessage(FromEmail, recipient.EmployeeEmail, emailSubject, body);
            ownerMessage.CC.Add(DbModel.FormOwnerEmail);
            submittedMails = new List<MailMessage> { ownerMessage };
        }
        else
        {
            var emailSubject = $"{FormTypeSubject} Request eForm {DbModel.FormInfoId} has been submitted for your review";
            emailSubject = IsReminder ? $"Reminder: {emailSubject}" : emailSubject;
            var currentApprover = CurrentApprovers.Single();
            if (currentApprover.PositionId.HasValue)
            {
                var approvers = await EmployeeService.GetEmployeeByPositionNumberAsync(CurrentApprovers.Single().PositionId.Value);
                foreach (var manager in approvers)
                {

                    var managerMessageBody = string.Format(RosterChangeEmailTemplate.SUBMITTED_TO_LINEMANAGER_TEMPLATE,
                                                    DbModel.FormOwnerName, FormTypeSubject,
                                                    DbModel.FormInfoId, SummaryHref, SuffixText);
                    var managerMessage = new MailMessage(FromEmail, manager.EmployeeEmail, emailSubject, managerMessageBody);
                    managerMessage = IncludeAdditionalRecipients(managerMessage, rosterChangeModel);
                    submittedMails.Add(managerMessage);

                }
            }
            else if (!currentApprover.UserId.HasValue && currentApprover.GroupId.HasValue)
            {
                var managerMessageBody = string.Format(RosterChangeEmailTemplate.SUBMITTED_TO_LINEMANAGER_TEMPLATE,
                                DbModel.FormOwnerName, FormTypeSubject,
                                DbModel.FormInfoId, SummaryHref, SuffixText);
                var managerMessage = new MailMessage(FromEmail,
                                                        "darryl.francis@transport.wa.gov.au",
                                                        //RosterChangeRequest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_EMAIL,
                                                        emailSubject, managerMessageBody);
                submittedMails.Add(managerMessage);
            }
        }
        return submittedMails;
    }

    protected async Task<List<MailMessage>> GetRecalledMail()
    {
        var emailSubject = $" {FormTypeSubject} Request eForm {DbModel.FormInfoId} has been recalled";
        var recallMessages = new List<MailMessage> { };
        var rosterChangeModel = JsonConvert.DeserializeObject<RosterChangeModel>(DbModel.Response);
        var approverDetails = await EmployeeService.GetEmployeeByEmailAsync(DbModel.NextApprover);
        if (!string.IsNullOrEmpty(rosterChangeModel.RosterChangeRequestGroup.OtherRequestorFlag)
            && bool.Parse(rosterChangeModel.RosterChangeRequestGroup.OtherRequestorFlag))
        {
            var formOwner = await EmployeeService.GetEmployeeByPositionNumberAsync(FormOwnerPermission.PositionId.Value);

            if (formOwner.Count() == 1)
            {
                var ownerMessageBody = string.Format(RosterChangeEmailTemplate.RECALL_OWNER_TEMPLATE,
                                        RequestingUser.EmployeePreferredFullName,
                                        DbModel.FormInfoId, approverDetails.EmployeePreferredFullName, SummaryHref, SuffixText);
                var ownerMail = new MailMessage(FromEmail, RequestingUser.EmployeeEmail,
                    emailSubject, ownerMessageBody);
                recallMessages = new List<MailMessage> { ownerMail };
            }
            else
            {
                foreach (var userManagers in formOwner)
                {
                    var ownerMessageBody = string.Format(RosterChangeEmailTemplate.RECALL_OWNER_TEMPLATE,
                                            userManagers.EmployeePreferredFullName,
                                            DbModel.FormInfoId, approverDetails.EmployeePreferredFullName, SummaryHref, SuffixText);
                    var ownerMail = new MailMessage(FromEmail, userManagers.EmployeeEmail,
                        emailSubject, ownerMessageBody);
                    recallMessages.Add(ownerMail);
                }

            }
        }
        else
        {
            var viewPermissions = Permissions.Where(x => x.PermissionFlag == (byte)PermissionFlag.View && !x.IsOwner && x.PositionId.HasValue);
            var managers = await EmployeeService.GetEmployeeByPositionNumberAsync(viewPermissions.First().PositionId.Value);

            var ownerMessageBody = string.Format(RosterChangeEmailTemplate.RECALL_OWNER_TEMPLATE, RequestingUser.EmployeePreferredFullName,
                                    DbModel.FormInfoId, approverDetails.EmployeePreferredFullName, SummaryHref, SuffixText);

            var messageBody = new MailMessage(FromEmail, RequestingUser.EmployeeEmail, emailSubject, ownerMessageBody);

            foreach (var manager in managers)
            {
                messageBody.CC.Add(new MailAddress(manager.EmployeeEmail));
            }
            recallMessages.Add(messageBody);
        }

        return recallMessages;
    }
    protected virtual async Task<List<MailMessage>> GetRejectedMail()
    {

        var emailSubject = $"{FormTypeSubject}  Request eForm {DbModel.FormInfoId} has been rejected";
        var rejectedMails = new List<MailMessage> { };
        var rosterChangeModel = JsonConvert.DeserializeObject<RosterChangeModel>(DbModel.Response);

        if (!string.IsNullOrEmpty(rosterChangeModel.RosterChangeRequestGroup.OtherRequestorFlag)
            && bool.Parse(rosterChangeModel.RosterChangeRequestGroup.OtherRequestorFlag))
        {
            var formOwner = await EmployeeService.GetEmployeeByPositionNumberAsync(FormOwnerPermission.PositionId.Value);
            var ccEmailList = rosterChangeModel.SupportingInfoGroup.AdditionalNotification.Select(x => x.EmployeeEmail).ToList();
            if (formOwner.Count() == 1)
            {
                var ownerMessageBody = string.Format(RosterChangeEmailTemplate.REJECTED_TEMPLATE,
                     formOwner.First().EmployeePreferredFullName,
                     RequestingUser.EmployeePreferredFullName,
                     DbModel.FormInfoId, RequestingUser.EmployeePreferredFullName,
                     rosterChangeModel.ReasonForDecision, SummaryHref, SuffixText);
                var ownerMail = new MailMessage(FromEmail, formOwner.First().EmployeeEmail,
                    emailSubject, ownerMessageBody);

                ownerMail.CC.Add(new MailAddress(RequestingUser.EmployeeEmail));
                ccEmailList.ForEach(obj =>
                {
                    ownerMail.CC.Add(obj);
                });
                rejectedMails = new List<MailMessage> { ownerMail };

            }
            else
            {
                foreach (var userManagers in formOwner)
                {
                    var ownerMessageBody = string.Format(RosterChangeEmailTemplate.REJECTED_TEMPLATE,
                        formOwner.First().EmployeePreferredFullName,
                        RequestingUser.EmployeePreferredFullName,
                        DbModel.FormInfoId, RequestingUser.EmployeePreferredFullName,
                        rosterChangeModel.ReasonForDecision, SummaryHref, SuffixText);
                    var ownerMail = new MailMessage(FromEmail, userManagers.EmployeeEmail,
                        emailSubject, ownerMessageBody);
                    rejectedMails.Add(ownerMail);
                    ccEmailList.ForEach(obj =>
                    {
                        ownerMail.CC.Add(obj);
                    });
                    ownerMail.CC.Add(new MailAddress(RequestingUser.EmployeeEmail));

                }
            }


        }
        else
        {
            var viewPermissions = Permissions.Where(x => x.PermissionFlag == (byte)PermissionFlag.View && !x.IsOwner && x.PositionId.HasValue);
            var managers = await EmployeeService.GetEmployeeByPositionNumberAsync(viewPermissions.First().PositionId.Value);
            var employeeDetails = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);

            var ownerMessageBody = string.Format(RosterChangeEmailTemplate.REJECTED_TEMPLATE,
                employeeDetails.EmployeePreferredFullName,
                RequestingUser.EmployeePreferredFullName, DbModel.FormInfoId,
                RequestingUser.EmployeePreferredFullName,
                rosterChangeModel.ReasonForDecision, SummaryHref, SuffixText);
            var messageBody = new MailMessage(FromEmail, employeeDetails.EmployeeEmail, emailSubject, ownerMessageBody);
            foreach (var manager in managers)
            {
                messageBody.CC.Add(new MailAddress(manager.EmployeeEmail));
            }
            var ccEmailList = rosterChangeModel.SupportingInfoGroup.AdditionalNotification.Select(x => x.EmployeeEmail).ToList();
            ccEmailList.ForEach(obj =>
            {
                messageBody.CC.Add(obj);
            });

            rejectedMails.Add(messageBody);
        }
        return rejectedMails;
    }

    protected virtual async Task<List<MailMessage>> GetCompletedEmail(bool ccManager = true)
    {
        var completedMail = new List<MailMessage> { };
        var emailSubject = $" {FormTypeSubject} Request  {DbModel.FormInfoId} has been completed";
        var rosterChangeModel = JsonConvert.DeserializeObject<RosterChangeModel>(DbModel.Response);
        if (!string.IsNullOrEmpty(rosterChangeModel.RosterChangeRequestGroup.OtherRequestorFlag)
            && bool.Parse(rosterChangeModel.RosterChangeRequestGroup.OtherRequestorFlag)
            && rosterChangeModel.RosterChangeRequestGroup.otherRequestor.Any())
        {
            var formOwner = await EmployeeService.GetEmployeeByPositionNumberAsync(FormOwnerPermission.PositionId.Value);
            foreach (var formOwnerUser in formOwner)
            {
                var requestorEmail = rosterChangeModel.RosterChangeRequestGroup.otherRequestor.First();
                var ownerMessageBody = string.Format(RosterChangeEmailTemplate.COMPLETED_TEMPLATE_TO_MANAGER, requestorEmail.EmployeeFullName,
                                            FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
                var messageBody = new MailMessage(FromEmail, requestorEmail.EmployeeEmail, emailSubject, ownerMessageBody);
                messageBody = IncludeAdditionalRecipients(messageBody, rosterChangeModel);
                messageBody.CC.Add(formOwnerUser.EmployeeEmail);
                completedMail.Add(messageBody);
            }
        }
        else
        {
            var employeeDetails = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);
            var recipients = employeeDetails.Managers;
            var body = string.Format(RosterChangeEmailTemplate.COMPLETED_TEMPLATE_TO_MANAGER, DbModel.FormOwnerName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
            var ownerMail = new MailMessage(FromEmail, employeeDetails.EmployeeEmail,
                emailSubject, body);
            ownerMail = IncludeAdditionalRecipients(ownerMail, rosterChangeModel);
            if (recipients.Count != 0)
            {
                foreach (var userManagers in recipients)
                {
                    ownerMail.CC.Add(new MailAddress(userManagers.EmployeeEmail));
                }
            }
            else if (employeeDetails.EmployeeManagementTier is 1)
            {
                var executiveDirectorDetail = await EmployeeService.GetEmployeeByPositionNumberAsync(RosterChangeRequest.ED_ODG_POSITION_ID);
                ownerMail.CC.Add(executiveDirectorDetail.First().EmployeeEmail);
            }
            else
            {

                foreach (var recipient in employeeDetails.ExecutiveDirectors)
                {
                    ownerMail.CC.Add(new MailAddress(recipient.EmployeeEmail));
                }
            }
            completedMail = new List<MailMessage> { ownerMail };
        }
        return completedMail;
    }

    protected async Task<List<MailMessage>> GetDelegatedMail()
    {
        var formType = (FormType)DbModel.AllFormsId;
        var submittedMails = new List<MailMessage> { };
        var employeeDetails = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);
        var approvingUser = Permissions.Single(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);
        var emailSubject = $"{FormTypeSubject} Request eForm {DbModel.FormInfoId} has been delegated for your review ";
        var body = string.Format(RosterChangeEmailTemplate.DELEGATE_TEMPLATE, DbModel.FormOwnerName, DbModel.FormInfoId, SummaryHref, SuffixText);
        var ownerMessage = new MailMessage(FromEmail, DbModel.NextApprover,
        emailSubject, body);
        submittedMails = new List<MailMessage> { ownerMessage };
        return submittedMails;
    }

    protected async Task<List<MailMessage>> GetEscalatedMail()
    {
        var escalationMail = new List<MailMessage> { };
        var emailSubject = $"Escalation: {FormTypeSubject} Request eForm {DbModel.FormInfoId} has been has escalated for your review";
        if (CurrentApprovers.Single().PositionId != null)
        {
            var approvers = await EmployeeService.GetEmployeeByPositionNumberAsync(CurrentApprovers.Single().PositionId.Value);
            var employeeDetails = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);
            foreach (var manager in approvers)
            {
                var managerMessageBody = string.Format(RosterChangeEmailTemplate.ESCALATION_MANAGER_TEMPLATE,
                            manager.EmployeePreferredFullName, employeeDetails.EmployeePreferredFullName,
                            FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
                var managerMessage = new MailMessage(FromEmail, manager.EmployeeEmail, emailSubject, managerMessageBody);
                escalationMail.Add(managerMessage);
            }
        }
        else
        {
            var groupemployeeDetails = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);
            var managerMessageBody = string.Format(RosterChangeEmailTemplate.ESCALATION_PODGROUP_TEMPLATE,
                            groupemployeeDetails.EmployeePreferredFullName, FormTypeSubject,
                            DbModel.FormInfoId, SummaryHref, SuffixText);
            var managerMessage = new MailMessage(FromEmail, RosterChangeRequest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_EMAIL,
                                                    emailSubject, managerMessageBody);
            escalationMail.Add(managerMessage);
        }
        return escalationMail;
    }

    protected List<MailMessage> GetApprovedMail(bool IsReminder = false)
    {
        var completedMail = new List<MailMessage> { };
        var emailSubject = $"{FormTypeSubject} Request eForm {DbModel.FormInfoId} has been submitted for your review";
        emailSubject = IsReminder ? $"Reminder: {emailSubject}" : emailSubject;
        if (RequestingUser.EmployeeManagementTier is > 3)
        {
            var body = string.Format(RosterChangeEmailTemplate.SUBMITTED_TO_LINEMANAGER_TEMPLATE, DbModel.FormOwnerName,
                        FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
            var ownerMail = new MailMessage(FromEmail, RequestingUser.ExecutiveDirectorIdentifier,
                emailSubject, body);
            completedMail = new List<MailMessage> { ownerMail };
        }
        return completedMail;
    }

    protected virtual async Task<List<MailMessage>> GetEndorsedMail()
    {
        var specification = new FormPermissionSpecification(formId: DbModel.FormInfoId, addPositionInfo: true, addUserInfo: true);
        var permissions = await frmPermissionRepo.ListAsync(specification);
        var owner = permissions.Single(x => x.IsOwner);
        var currentApprover = permissions.Single(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);
        var body = string.Format(RosterChangeEmailTemplate.ENDORSED_TEMPLATE, currentApprover.Name, owner.Name, FormTypeBody,
            SummaryHref, SuffixText);
        var mail = new MailMessage(FromEmail, currentApprover.Email,
            $"{FormTypeSubject} - Roster Change declaration - for approval", body);

        return new List<MailMessage> { mail };
    }

    protected async Task<List<MailMessage>> GetCancelledEmail()
    {
        var permissionSpec = new FormPermissionSpecification(formId: DbModel.FormInfoId, addPositionInfo: true, addUserInfo: true);
        var permissions = await frmPermissionRepo.ListAsync(permissionSpec);
        var ownerPermission = permissions.Single(x => x.IsOwner);
        var owningUser = await EmployeeService.GetEmployeeByAzureIdAsync(ownerPermission.UserId!.Value);
        var body = string.Format(RosterChangeEmailTemplate.CANCELLED_TEMPLATE, owningUser.EmployeePreferredFullName, FormTypeBody, EditHref, SuffixText);
        var mail = new MailMessage(FromEmail, owningUser.EmployeeEmail,
            $"{FormTypeSubject} - Roster Change Request has cancelled", body);
        var cancelledMail = new List<MailMessage> { mail };
        cancelledMail.AddRange(
            from manager in owningUser.Managers.Where(x => permissions.Any(p => p.PositionId == x.EmployeePositionId))
            let managerBody =
                string.Format(RosterChangeEmailTemplate.CANCELLED_TO_MANAGER_TEMPLATE, manager.EmployeePreferredFullName, owningUser.EmployeePreferredFullName,
                    FormTypeBody, SummaryHref, SuffixText)
            select new MailMessage(FromEmail, manager.EmployeeEmail,
                $"{FormTypeSubject} - Roster Change Request has cancelled", managerBody));

        return cancelledMail;
    }
    private MailMessage IncludeAdditionalRecipients(MailMessage mailMessage, RosterChangeModel rosterChangeModel)
    {
        var ccEmailList = rosterChangeModel.SupportingInfoGroup.AdditionalNotification.Select(x => x.EmployeeEmail).ToList();

        ccEmailList.ForEach(obj =>
        {
            mailMessage.CC.Add(obj);
        });

        return mailMessage;
    }
    protected override Task<List<MailMessage>> GetMessageInternalAsync()
    {
        throw new System.NotImplementedException();
    }
}
