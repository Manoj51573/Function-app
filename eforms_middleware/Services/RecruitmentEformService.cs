using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Settings;
using eforms_middleware.Workflows;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eforms_middleware.Services
{
    public class RecruitmentEformService : BaseApprovalService, IRecruitmentEformService
    {
        private readonly IAttachmentRecordService _attachmentRecordService;
        private readonly ILogger<RecruitmentEformService> _logger;
        private readonly IMessageFactoryService _messageFactoryService;
        public RecruitmentEformService(
            IAttachmentRecordService attachmentRecordService,
            IFormEmailService formEmailService,
        IRepository<AdfGroup> adfGroup, IRepository<AdfPosition> adfPosition,
        IRepository<AdfUser> adfUser, IRepository<AdfGroupMember> adfGroupMember,
        IRepository<FormInfo> formInfoRepository, IRepository<RefFormStatus> formRefStatus,
        IRepository<FormHistory> formHistoryRepository, IPermissionManager permissionManager,
        IFormInfoService formInfoService,
        IConfiguration configuration, ITaskManager taskManager,
        IFormHistoryService formHistoryService, IRequestingUserProvider requestingUserProvider,
        IEmployeeService employeeService, IRepository<WorkflowBtn> workflowBtnRepository,
        ILogger<RecruitmentEformService> logger, IMessageFactoryService messageFactoryService)
        : base(formEmailService, adfGroup, adfPosition, adfUser, adfGroupMember, formInfoRepository, formRefStatus,
               formHistoryRepository, permissionManager, formInfoService, configuration, taskManager, formHistoryService,
               requestingUserProvider, employeeService, workflowBtnRepository)
            => (_attachmentRecordService, _logger, _messageFactoryService) = (attachmentRecordService, logger, messageFactoryService)
        ;

        public async Task<RequestResult> BusinessCaseNonAdvCreateOrUpdate(BusinessCaseNonAdvHttpRequest httpRequest)
        {
            try
            {
                var permissions = new List<FormPermission>();
                var dataModel = JsonConvert.DeserializeObject<BusCaseNonAdvForm>(httpRequest.FormDetails.Response);
                var formId = httpRequest.FormDetails.FormInfoID;
                var formStatus = httpRequest.FormAction.GetParseEnum<FormStatus>();
                var workflow = new WorkflowModel();
                var statusBtnData = new StatusBtnData();
                var request = new FormInfoUpdate
                {
                    FormAction = httpRequest.FormAction,
                    FormDetails = httpRequest.ToFormDetailRequest(),
                    UserId = string.Empty
                };
                await UpdateAttachment(dataModel.AdditionalInfo?.Attachments, formStatus, formId);

                var dbForm = await FormInfoService.GetExistingOrNewFormInfoAsync(request);
                dbForm.FormStatusId = (int)formStatus;
                dbForm.FormSubStatus = formStatus.ToString();
                await SetupBaseApprovalService(httpRequest.FormDetails.FormInfoID);

                var approvalCondition = new
                {
                    Status = formStatus,
                    RequestingUserEmployeeManagementTier1 = RequestingUser.EmployeeManagementTier is 1,
                    RequestingUserEmployeeManagementTier2 = RequestingUser.EmployeeManagementTier is 2,
                    RequestingUserEmployeeManagementTier1or2 = RequestingUser.EmployeeManagementTier is 1 or 2,
                    RequestingUserHasManager = RequestingUser.Managers.Any(),
                    RequestingUserHasExecutiveDirector = RequestingUser.ExecutiveDirectors.Any(),
                };

                switch (approvalCondition)
                {
                    case { Status: FormStatus.Unsubmitted }:
                        {
                            dbForm.Response = JsonConvert.SerializeObject(dataModel);
                            dbForm.FormApprovers = string.Empty;
                            permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, userId: RequestingUser.ActiveDirectoryId, isOwner: true));
                            break;
                        }
                    case { Status: FormStatus.Rejected }:
                        {
                            var originalResponse = JsonConvert.DeserializeObject<RosterChangeModel>(dbForm.Response);
                            originalResponse.ReasonForDecision = dataModel.ReasonForDecision;
                            dbForm.Response = JsonConvert.SerializeObject(originalResponse);
                            permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable,
                                 userId: FormOwner.ActiveDirectoryId, isOwner: true));
                            dbForm.FormStatusId = (int)FormStatus.Unsubmitted;
                            dbForm.FormSubStatus = formStatus.ToString();
                            break;
                        }
                    case { Status: FormStatus.Recall }:
                        {
                            dbForm.FormSubStatus = FormStatus.Recalled.ToString();
                            dbForm.FormStatusId = (int)FormStatus.Unsubmitted;
                            permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable,
                                 userId: FormOwner.ActiveDirectoryId, isOwner: true));
                            break;
                        }
                }
                // save to database
                var formInfo = await FormInfoService.SaveFormInfoAsync(request, dbForm);
                await UpdateTasksAsync(formInfo, true);
                await _permissionManager.UpdateFormPermissionsAsync(formInfo.FormInfoId, permissions);
                await UpdateHistoryAsync(request, formInfo, ActioningGroup?.Id, httpRequest.AdditionalInfo,
                                    dataModel?.ReasonForDecision ?? string.Empty);

                workflow = new WorkflowModel()
                {
                    StatusBtnData = statusBtnData,
                };

                await SetButton(formInfo, workflow);

                //if (Enum.Parse<FormStatus>(request.FormAction) is not FormStatus.Approved)
                //{
                //    await _messageFactoryService.SendEmailAsync(formInfo, request);
                //}
                return RequestResult.SuccessfulFormInfoRequest(formInfo);
            }
            catch (Exception ex)
            {
                return RequestResult.FailedRequest(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        private async Task BusinessCaseNonAdvApprovalProcess(BusCaseNonAdvForm dataModel, FormStatus formStatus, int? formId)
        {
            await SetupBaseApprovalService(formId.Value);
        }

        private async Task UpdateAttachment(IList<AttachmentResult> Attachments, FormStatus FormStatus, int? FormId)
        {
            var isUnSubmittedOrSubmitted = FormStatus is FormStatus.Unsubmitted or FormStatus.Submitted;
            var attachmentEvent = new
            {
                DeactivateAllAttachment = FormId.HasValue && Attachments is null
                             && isUnSubmittedOrSubmitted,
                ActivateAll = Attachments is not null && Attachments.Any()
                             && isUnSubmittedOrSubmitted
            };

            switch (attachmentEvent)
            {
                case { ActivateAll: true, DeactivateAllAttachment: false }:
                    {
                        await _attachmentRecordService.ActivateAttachmentRecordsAsync(FormId!.Value, Attachments);
                        break;
                    }
                case { DeactivateAllAttachment: true, ActivateAll: false }:
                    {
                        await _attachmentRecordService.DeactivateAllAttachmentsAsync(FormId!.Value);
                        break;
                    }
            }
        }
        private async Task SetButton(FormInfo formInfo, WorkflowModel workFlow)
        {
            if (formInfo.FormStatusId != (int)FormStatus.Unsubmitted)
            {
                await SetNextApproverBtn(formInfo.FormInfoId, workFlow.StatusBtnData);
            }
        }
    }
}
