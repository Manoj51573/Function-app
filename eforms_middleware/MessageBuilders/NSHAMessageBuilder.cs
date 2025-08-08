using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.Constants.NSHA;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Specifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace eforms_middleware.MessageBuilders;

public abstract class NSHAMessageBuilder : MessageBuilder
{
    protected readonly IRepository<FormPermission> FormPerissionRepo;

    private readonly IFormHistoryService _historyService;

    //protected IEmployeeService EmployeeService { get; }
    protected IRepository<TaskInfo> TaskInfoRepo { get; }
    protected abstract string FormTypeSubject { get; }
    protected abstract string FormTypeBody { get; }

    protected virtual string SuffixText => "Employee Services " +
                                           "on (08) 6551 6888 / " + "or by email at " +
                                           "<a href=\"mailto:fleetmanagement@transport.wa.gov.au\">fleetmanagement@transport.wa.gov.au</a>";

    public string ESOGroupName = "!DOT Employee Services Officer Group";

    protected NSHAMessageBuilder(
        IConfiguration configuration,
        IRequestingUserProvider requestingUserProvider, IPermissionManager permissionManager,
        IEmployeeService employeeService)
        : base(configuration, requestingUserProvider, permissionManager, employeeService)
    {
    }

    protected async Task<List<MailMessage>> GetSubmittedMail()
    {
        var submittedMails = new List<MailMessage> { };
        var nonStandardHardwareAcquisitionRequestModel =
            JsonConvert.DeserializeObject<NonStandardHardwareAcquisitionRequestModel>(DbModel.Response);

        var managerEmailSubject = $"{FormTypeSubject} Request eForm {DbModel.FormInfoId} requires your review";

        var managerMessageBody = string.Format(
            NonStandardHardwareAcquisitionRequestTemplate
                .EXPECTED_TECHNOLOGY_SERVICE_DELIVERY_GROUP_REVIEW_EMAIL_BODY,
            DbModel.FormOwnerName, DbModel.FormInfoId, SummaryHref);

        var managerMessage = new MailMessage(FromEmail,
            NonStandardHardwareAcquisitionRequest.ExpectedTechnologyServiceDeliveryGroupEmail, managerEmailSubject,
            managerMessageBody);


        var requesterEmailSubject = $"{FormTypeSubject} Request eForm {DbModel.FormInfoId} has been received ";

        var requesterMessageBody = string.Format(
            NonStandardHardwareAcquisitionRequestTemplate
                .EXPECTED_REQUESTER_SUBMIT_EMAIL_BODY, DbModel.FormOwnerName, DbModel.FormInfoId);
        var requesterMessage =
            new MailMessage(FromEmail, DbModel.FormOwnerEmail, requesterEmailSubject, requesterMessageBody);

        if ((nonStandardHardwareAcquisitionRequestModel.AdditionalNotificationsRecipients != null &&
             nonStandardHardwareAcquisitionRequestModel.AdditionalNotificationsRecipients.Any())
           )
        {
            foreach (var item in nonStandardHardwareAcquisitionRequestModel.AdditionalNotificationsRecipients)
            {
                requesterMessage.CC.Add(new MailAddress(item.EmployeeEmail));
            }
        }

        if (nonStandardHardwareAcquisitionRequestModel.OnBehalfEmployee != null &&
            nonStandardHardwareAcquisitionRequestModel.OnBehalfEmployee.Any())
        {
            foreach (var item in nonStandardHardwareAcquisitionRequestModel.OnBehalfEmployee)
            {
                requesterMessage.CC.Add(new MailAddress(item.EmployeeEmail));
            }
        }

        submittedMails.Add(managerMessage);
        submittedMails.Add(requesterMessage);


        return submittedMails;
    }

    protected virtual async Task<List<MailMessage>> GetApprovedMail()
    {
        //var formType = (FormType)DbModel.AllFormsId;
        var approvedMail = new List<MailMessage> { };
        var emailSubject = $"{FormTypeSubject} Request eForm {DbModel.FormInfoId} requires your review";
        var nonStandardHardwareAcquisitionRequestModel =
            JsonConvert.DeserializeObject<NonStandardHardwareAcquisitionRequestModel>(DbModel.Response);

        var body = string.Format(
            NonStandardHardwareAcquisitionRequestTemplate
                .EXPECTED_LEASE_ADMIN_GROUP_REVIEW_EMAIL_BODY,
            DbModel.FormOwnerName, DbModel.FormInfoId, SummaryHref);
        var ownerMail = new MailMessage(FromEmail,
            NonStandardHardwareAcquisitionRequest.ExpectedLeaseAdminGroupReviewEmail,
            emailSubject, body);
        approvedMail = new List<MailMessage> { ownerMail };


        return approvedMail;
    }

    protected virtual async Task<List<MailMessage>> GetCompletedEmail(bool ccManager = true)
    {
        var formType = (FormType)DbModel.AllFormsId;
        var completedMail = new List<MailMessage> { };
        var emailSubject = $" {FormTypeSubject} Request  {DbModel.FormInfoId} has been completed";
        var nonStandardHardwareAcquisitionRequestModel =
            JsonConvert.DeserializeObject<NonStandardHardwareAcquisitionRequestModel>(DbModel.Response);
        var employeeDetails = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);
        var body = string.Format(NonStandardHardwareAcquisitionRequestTemplate.EXPECTED_COMPLETED_EMAIL_BODY,
            DbModel.FormInfoId, SummaryHref);
        var ownerMail = new MailMessage(FromEmail, employeeDetails.EmployeeEmail,
            emailSubject, body);
    
          ownerMail.CC.Add(new MailAddress(employeeDetails.EmployeeEmail));

