using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure;
using Microsoft.Extensions.Configuration;
using eforms_middleware.Constants;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;
using System;
using eforms_middleware.Settings;
using eforms_middleware.Specifications;
//using eforms_middleware.Constants.TPAC;
using eforms_middleware.DataModel;
using Newtonsoft.Json;
using System.Linq;
using eforms_middleware.Interfaces;
using eforms_middleware.Constants.AAC;


namespace eforms_middleware.MessageBuilders
{
    internal class AdditionalHoursClaimsMessageBuilder : MessageBuilder
    {
        private readonly IRepository<FormPermission> _formPermissionRepo;
        private readonly IAllowanceClaimsFormsEscalationServiceManager _escalationService;
        private readonly IEmployeeService _employeeService;
        private string SummaryUrl = $"{Helper.BaseEformsURL}/allowance-and-claims-request/summary/";
        private string EditUrl = $"{Helper.BaseEformsURL}/allowance-and-claims-request/";

        public AdditionalHoursClaimsMessageBuilder(IConfiguration configuration
            , IRepository<FormPermission> formPermissionRepo
            , IAllowanceClaimsFormsEscalationServiceManager escalationService
            , IRequestingUserProvider requestingUserProvider
            , IPermissionManager permissionManager
            , IEmployeeService employeeService) : base(configuration
                                        , requestingUserProvider
                                        , permissionManager
                                        , employeeService)
        {
            _formPermissionRepo = formPermissionRepo;
            _escalationService = escalationService;
            _employeeService = employeeService;
        }


        private async Task<List<MailMessage>> SetMessageQ(List<FormPermission> currentApprovers
            , string subject
            , string body
            , bool allowNotificaiton = true)
        {
            var mailMessage = new List<MailMessage>();
            foreach (var currentApprover in currentApprovers)
            {
                var users = await EmployeeService.GetEmployeeByPositionNumberAsync(currentApprover.PositionId ?? 0);

                if (users == null || users.Count == 0)
                {
                    var messageQ = new MailMessage(Helper.FromEmail, currentApprover.Email, subject, body);                    
                    mailMessage.Add(messageQ);
                }
                else
                {
                    foreach (var user in users)
                    {
                        var messageQ = new MailMessage(Helper.FromEmail, user.EmployeeEmail, subject, body);
                        mailMessage.Add(messageQ);
                    }
                }
                if (allowNotificaiton)
                {
                    await SetTaskNotification(currentApprover);
                }
            }
            return mailMessage;
        }


        private async Task<List<MailMessage>> GetSubmittedEmailAsync(FormPermission owner
                                                , List<FormPermission> currentApprovers
                                                , AdditionalHoursClaimsModel responseData)
        {
            var subject = $"Reminder: Additional Hours Claims eForm #{DbModel.FormInfoId} requires your approval";
            var body = string.Format(AdditionalHoursClaimsTemplate.ReminderToApprover
                             , owner.Name
                             , DbModel.FormInfoId
                             , $"{SummaryUrl}{DbModel.FormInfoId}");

            return await SetMessageQ(currentApprovers, subject, body);
        }

        private async Task<List<MailMessage>> GetDelegatedMail(FormPermission owner
                                       , List<FormPermission> currentApprovers)
        {
            var subject = $"Reminder: Additional Hours Claims eForm #{DbModel.FormInfoId} has been delegated for your approval";

            var body = string.Format(AdditionalHoursClaimsTemplate.Delegated
                            , owner.Name
                            , DbModel.FormInfoId
                            , $"{SummaryUrl}{DbModel.FormInfoId}");
            //fix the below dpf
            return await SetMessageQ(currentApprovers, subject, body);
        }

        private async Task<List<MailMessage>> GetEscalatedMail(FormPermission owner
                                        , List<FormPermission> currentApprovers)
        {
            var subject = $"Reminder: Additional Hours Claims eForm #{DbModel.FormInfoId} has escalated for your review";
            var body = string.Format(AdditionalHoursClaimsTemplate.Escalated
                  , owner.Name
                            , DbModel.FormInfoId
                            , $"{SummaryUrl}{DbModel.FormInfoId}");
            //fix the below dpf


            return await SetMessageQ(currentApprovers, subject, body, false);
        }

        //Task Escalation - Task Notification to PODeFormsBusinessAdmins Group
        private async Task<List<MailMessage>> GetEscalationNotificationToPODeFormsBusinessAdminsGroup(FormPermission owner
                                           , List<FormPermission> currentApprovers)
        {            
            var subject = $"Additional Hours Claims eForm #{DbModel.FormInfoId} has escalated for your action";
            var body = string.Format(AdditionalHoursClaimsTemplate.EscalationNotificationToPODeFormsBusinessAdminsGroup
                 , owner.Name
                 , DbModel.FormInfoId
                 , $"{EditUrl}{DbModel.FormInfoId}");
            
            return await SetMessageQ(currentApprovers, subject, body);
        }


        private async Task SetTaskNotification(FormPermission currentApprover)
        {
            if (currentApprover?.Position?.ManagementTier <= 3)
            {
                await _escalationService.NotifyFormAsync(dbModel: DbModel, escalationDays: 3);
            }
        }

        protected override async Task<List<MailMessage>> GetMessageInternalAsync()
        {

            try
            {
                var messages = new List<MailMessage>();
                var specification = new FormPermissionSpecification(formId: DbModel.FormInfoId, addUserInfo: true, addGroupMemberInfo: true, addPositionInfo: true);
                var permissions = await _formPermissionRepo.ListAsync(specification);
                var owner = permissions.Single(x => x.IsOwner);
                var currentApprovers = permissions.Where(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable).ToList();
                var approvalPermission = permissions.Single(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);
                var isPodGroup = await _employeeService.IsPodUserGroupEmail(approvalPermission.Email);
                var responseData = JsonConvert.DeserializeObject<AdditionalHoursClaimsModel>(DbModel.Response);

                

                messages = DbModel.FormStatusId switch
                {
                    (int)FormStatus.Submitted or (int)FormStatus.Approved 
                        when isPodGroup
                            => await GetEscalationNotificationToPODeFormsBusinessAdminsGroup(owner, currentApprovers),
                    (int)FormStatus.Submitted or (int)FormStatus.Approved
                        when DbModel.FormSubStatus == FormStatus.Escalated.ToString() => await GetEscalatedMail(owner, currentApprovers),

                    (int)FormStatus.Submitted or (int)FormStatus.Approved =>
                        await GetSubmittedEmailAsync(owner, currentApprovers, responseData),
                    (int)FormStatus.Delegated => await GetDelegatedMail(owner, currentApprovers),
                    _ => throw new ArgumentOutOfRangeException()
                };




                return messages;
            }
            catch (Exception e)
            {
                return new List<MailMessage>();
            }
        }
    }
}