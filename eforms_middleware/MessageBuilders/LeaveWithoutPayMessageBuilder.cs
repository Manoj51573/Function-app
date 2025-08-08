using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure.Interfaces;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Services;
using eforms_middleware.Specifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace eforms_middleware.MessageBuilders;

public class LeaveWithoutPayMessageBuilder : LCOMessageBuilder
{
    private readonly ILogger<LeaveWithoutPayMessageBuilder> _logger;
    protected override string EditPath => "leave-cash-out";
    protected override string SummaryPath => "leave-cash-out/summary";
    protected override string FormTypeSubject => "Leave Without Pay";
    protected override string FormTypeBody => "";
    public IList<IUserInfo> ExecutiveDirectorPeopleAndCulture { get; set; }
    public LeaveWithoutPayMessageBuilder(IEmployeeService employeeService, IRepository<TaskInfo> taskInfoRepo,
        ILogger<LeaveWithoutPayMessageBuilder> logger, IConfiguration configuration, IRepository<FormPermission> formPermissionRepo,
        IFormHistoryService formHistoryService, IRequestingUserProvider requestingUserProvider, IPermissionManager permissionManager)
        : base(configuration, requestingUserProvider, permissionManager, employeeService)
    {
        _logger = logger;
    }

    protected async Task<List<MailMessage>> GetSubmittedMail()
    {
        var submittedMails = new List<MailMessage> { };
        var leaveModel = JsonConvert.DeserializeObject<LeaveWithoutPayModel>(DbModel.Response);
        if (leaveModel.IsLeaveRequesterIsManager is "Yes")
        {
            var emailSubject = $"{FormTypeSubject} Request eForm {DbModel.FormInfoId} has been submitted on your behalf";
            var body = string.Format(LeaveAmendmentTemplates.SUBMITTED_TO_EMPLOYEE_TEMPLATE, leaveModel.allEmployees[0].EmployeeFullName, DbModel.FormOwnerName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
            var ownerMessage = new MailMessage(FromEmail, leaveModel.allEmployees[0].EmployeeEmail,
            emailSubject, body);
            submittedMails = new List<MailMessage> { ownerMessage };
            //Email to tier 3 
            emailSubject = $"{FormTypeSubject} Request eForm {DbModel.FormInfoId} has been submitted for your review";
            body = string.Format(LeaveAmendmentTemplates.SUBMITTED_TO_LINEMANAGER_TEMPLATE, DbModel.FormOwnerName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
            ownerMessage = new MailMessage(FromEmail, RequestingUser.ManagerIdentifier==null? RequestingUser.ExecutiveDirectorIdentifier: RequestingUser.ManagerIdentifier,
            emailSubject, body);
            submittedMails.Add(ownerMessage);
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

    protected  async Task<List<MailMessage>> GetCompletedEmail(bool ccManager = true)
    {
        var completedMail = new List<MailMessage> { };
        var emailSubject = $" {FormTypeSubject} Request  {DbModel.FormInfoId} has been completed";
        var leaveModel = JsonConvert.DeserializeObject<LeaveWithoutPayModel>(DbModel.Response);
        if (leaveModel.IsLeaveRequesterIsManager == "Yes")
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
        var leaveModel = JsonConvert.DeserializeObject<LeaveWithoutPayModel>(DbModel.Response);

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
        var emailSubject = $"{FormTypeSubject} Request eForm {DbModel.FormInfoId} has been submitted for your review";
        var leaveModel = JsonConvert.DeserializeObject<LeaveWithoutPayModel>(DbModel.Response);
        var employeePreferredFullName = "";
        if (leaveModel.IsLeaveRequesterIsManager!="Yes")
        {
            var owner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId!.Value);
             employeePreferredFullName =  owner.EmployeePreferredFullName;
        }
        else
        {
            var ownerManager = await EmployeeService.GetEmployeeByPositionNumberAsync(FormOwnerPermission.PositionId!.Value);
            employeePreferredFullName = ownerManager.First().EmployeePreferredFullName;
        }

        if (RequestingUser.EmployeeManagementTier is > 3 && RequestingUser.EmployeeManagementTier!=4 && RequestingUser.EmployeeManagementTier!=3)
        {
            var approvers = await EmployeeService.GetEmployeeByPositionNumberAsync(CurrentApprovers.Single().PositionId.Value);
            foreach (var manager in approvers)
            {
                var managerMessageBody = string.Format(LeaveAmendmentTemplates.SUBMITTED_TO_LINEMANAGER_TEMPLATE, employeePreferredFullName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
                var managerMessage = new MailMessage(FromEmail, manager.EmployeeEmail, emailSubject, managerMessageBody);
                approvedMail.Add(managerMessage);
            }
        }
        else if (RequestingUser.EmployeeManagementTier == 4)
        {
            var body = string.Format(LeaveAmendmentTemplates.SUBMITTED_TO_LINEMANAGER_TEMPLATE, employeePreferredFullName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
            var ownerMail = new MailMessage(FromEmail, RequestingUser.ExecutiveDirectorIdentifier,
                emailSubject, body);
            approvedMail = new List<MailMessage> { ownerMail };
        }
        else if (leaveModel.DifferenceInDays>90 && RequestingUser.EmployeePositionTitle != "Executive Director People and Culture")
        {
            ExecutiveDirectorPeopleAndCulture = await GetEDPeopleAndCulture();
            var body = string.Format(LeaveAmendmentTemplates.SUBMITTED_TO_ED_PAndC_GROUP_TEMPLATE, DbModel.FormOwnerName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
            var ownerMail = new MailMessage(FromEmail, ExecutiveDirectorPeopleAndCulture.GetDescription(),
                emailSubject, body);
            approvedMail = new List<MailMessage> { ownerMail };
        }
        return approvedMail;
    }

    public async Task<IList<IUserInfo>> GetEDPeopleAndCulture(bool isLeaveFlag = true)
    {
        try
        {
            var result =
                await EmployeeService.GetEmployeeByPositionNumberAsync(ConflictOfInterest
                    .ED_PEOPLE_AND_CULTURE_POSITION_ID);
            return result;
        }
        catch
        {
            return null;
        }
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
            var managers = await EmployeeService.GetEmployeeByPositionNumberAsync(viewPermissions.First().PositionId.Value);
            var ownerMessageBody = string.Format(LeaveAmendmentTemplates.RECALL_OWNER_TEMPLATE, RequestingUser.EmployeePreferredFullName, FormTypeSubject, DbModel.FormInfoId, SummaryHref, SuffixText);
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