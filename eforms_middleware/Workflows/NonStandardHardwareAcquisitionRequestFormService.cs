using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure.Interfaces;
using eforms_middleware.Constants;
using eforms_middleware.Constants.NSHA;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace eforms_middleware.Workflows;

public class NonStandardHardwareAcquisitionRequestFormService : BaseApprovalService
{
    private readonly ILogger<RecruitmentFormService> _logger;
    private readonly IMessageFactoryService _messageFactoryService;


    public NonStandardHardwareAcquisitionRequestFormService(IFormEmailService formEmailService,
        IRepository<AdfGroup> adfGroup,
        IRepository<AdfPosition> adfPosition, IRepository<AdfUser> adfUser,
        IRepository<AdfGroupMember> adfGroupMember, IRepository<FormInfo> formInfoRepository,
        IRepository<RefFormStatus> formRefStatus, IRepository<FormHistory> formHistoryRepository,
        IPermissionManager permissionManager, IFormInfoService formInfoService, IConfiguration configuration,
        ITaskManager taskManager, IFormHistoryService formHistoryService,
        IRepository<WorkflowBtn> workflowBtnRepository,
        IRequestingUserProvider requestingUserProvider, IEmployeeService employeeService,
        ILogger<RecruitmentFormService> logger, IMessageFactoryService messageFactoryService)
        : base(formEmailService, adfGroup, adfPosition, adfUser,
            adfGroupMember, formInfoRepository, formRefStatus, formHistoryRepository, permissionManager,
            formInfoService,
            configuration, taskManager, formHistoryService, requestingUserProvider, employeeService,
            workflowBtnRepository)
    {
        _logger = logger;
        _messageFactoryService = messageFactoryService;
    }

    private async Task SetButton(FormInfo formInfo,
        WorkflowModel workFlow)
    {
        if (formInfo.FormStatusId != (int)FormStatus.Unsubmitted)
            await SetNextApproverBtn(formInfo.FormInfoId, workFlow.StatusBtnData);
    }

    public async Task<RequestResult> NonStandardHardwareAcquisitionRequestApprovalProcess(
        HomeGaragingInfoInsertModel formInfoInsertModel)
    {
        await SetupBaseApprovalService(formInfoInsertModel.FormDetails.FormInfoID);
        var permissions = new List<FormPermission>();
        var nonStandardHardwareAcquisitionRequestModel =
            JsonConvert.DeserializeObject<NonStandardHardwareAcquisitionRequestModel>(formInfoInsertModel.FormDetails
                .Response);
        var formStatus = formInfoInsertModel.FormAction.GetParseEnum<FormStatus>();
        nonStandardHardwareAcquisitionRequestModel.ReasonForDecision = formInfoInsertModel.RejectionReason;
        var emailNotificationModel = new EmailNotificationModel();
        emailNotificationModel.EmailSendType = new List<EmailSendType>();


        var workFlow = new WorkflowModel();
        var request = new FormInfoUpdate
        {
            FormAction = formInfoInsertModel.FormAction, FormDetails = formInfoInsertModel.ToFormDetailsRequest(),
            UserId = ""
        };
        var dbForm = await FormInfoService.GetExistingOrNewFormInfoAsync(request);
        var group = await GetAdfGroupById(NonStandardHardwareAcquisitionRequest.TechnologyServiceDeliveryGroupId);
        var leaseGroup = await GetAdfGroupById(NonStandardHardwareAcquisitionRequest.LeaseAdminGroupReviewId);

        dbForm.FormStatusId = (int)formStatus;
        dbForm.FormSubStatus = formStatus.ToString();
        var statusBtnData = new StatusBtnData();
        // var invalidRequest = string.Empty;
        switch (formStatus)
        {
            case FormStatus.Unsubmitted:
                dbForm.Response = JsonConvert.SerializeObject(nonStandardHardwareAcquisitionRequestModel);
                dbForm.FormApprovers = "";
                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable,
                    RequestingUser.ActiveDirectoryId, isOwner: true));
                emailNotificationModel.EmailSendType.Add(EmailSendType.Created);
                break;
            case FormStatus.Rejected:
                var originalResponse = JsonConvert.DeserializeObject<NonStandardHardwareAcquisitionRequestModel>(dbForm.Response);
                originalResponse.ReasonForDecision = nonStandardHardwareAcquisitionRequestModel.ReasonForDecision;
                dbForm.Response = JsonConvert.SerializeObject(originalResponse);
                emailNotificationModel.EmailSendType.Add(EmailSendType.Rejected);
                goto case FormStatus.Recall;
            case FormStatus.Recall:
                dbForm.FormApprovers = "";
                dbForm.FormSubStatus = formStatus == FormStatus.Rejected ? "Rejected" : formStatus.ToString();
                dbForm.FormStatusId = (int)FormStatus.Unsubmitted;
                dbForm.NextApprover = null;
                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable,
                    FormOwner.ActiveDirectoryId, isOwner: true));
                emailNotificationModel.EmailSendType.Add(EmailSendType.Recalled);
                break;
            case FormStatus.Submitted:
                nonStandardHardwareAcquisitionRequestModel.ReasonForDecision = string.Empty;
                dbForm.Response = JsonConvert.SerializeObject(nonStandardHardwareAcquisitionRequestModel);
                dbForm.SubmittedDate = DateTime.Now;

                dbForm.NextApprover = group.GroupName;
                permissions.Add(new FormPermission((byte)PermissionFlag.View, RequestingUser.ActiveDirectoryId,
                    isOwner: true));
                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable,
                    groupId: NonStandardHardwareAcquisitionRequest.TechnologyServiceDeliveryGroupId));
                dbForm.NextApprovalLevel = group.GroupName;
                statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                {
                    SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                    SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                    SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(), false,
                        FormStatus.Rejected.ToString())
                };
                emailNotificationModel.EmailSendType.Add(EmailSendType.Submitter);
                break;
            case FormStatus.Approved:
                dbForm.NextApprover = leaseGroup.GroupName;
                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable,
                    groupId: NonStandardHardwareAcquisitionRequest.LeaseAdminGroupReviewId));
                dbForm.NextApprovalLevel = leaseGroup.GroupName;
                statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                {
                    SetStatusBtnData(FormStatus.Completed, FormStatus.Complete.ToString(), true),
                    SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true)
                };
                break;

            case FormStatus.Completed:
                dbForm.FormApprovers = "";
                dbForm.FormSubStatus = formStatus.ToString();
                dbForm.FormStatusId = (int)FormStatus.Completed;
                dbForm.NextApprover = null;
                break;
        }

        try
        {
            var formInfo = await FormInfoService.SaveFormInfoAsync(request, dbForm);
            await UpdateTasksAsync(formInfo, true);
            await _permissionManager.UpdateFormPermissionsAsync(formInfo.FormInfoId, permissions);
            await UpdateHistoryAsync(request, formInfo, ActioningGroup?.Id, formInfoInsertModel.AdditionalInfo,
                nonStandardHardwareAcquisitionRequestModel.ReasonForDecision);
            workFlow = new WorkflowModel()
            {
                StatusBtnData = statusBtnData,
                EmailNotificationModel = emailNotificationModel,
                FormInfoRequest = new FormInfoRequest()
            };
            await SetButton(formInfo, workFlow);
            await _messageFactoryService.SendEmailAsync(formInfo, request);


            return RequestResult.SuccessfulFormInfoRequest(formInfo);
        }
        catch (Exception e)
        {
            return RequestResult.FailedRequest(StatusCodes.Status500InternalServerError, e.Message);
        }
    }
}