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
using eforms_middleware.Constants.TPAC;
using eforms_middleware.DataModel;
using Newtonsoft.Json;
using System.Linq;
using eforms_middleware.Interfaces;
using eforms_middleware.Constants.OPR;

namespace eforms_middleware.MessageBuilders
{
    internal class WHSIRMessageBuilder : MessageBuilder
    {
        private readonly IRepository<FormPermission> _formPermissionRepo;
        private readonly IEscalationService _escalationService;
        private string SummaryUrl = $"{Helper.BaseEformsURL}/whs-incident-report/summary/";
        private string EditUrl = $"{Helper.BaseEformsURL}/whs-incident-report/";

        public WHSIRMessageBuilder(IConfiguration configuration
            , IRepository<FormPermission> formPermissionRepo
            , IEscalationService escalationService
            , IRequestingUserProvider requestingUserProvider
            , IPermissionManager permissionManager
            , IEmployeeService employeeService) : base(configuration
                                        , requestingUserProvider
                                        , permissionManager
                                        , employeeService)
        {
            _formPermissionRepo = formPermissionRepo;
            _escalationService = escalationService;
        }

        private async Task<List<MailMessage>> GetSubmittedEmailAsync(FormPermission owner
                                                , List<FormPermission> currentApprovers
                                                , dynamic responseData)
        {
            var subject = $"Reminder: Work Health and Safety Incident Report #{DbModel.FormInfoId} has been submitted for your review";
            var body = string.Format(WHSIREmailTemplate.RequestorManagerReminder
                             , owner.Name
                             , $"{SummaryUrl}{DbModel.FormInfoId}"
                             );

            return await SetMessageQ(currentApprovers, subject, body);
        }


        private async Task<List<MailMessage>> GetDelegatedMail(FormPermission owner
                                        , List<FormPermission> currentApprovers)
        {
            var subject = $"Reminder: Work Health and Safety Incident Report #{DbModel.FormInfoId} has been delegated for your review";

            var body = string.Format(WHSIREmailTemplate.DelegatedManagerReminder
                            , owner.Name
                            , $"{SummaryUrl}{DbModel.FormInfoId}");

            return await SetMessageQ(currentApprovers, subject, body);
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
                foreach (var user in users)
                {
                    var messageQ = new MailMessage(Helper.FromEmail, user.EmployeeEmail, subject, body);
                    mailMessage.Add(messageQ);
                }
                if (allowNotificaiton)
                {
                    await SetTaskNotification(currentApprover);
                }
            }
            return mailMessage;
        }

        private async Task<List<MailMessage>> GetEscalatedMail(FormPermission owner
                                        , List<FormPermission> currentApprovers)
        {
            var subject = $"Work Health and Safety Incident Report #{DbModel.FormInfoId} has escalated for your review";
            var body = string.Format(WHSIREmailTemplate.Escalated
                  , owner.Name
                  , $"{SummaryUrl}{DbModel.FormInfoId}");

            return await SetMessageQ(currentApprovers, subject, body, false);
        }
        /*
        private async Task<List<MailMessage>> GetPODEscalateRemider(FormPermission owner
                                            , List<FormPermission> currentApprovers)
        {
            var subject = $"Travel Proposal and Allowance eForm #{DbModel.FormInfoId} is ready to be completed";
            var body = string.Format(WHSIREmailTemplate.PODEscalate
                 , owner.Name
                 , DbModel.FormInfoId
                 , $"{EditUrl}{DbModel.FormInfoId}");

            return await SetMessageQ(currentApprovers, subject, body, false);
        }*/

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
                dynamic responseData = JsonConvert.DeserializeObject(DbModel.Response);

                messages = DbModel.FormStatusId switch
                {
                    (int)FormStatus.Submitted or (int)FormStatus.Approved when DbModel.FormSubStatus == FormStatus.Escalated.ToString() => await GetEscalatedMail(owner, currentApprovers),
                    (int)FormStatus.Submitted or (int)FormStatus.Approved =>
                        await GetSubmittedEmailAsync(owner, currentApprovers, responseData),
                    (int)FormStatus.Delegated when DbModel.FormSubStatus == FormStatus.Escalated.ToString() => await GetEscalatedMail(owner, currentApprovers),
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