        if (nonStandardHardwareAcquisitionRequestModel.OnBehalfEmployee != null && nonStandardHardwareAcquisitionRequestModel.OnBehalfEmployee.Any())
        {
            foreach (var onBehalfEmployee in nonStandardHardwareAcquisitionRequestModel.OnBehalfEmployee)
            {
                ownerMail.CC.Add(new MailAddress(onBehalfEmployee.EmployeeEmail));
            }
        }

        if (nonStandardHardwareAcquisitionRequestModel.AdditionalNotificationsRecipients != null && nonStandardHardwareAcquisitionRequestModel.AdditionalNotificationsRecipients.Any())
        {
            foreach (var additionalNotificationsRecipient in nonStandardHardwareAcquisitionRequestModel
                         .AdditionalNotificationsRecipients)
            {
                ownerMail.CC.Add(new MailAddress(additionalNotificationsRecipient.EmployeeEmail));
            }
        }

        ownerMail.CC.Add(new MailAddress(employeeDetails.ExecutiveDirectorIdentifier));
        completedMail = new List<MailMessage> { ownerMail };

        return completedMail;
    }


    protected virtual async Task<List<MailMessage>> GetRejectedMail()
    {
        var emailSubject = $"{FormTypeSubject}  Request eForm {DbModel.FormInfoId} has been rejected";
        var rejectedMails = new List<MailMessage> { };
        var nonStandardHardwareAcquisitionRequestModel =
            JsonConvert.DeserializeObject<NonStandardHardwareAcquisitionRequestModel>(DbModel.Response);
        
        var employeeDetails = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwnerPermission.UserId.Value);

        var ownerMessageBody = string.Format(
            NonStandardHardwareAcquisitionRequestTemplate.EXPECTED_REJECTED_EMAIL_BODY,
            RequestingUser.EmployeePreferredFullName, DbModel.FormInfoId, employeeDetails.EmployeePreferredFullName,
            nonStandardHardwareAcquisitionRequestModel.ReasonForDecision,
            EditHref);
        var messageBody = new MailMessage(FromEmail, employeeDetails.EmployeeEmail, emailSubject, ownerMessageBody);
        rejectedMails.Add(messageBody);

        if (nonStandardHardwareAcquisitionRequestModel.OnBehalfEmployee != null && nonStandardHardwareAcquisitionRequestModel.OnBehalfEmployee.Any())
        {
            foreach (var onBehalfEmployee in nonStandardHardwareAcquisitionRequestModel.OnBehalfEmployee)
            {
                messageBody.CC.Add(new MailAddress(onBehalfEmployee.EmployeeEmail));
            }
        }

        if (nonStandardHardwareAcquisitionRequestModel.AdditionalNotificationsRecipients != null && nonStandardHardwareAcquisitionRequestModel.AdditionalNotificationsRecipients.Any())
        {
            foreach (var additionalNotificationsRecipient in nonStandardHardwareAcquisitionRequestModel
                         .AdditionalNotificationsRecipients)
            {
                messageBody.CC.Add(new MailAddress(additionalNotificationsRecipient.EmployeeEmail));
            }
        }

        return rejectedMails;
    }

    protected async Task<List<MailMessage>> GetRecalledMail()
    {
        var emailSubject = $" {FormTypeSubject} Request eForm {DbModel.FormInfoId} has been recalled";
        var recallMessages = new List<MailMessage> { };

        var viewPermissions = Permissions.Where(x =>
            x.PermissionFlag == (byte)PermissionFlag.View && !x.IsOwner && x.PositionId.HasValue);
        var managers = await EmployeeService.GetEmployeeByPositionNumberAsync(viewPermissions.First().PositionId.Value);

        var ownerMessageBody = string.Format(NonStandardHardwareAcquisitionRequestTemplate.EXPECTED_RECALL_OWNER_BODY,
            managers.First().ManagerName,
            RequestingUser.EmployeePreferredFullName,
            FormTypeSubject, DbModel.FormInfoId, SummaryHref);
        var messageBody = new MailMessage(FromEmail, RequestingUser.EmployeeEmail, emailSubject, ownerMessageBody);


        messageBody.CC.Add(new MailAddress(NonStandardHardwareAcquisitionRequest
            .ExpectedTechnologyServiceDeliveryGroupEmail));


        recallMessages.Add(messageBody);

        return recallMessages;
    }

    protected async Task<List<MailMessage>> GetCancelledEmail()
    {
        var permissionSpec =
            new FormPermissionSpecification(formId: DbModel.FormInfoId, addPositionInfo: true, addUserInfo: true);
        var permissions = await FormPerissionRepo.ListAsync(permissionSpec);
        var ownerPermission = permissions.Single(x => x.IsOwner);
        var owningUser = await EmployeeService.GetEmployeeByAzureIdAsync(ownerPermission.UserId!.Value);
        var body = string.Format(LeaveAmendmentTemplates.CANCELLED_TEMPLATE, owningUser.EmployeePreferredFullName,
            FormTypeBody, EditHref, SuffixText);
        var mail = new MailMessage(FromEmail, owningUser.EmployeeEmail,
            $"{FormTypeSubject} - Conflict of Interest declaration has cancelled", body);
        var cancelledMail = new List<MailMessage> { mail };
        cancelledMail.AddRange(
            from manager in owningUser.Managers.Where(x => permissions.Any(p => p.PositionId == x.EmployeePositionId))
            let managerBody =
                string.Format(LeaveAmendmentTemplates.CANCELLED_TO_MANAGER_TEMPLATE, manager.EmployeePreferredFullName,
                    owningUser.EmployeePreferredFullName,
                    FormTypeBody, SummaryHref, SuffixText)
            select new MailMessage(FromEmail, manager.EmployeeEmail,
                $"{FormTypeSubject} - Conflict of Interest declaration has cancelled", managerBody));

        return cancelledMail;
    }
}