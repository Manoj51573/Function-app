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
using Newtonsoft.Json;

namespace eforms_middleware.MessageBuilders;

public abstract class LCOMessageBuilder : MessageBuilder
{
    protected readonly IRepository<FormPermission> FormPerissionRepo;
    private readonly IFormHistoryService _historyService;
    //protected IEmployeeService EmployeeService { get; }
    protected IRepository<TaskInfo> TaskInfoRepo { get; }
    protected abstract string FormTypeSubject { get; }
    protected abstract string FormTypeBody { get; }
    protected virtual string SuffixText => "Employee Services " +
                                          "on (08) 6551 6888 / " +
                                          "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a>";

    public string ESOGroupName = "!DOT Employee Services Officer Group";

    protected LCOMessageBuilder(
        IConfiguration configuration,
        IRequestingUserProvider requestingUserProvider, IPermissionManager permissionManager, IEmployeeService employeeService)
        : base(configuration, requestingUserProvider, permissionManager, employeeService)
    {
    }

    protected async Task<List<MailMessage>> GetSubmittedMail()
    {
            var submittedMails = new List<MailMessage> {  };
            var leaveModel = JsonConvert.DeserializeObject<LeaveAmendmentModel>(DbModel.Response);
            if (leaveModel.isLeaveRequesterIsManager is "Yes"){
                var emailSubject = $"{FormTypeSubject} Request eForm {DbModel.FormInfoId} has been submitted on your behalf";
                var body= string.Format(LeaveAmendmentTemplates.SUBMITTED_TO_EMPLOYEE_TEMPLATE, leaveModel.allEmployees[0].EmployeeFullName, DbModel.FormOwnerName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
                var ownerMessage = new MailMessage(FromEmail, leaveModel.allEmployees[0].EmployeeEmail,
                emailSubject, body);
                submittedMails = new List<MailMessage> { ownerMessage };
            }
            else{
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

    protected async Task<List<MailMessage>> GetRecalledMail()
    {
        var emailSubject = $" {FormTypeSubject} Request eForm {DbModel.FormInfoId} has been recalled";
        var recallMessages = new List<MailMessage> { };
        var leaveModel = JsonConvert.DeserializeObject<LeaveAmendmentModel>(DbModel.Response);
        int count = 0;
        if (leaveModel.isLeaveRequesterIsManager is "Yes") {
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
                recallMessages.Add(ownerMail );
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
    protected virtual async Task<List<MailMessage>> GetRejectedMail()
    {

        var emailSubject = $"{FormTypeSubject}  Request eForm {DbModel.FormInfoId} has been rejected";
        var rejectedMails = new List<MailMessage> { };
        var leaveModel = JsonConvert.DeserializeObject<LeaveAmendmentModel>(DbModel.Response);

        if (leaveModel.isLeaveRequesterIsManager=="Yes")
        {
            var formOwner = await EmployeeService.GetEmployeeByPositionNumberAsync(FormOwnerPermission.PositionId.Value);
            int count = 0;
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
                ownerMail.CC.Add(new MailAddress(leaveModel.allEmployees[0].EmployeeEmail));

                rejectedMails.Add(ownerMail);
            }
        }
        else
        {
            var viewPermissions = Permissions.Where(x => x.PermissionFlag == (byte)PermissionFlag.View && !x.IsOwner && x.PositionId.HasValue);
            var managers = await EmployeeService.GetEmployeeByPositionNumberAsync(viewPermissions.First().PositionId.Value);
            var employeeDetails = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);

            var ownerMessageBody = string.Format(LeaveAmendmentTemplates.REJECTED_TEMPLATE, employeeDetails.EmployeePreferredFullName, RequestingUser.EmployeePreferredFullName, FormTypeSubject, DbModel.FormInfoId, leaveModel.ReasonForDecision, SummaryHref, SuffixText);
            var messageBody = new MailMessage(FromEmail, employeeDetails.EmployeeEmail, emailSubject, ownerMessageBody);
            foreach (var manager in managers)
            {
                messageBody.CC.Add(new MailAddress(manager.EmployeeEmail));
            }
            if (DbModel.NextApprovalLevel != "!DOT Employee Services Officer Group")
            {
                messageBody.CC.Add(new MailAddress(RequestingUser.EmployeeEmail));
            }
            rejectedMails.Add(messageBody);
        }
        return rejectedMails;
    }

    protected virtual async Task<List<MailMessage>> GetCompletedEmail(bool ccManager = true)
    {
        var formType = (FormType)DbModel.AllFormsId;
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


    protected async Task<List<MailMessage>> GetDelegatedMail()
    {
        var formType = (FormType)DbModel.AllFormsId;
        var submittedMails = new List<MailMessage> { };
        var employeeDetails = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);
        var approvingUser = Permissions.Single(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);
        var emailSubject = $"{FormTypeSubject} Request eForm {DbModel.FormInfoId} has been delegated for your review ";
        var body = string.Format(LeaveAmendmentTemplates.DELEGATE_TEMPLATE, DbModel.FormOwnerName, FormTypeSubject,DbModel.FormInfoId, SummaryHref, SuffixText);//string.Format(LeaveAmendmentTemplates.SUBMITTED_TO_EMPLOYEE_TEMPLATE, owner.User.EmployeeFullName, FormTypeBody, SummaryHref, SuffixText);
        var ownerMessage = new MailMessage(FromEmail, DbModel.NextApprover,
        emailSubject, body);
        submittedMails = new List<MailMessage> { ownerMessage };
        return submittedMails;
    }

    protected async Task<List<MailMessage>> GetReminderMail()
    {
        var ownerUser = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);
        var currentApprover = Permissions.Single(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);
        var approvers = currentApprover.Position.AdfUserPositions.ToList();
        var emailSubject = $"Reminder: {FormTypeSubject} Request eForm {DbModel.FormInfoId} has been submitted for your review ";
        var reminderMails = (from approver in approvers
                             let approverMessageBody =
                                 string.Format(LeaveAmendmentTemplates.SUBMITTED_TO_LINE_MANAGER_TEMPLATE, approver.EmployeeFullName, ownerUser.EmployeePreferredFullName, DbModel.FormInfoId,
                                     SummaryHref, SuffixText, FormTypeSubject)
                          
                             select new MailMessage(FromEmail, approver.EmployeeEmail, emailSubject,
                                 approverMessageBody)).ToList();
        return reminderMails;
    }   

    
    protected async Task<List<MailMessage>> GetEscalatedMail()
    {
        var escalationMail = new List<MailMessage> { };
        var emailSubject = $"Escalation: {FormTypeSubject} Request eForm {DbModel.FormInfoId} has been has escalated for your review";
        var count = 0;
        if (CurrentApprovers.Single().PositionId!=null)
        {
            var approvers = await EmployeeService.GetEmployeeByPositionNumberAsync(CurrentApprovers.Single().PositionId.Value);
            var employeeDetails = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);
            
            foreach (var manager in approvers)
            {
                count++;
                var managerMessageBody = string.Format(LeaveAmendmentTemplates.ESCALATION_MANAGER_TEMPLATE, manager.EmployeePreferredFullName, employeeDetails.EmployeePreferredFullName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
                var managerMessage = new MailMessage(FromEmail, manager.EmployeeEmail, emailSubject, managerMessageBody);
                escalationMail.Add(managerMessage);
                if (LeaveForms.LAST_APPROVER_EMAILID != "" && count==1)
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

    protected async Task<List<MailMessage>> GetApprovedMail(bool isReminder = false)
    {
        var formType = (FormType)DbModel.AllFormsId;
        var completedMail = new List<MailMessage> { };
        var emailSubject = $"{FormTypeSubject} Request eForm {DbModel.FormInfoId} has been submitted for your review";
        var leaveModel = JsonConvert.DeserializeObject<LeaveAmendmentModel>(DbModel.Response);
        if (RequestingUser.EmployeeManagementTier is > 3)
        {
            var employeeDetails = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);
            var body = string.Format(LeaveAmendmentTemplates.SUBMITTED_TO_LINEMANAGER_TEMPLATE, DbModel.FormOwnerName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
            var ownerMail = new MailMessage(FromEmail, RequestingUser.ExecutiveDirectorIdentifier,
                emailSubject, body);
            completedMail = new List<MailMessage> { ownerMail };
        }
        return completedMail;
    }

    protected virtual async Task<List<MailMessage>> GetEndorsedMail()
    {
        var specification = new FormPermissionSpecification(formId: DbModel.FormInfoId, addPositionInfo: true, addUserInfo: true);
        var permissions = await FormPerissionRepo.ListAsync(specification);
        var owner = permissions.Single(x => x.IsOwner);
        var currentApprover = permissions.Single(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);
        var body = string.Format(LeaveAmendmentTemplates.ENDORSED_TEMPLATE, currentApprover.Name, owner.Name, FormTypeBody,
            SummaryHref, SuffixText);
        var mail = new MailMessage(FromEmail, currentApprover.Email,
            $"{FormTypeSubject} - Conflict of interest declaration - for approval", body);

        return new List<MailMessage> { mail };
    }

  
    

    protected async Task<List<MailMessage>> GetCancelledEmail()
    {
        var permissionSpec = new FormPermissionSpecification(formId: DbModel.FormInfoId, addPositionInfo: true, addUserInfo: true);
        var permissions = await FormPerissionRepo.ListAsync(permissionSpec);
        var ownerPermission = permissions.Single(x => x.IsOwner);
        var owningUser = await EmployeeService.GetEmployeeByAzureIdAsync(ownerPermission.UserId!.Value);
        var body = string.Format(LeaveAmendmentTemplates.CANCELLED_TEMPLATE, owningUser.EmployeePreferredFullName, FormTypeBody, EditHref, SuffixText);
        var mail = new MailMessage(FromEmail, owningUser.EmployeeEmail,
            $"{FormTypeSubject} - Conflict of Interest declaration has cancelled", body);
        var cancelledMail = new List<MailMessage> { mail };
        cancelledMail.AddRange(
            from manager in owningUser.Managers.Where(x => permissions.Any(p => p.PositionId == x.EmployeePositionId))
            let managerBody =
                string.Format(LeaveAmendmentTemplates.CANCELLED_TO_MANAGER_TEMPLATE, manager.EmployeePreferredFullName, owningUser.EmployeePreferredFullName,
                    FormTypeBody, SummaryHref, SuffixText)
            select new MailMessage(FromEmail, manager.EmployeeEmail,
                $"{FormTypeSubject} - Conflict of Interest declaration has cancelled", managerBody));

        return cancelledMail;
    }



    protected override Task<List<MailMessage>> GetMessageInternalAsync()
    {
        throw new System.NotImplementedException();
    }
}