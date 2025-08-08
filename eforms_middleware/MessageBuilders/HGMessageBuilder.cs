using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Azure.Core;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels;
using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure.Interfaces;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Services;
using eforms_middleware.Specifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace eforms_middleware.MessageBuilders;

public abstract class HGMessageBuilder : MessageBuilder
{
    protected readonly IRepository<FormPermission> FormPerissionRepo;
    private readonly IFormHistoryService _historyService;
    private readonly IRepository<AdfUser> _adfUser;

    //protected IEmployeeService EmployeeService { get; }
    protected IRepository<TaskInfo> TaskInfoRepo { get; }
    protected abstract string FormTypeSubject { get; }
    protected abstract string FormTypeBody { get; }
    protected virtual string SuffixText => "Employee Services " +
                                          "on (08) 6551 6888 / " + "or by email at " +
                                          "<a href=\"mailto:fleetmanagement@transport.wa.gov.au\">fleetmanagement@transport.wa.gov.au</a>";

    public string ESOGroupName = "!DOT Employee Services Officer Group";

    protected HGMessageBuilder(
        IConfiguration configuration,
        IRequestingUserProvider requestingUserProvider, IPermissionManager permissionManager, IEmployeeService employeeService, IRepository<AdfUser> adfUser)
        : base(configuration, requestingUserProvider, permissionManager, employeeService)
    {
        this._adfUser = adfUser;
    }

    protected async Task<List<MailMessage>> GetSubmittedMail(bool ccManager = true)
    {
        var submittedMails = new List<MailMessage> { };
        var homeGaragingModel = JsonConvert.DeserializeObject<HomeGaragingModel>(DbModel.Response);

        var emailSubject = $"{FormTypeSubject} Request eForm {DbModel.FormInfoId} requires your review";
        var approvers = await EmployeeService.GetEmployeeByPositionNumberAsync(CurrentApprovers.Single().PositionId.Value);
        foreach (var manager in approvers)
        {
            var managerMessageBody = string.Format(HomeGaragingTemplates.SUBMITTED_TO_LINEMANAGER_TEMPLATE,
                RequestingUser.EmployeePreferredFullName, FormTypeSubject,
                DbModel.FormInfoId, SummaryHrefHg, SuffixText);
            var managerMessage = new MailMessage(FromEmail, manager.EmployeeEmail, emailSubject, managerMessageBody);

            if (homeGaragingModel.AdditionalRecipient != null)
            {
                foreach (var item in homeGaragingModel.AdditionalRecipient)
                {
                    managerMessage.CC.Add(new MailAddress(item.EmployeeEmail));
                }
            }
            submittedMails.Add(managerMessage);
        }
        return submittedMails;
    }

