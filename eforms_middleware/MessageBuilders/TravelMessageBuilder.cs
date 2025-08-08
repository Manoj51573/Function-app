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

namespace eforms_middleware.MessageBuilders
{
    internal class TravelMessageBuilder : MessageBuilder
    {
        private readonly IRepository<FormPermission> _formPermissionRepo;
        private readonly IEscalationService _escalationService;
        private string SummaryUrl = $"{Helper.BaseEformsURL}/travel-proposal/summary/";
        private string EditUrl = $"{Helper.BaseEformsURL}/travel-proposal/";

        public TravelMessageBuilder(IConfiguration configuration
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
                                                , TravelFormResponseModel responseData)
        {
            var subject = $"Reminder: Travel Proposal and Allowance Claim eForm #{DbModel.FormInfoId} requires your approval";
            var body = string.Format(TravelProposalTemplate.RequestManager
                             , owner.Name
                             , DbModel.FormInfoId
                             , responseData.DestinationType
                             , responseData.TravelStartDate?.ToString("dd/MM/yyyy")
                             , responseData.TravelEndDate?.ToString("dd/MM/yyyy")
                             , responseData.TotalEstimatedTravelCosts
                             , $"{SummaryUrl}{DbModel.FormInfoId}");

            return await SetMessageQ(currentApprovers, subject, body);
        }

        private List<MailMessage> GetReconsileRemider(FormPermission owner)
        {
            var subject = $"Travel Proposal and Allowance eForm #{DbModel.FormInfoId} is ready to be completed";
            var body = string.Format(TravelProposalTemplate.Reconsile
                 , owner.Name
                 , DbModel.FormInfoId
                 , $"{EditUrl}{DbModel.FormInfoId}");

            return new List<MailMessage>
                {
                    new MailMessage(Helper.FromEmail, owner.Email, subject, body)
                };
        }

        

        private async Task<List<MailMessage>> GetDelegatedMail(FormPermission owner
                                        , List<FormPermission> currentApprovers)
        {
            var subject = $"Reminder: Travel Proposal and Allowance eForm #{DbModel.FormInfoId} has been delegated for your approval";

            var body = string.Format(TravelProposalTemplate.Delegated
                            , owner.Name
                            , DbModel.FormInfoId
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
            var subject = $"Reminder: Travel Proposal and Allowance eForm #{DbModel.FormInfoId} has escalated for your approval";
            var body = string.Format(TravelProposalTemplate.Escalated
                  , owner.Name
                            , DbModel.FormInfoId
                            , $"{SummaryUrl}{DbModel.FormInfoId}");

            return await SetMessageQ(currentApprovers, subject, body, false);
        }

        private async Task<List<MailMessage>> GetPODEscalateRemider(FormPermission owner
                                            , List<FormPermission> currentApprovers)
        {
            var subject = $"Travel Proposal and Allowance eForm #{DbModel.FormInfoId} is ready to be completed";
            var body = string.Format(TravelProposalTemplate.PODEscalate
                 , owner.Name
                 , DbModel.FormInfoId
                 , $"{EditUrl}{DbModel.FormInfoId}");

            return await SetMessageQ(currentApprovers, subject, body, false);
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
                var responseData = JsonConvert.DeserializeObject<TravelFormResponseModel>(DbModel.Response);

                messages = DbModel.FormStatusId switch
                {
                    (int)FormStatus.Submitted or (int)FormStatus.Approved when DbModel.FormSubStatus == FormStatus.Escalated.ToString() => await GetEscalatedMail(owner, currentApprovers),
                    (int)FormStatus.Submitted or (int)FormStatus.Approved =>
                        await GetSubmittedEmailAsync(owner, currentApprovers, responseData),
                    (int)FormStatus.Delegated => await GetDelegatedMail(owner, currentApprovers),
                    (int)FormStatus.Unsubmitted when responseData.ReadyForReconsiliation => GetReconsileRemider(owner),
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