    protected async Task<List<MailMessage>> GetApprovedMail()
    {
        //var formType = (FormType)DbModel.AllFormsId;
        var approvedMail = new List<MailMessage> { };
        var emailSubject = $"{FormTypeSubject} Request eForm {DbModel.FormInfoId} requires your review";
        var homeGaragingModel = JsonConvert.DeserializeObject<HomeGaragingModel>(DbModel.Response);
        if (RequestingUser.EmployeeManagementTier is > 3 && RequestingUser.EmployeePositionId != HomeGaraging.DIRECTOR_PFM_POSITION_NUMBER)
        {
            var body = string.Format(HomeGaragingTemplates.APPROVED_TEMPLATE_ED, DbModel.FormOwnerName,
                FormTypeSubject, DbModel.FormInfoId, SummaryHrefHg, SuffixText);

            if (RequestingUser.ExecutiveDirectors.Count > 1)
            {
                foreach (var item in RequestingUser.ExecutiveDirectors)
                {
                    var edMessage = new MailMessage(FromEmail, item.EmployeeEmail, emailSubject, body);
                    approvedMail.Add(edMessage);
                }
            }
            else
            {
                var ownerMail = new MailMessage(FromEmail, RequestingUser.ExecutiveDirectorIdentifier,
                          emailSubject, body);
                approvedMail = new List<MailMessage> { ownerMail };
            }
        }
        else if (RequestingUser.EmployeeManagementTier is 3)
        {
            var body = string.Format(HomeGaragingTemplates.APPROVED_TEMPLATE_PFM, DbModel.FormOwnerName,
                FormTypeSubject, DbModel.FormInfoId, SummaryHrefHg, SuffixText);

            var directorPefInfo = await EmployeeService.GetEmployeeByPositionNumberAsync(HomeGaraging.DIRECTOR_PFM_POSITION_NUMBER);
            var ownerMail = new MailMessage(FromEmail, directorPefInfo.First().EmployeeEmail,
                emailSubject, body);
            approvedMail = new List<MailMessage> { ownerMail };
        }
        else if (RequestingUser.EmployeePositionId == HomeGaraging.DIRECTOR_PFM_POSITION_NUMBER)
        {
            var body = string.Format(HomeGaragingTemplates.APPROVED_TEMPLATE_FLEET_GROUP, DbModel.FormOwnerName,
                FormTypeSubject, DbModel.FormInfoId, SummaryHrefHg, SuffixText);
            var ownerMail = new MailMessage(FromEmail, HomeGaraging.LEASE_AND_FLEET_FORM_ADMIN_GROUP,
                emailSubject, body);
            approvedMail = new List<MailMessage> { ownerMail };
        }
        return approvedMail;
    }
    protected virtual async Task<List<MailMessage>> GetCompletedEmail(bool ccManager = true)
    {
        var formType = (FormType)DbModel.AllFormsId;
        var completedMail = new List<MailMessage> { };
        var emailSubject = $" {FormTypeSubject} Request  {DbModel.FormInfoId} has been completed";
        var homeGaragingModel = JsonConvert.DeserializeObject<HomeGaragingModel>(DbModel.Response);
        var employeeDetails = await GetAdfUserByEmail(FormOwnerPermission.Email);

        var body = string.Format(HomeGaragingTemplates.COMPLETED_TEMPLATE_TO_MANAGER, DbModel.FormOwnerName, FormTypeSubject, DbModel.FormInfoId, SummaryHrefHg, SuffixText);
        var ownerMail = new MailMessage(FromEmail, employeeDetails.EmployeeEmail,
            emailSubject, body);
        if (employeeDetails.EmployeePositionId is not null)
        {
            var recipients = employeeDetails.Managers;
            foreach (var userManagers in recipients)
            {
                ownerMail.CC.Add(new MailAddress(userManagers.EmployeeEmail));
            }
        }
        else
        {
            var formOwnerContractorManagers = await getUserManager(FormOwnerPermission.Email);
            var requestingUserEd = await EmployeeService.GetEmployeeByEmailAsync(formOwnerContractorManagers.First().EmployeeEmail);
            ownerMail.CC.Add(new MailAddress(formOwnerContractorManagers.First().EmployeeEmail));
        }


        completedMail = new List<MailMessage> { ownerMail };

        return completedMail;
    }
    protected async Task<List<MailMessage>> GetDelegatedMail(bool isReminder = false)
    {
        var formType = (FormType)DbModel.AllFormsId;
        var submittedMails = new List<MailMessage> { };
        var employeeDetails = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);
        var approvingUser = Permissions.Single(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);
        var emailSubject = isReminder ? $"Reminder: {FormTypeSubject} Request eForm {DbModel.FormInfoId} has been delegated for your review" : $"{FormTypeSubject} Request eForm {DbModel.FormInfoId} has been delegated for your review ";
        var body = string.Format(HomeGaragingTemplates.DELEGATE_TEMPLATE, DbModel.FormOwnerName, FormTypeSubject, DbModel.FormInfoId, SummaryHrefHg, SuffixText);//string.Format(LeaveAmendmentTemplates.SUBMITTED_TO_EMPLOYEE_TEMPLATE, owner.User.EmployeeFullName, FormTypeBody, SummaryHrefHg, SuffixText);
        var ownerMessage = new MailMessage(FromEmail, DbModel.NextApprover,
        emailSubject, body);
        submittedMails = new List<MailMessage> { ownerMessage };
        return submittedMails;
    }
    protected async Task<List<MailMessage>> GetRecalledMail()
    {
        var emailSubject = $" {FormTypeSubject} Request eForm {DbModel.FormInfoId} has been recalled";
        var recallMessages = new List<MailMessage> { };
        var homeGaragingModel = JsonConvert.DeserializeObject<HomeGaragingModel>(DbModel.Response);

        var viewPermissions = Permissions.Where(x => x.PermissionFlag == (byte)PermissionFlag.View && !x.IsOwner && x.PositionId.HasValue);
        var managers = await EmployeeService.GetEmployeeByPositionNumberAsync(viewPermissions.First().PositionId.Value);

        var ownerMessageBody = string.Format(HomeGaragingTemplates.RECALL_OWNER_TEMPLATE, managers.First().ManagerName,
            RequestingUser.EmployeePreferredFullName,
            FormTypeSubject, DbModel.FormInfoId, SummaryHrefHg, SuffixText);
        var messageBody = new MailMessage(FromEmail, RequestingUser.EmployeeEmail, emailSubject, ownerMessageBody);


        foreach (var manager in managers)
        {
            messageBody.CC.Add(new MailAddress(manager.EmployeeEmail));
        }
        recallMessages.Add(messageBody);

        return recallMessages;
    }
    protected virtual async Task<List<MailMessage>> GetRejectedMail()
    {
        var emailSubject = $"{FormTypeSubject}  Request eForm {DbModel.FormInfoId} has been rejected";
        var rejectedMails = new List<MailMessage> { };
        var homeGaragingModel = JsonConvert.DeserializeObject<HomeGaragingModel>(DbModel.Response);

        var viewPermissions = Permissions.Where(x => x.PermissionFlag == (byte)PermissionFlag.View && !x.IsOwner && x.PositionId.HasValue);
        var employeeDetails = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);

        var ownerMessageBody = string.Format(HomeGaragingTemplates.REJECTED_TEMPLATE, employeeDetails.EmployeePreferredFullName,
            RequestingUser.EmployeePreferredFullName, FormTypeSubject, DbModel.FormInfoId, homeGaragingModel.ReasonForDecision, SummaryHrefHg, SuffixText);
        var messageBody = new MailMessage(FromEmail, employeeDetails.EmployeeEmail, emailSubject, ownerMessageBody);
        rejectedMails.Add(messageBody);

        return rejectedMails;
    }



    protected async Task<List<MailMessage>> GetReminderMail()
    {
        var ownerUser = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);
        var currentApprover = Permissions.Single(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);
        var approvers = currentApprover.Position.AdfUserPositions.ToList();
        var emailSubject = $"Reminder: {FormTypeSubject} Request eForm {DbModel.FormInfoId} has been submitted for your review ";
        var reminderMails = (from approver in approvers
                             let approverMessageBody =
                                 string.Format(HomeGaragingTemplates.SUBMITTED_TO_LINEMANAGER_TEMPLATE,
                ownerUser.EmployeePreferredFullName, FormTypeSubject,
                DbModel.FormInfoId, SummaryHrefHg, SuffixText)

                             select new MailMessage(FromEmail, approver.EmployeeEmail, emailSubject,
                                 approverMessageBody)).ToList();
        return reminderMails;
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
                var managerMessageBody = string.Format(HomeGaragingTemplates.ESCALATION_MANAGER_TEMPLATE, manager.EmployeePreferredFullName, employeeDetails.EmployeePreferredFullName, FormTypeSubject, DbModel.FormInfoId, SummaryHrefHg, SuffixText);
                var managerMessage = new MailMessage(FromEmail, manager.EmployeeEmail, emailSubject, managerMessageBody);
                if (HomeGaraging.lastApproverEmailid != "")
                {
                    managerMessage.CC.Add(new MailAddress(HomeGaraging.lastApproverEmailid));
                }
                escalationMail.Add(managerMessage);
            }
        }
        else
        {
            var groupemployeeDetails = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);
            var managerMessageBody = string.Format(HomeGaragingTemplates.ESCALATION_FLEETGROUP_TEMPLATE, groupemployeeDetails.EmployeePreferredFullName, FormTypeSubject, DbModel.FormInfoId, SummaryHrefHg, SuffixText);
            var managerMessage = new MailMessage(FromEmail, HomeGaraging.LEASE_AND_FLEET_FORM_ADMIN_GROUP, emailSubject, managerMessageBody);
            if (HomeGaraging.lastApproverEmailid != "")
            {
                managerMessage.CC.Add(new MailAddress(HomeGaraging.lastApproverEmailid));
            }
            escalationMail.Add(managerMessage);
        }
        return escalationMail;
    }

    protected async Task<List<MailMessage>> GetCancelledEmail()
    {
        var permissionSpec = new FormPermissionSpecification(formId: DbModel.FormInfoId, addPositionInfo: true, addUserInfo: true);
        var permissions = await FormPerissionRepo.ListAsync(permissionSpec);
        var ownerPermission = permissions.Single(x => x.IsOwner);
        var owningUser = await EmployeeService.GetEmployeeByAzureIdAsync(ownerPermission.UserId!.Value);
        var body = string.Format(LeaveAmendmentTemplates.CANCELLED_TEMPLATE, owningUser.EmployeePreferredFullName, FormTypeBody, EditHrefHg, SuffixText);
        var mail = new MailMessage(FromEmail, owningUser.EmployeeEmail,
            $"{FormTypeSubject} - Conflict of Interest declaration has cancelled", body);
        var cancelledMail = new List<MailMessage> { mail };
        cancelledMail.AddRange(
            from manager in owningUser.Managers.Where(x => permissions.Any(p => p.PositionId == x.EmployeePositionId))
            let managerBody =
                string.Format(LeaveAmendmentTemplates.CANCELLED_TO_MANAGER_TEMPLATE, manager.EmployeePreferredFullName, owningUser.EmployeePreferredFullName,
                    FormTypeBody, SummaryHrefHg, SuffixText)
            select new MailMessage(FromEmail, manager.EmployeeEmail,
                $"{FormTypeSubject} - Conflict of Interest declaration has cancelled", managerBody));

        return cancelledMail;
    }



    protected override Task<List<MailMessage>> GetMessageInternalAsync()
    {
        throw new System.NotImplementedException();
    }

    public async Task<IUserInfo> GetAdfUserByEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return null;

        try
        {
            return await EmployeeService.GetEmployeeByEmailAsync(email);
        }
        catch
        {
            return null;
        }
    }

    public async Task<AdfUser> GetUserByEmail(string email)
    {
        return await _adfUser.FirstOrDefaultAsync(x => x.EmployeeEmail == email);
    }

    public async Task<IList<IUserInfo>> GetAdfUserByPositionId(int id)
    {
        return await EmployeeService.GetEmployeeByPositionNumberAsync(id);

    }
    public async Task<IList<IUserInfo>> getUserManager(string userEmail)
    {
        var user = await GetAdfUserByEmail(userEmail);
        bool IsUserContractor = false;

        if (string.IsNullOrEmpty(user.EmployeePositionNumber))
            IsUserContractor = true;

        dynamic reqManagers;

        reqManagers = user.Managers;
        if (IsUserContractor)
        {
            var adfUser = await GetUserByEmail(user.EmployeeEmail);
            reqManagers = await GetAdfUserByPositionId((int)adfUser.ReportsToPositionId);
        }

        if (user.EmployeeManagementTier == 4)
        {
            reqManagers = user.ExecutiveDirectors;
        }

        return reqManagers;

    }
}