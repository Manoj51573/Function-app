using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Settings;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using static eforms_middleware.Settings.Helper;
using System;
using System.Collections.Generic;
using DoT.Infrastructure.Interfaces;
using eforms_middleware.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Core;
using DoT.Infrastructure.DbModels;
using eforms_middleware.Services;
using eforms_middleware.Specifications;
using Microsoft.Graph;
using System.IO;

namespace eforms_middleware.Workflows;

public class HomeGaragingFormService : BaseApprovalService
{
    private readonly ILogger<RecruitmentFormService> _logger;
    private readonly IMessageFactoryService _messageFactoryService;
    public IList<IUserInfo> ExecutiveDirectorPeopleAndCulture { get; set; }

    public HomeGaragingFormService(IFormEmailService formEmailService, IRepository<AdfGroup> adfGroup,
        IRepository<AdfPosition> adfPosition, IRepository<AdfUser> adfUser,
        IRepository<AdfGroupMember> adfGroupMember, IRepository<FormInfo> formInfoRepository,
        IRepository<RefFormStatus> formRefStatus, IRepository<FormHistory> formHistoryRepository,
        IPermissionManager permissionManager, IFormInfoService formInfoService, IConfiguration configuration,
        ITaskManager taskManager, IFormHistoryService formHistoryService, IRepository<WorkflowBtn> workflowBtnRepository,
        IRequestingUserProvider requestingUserProvider, IEmployeeService employeeService, ILogger<RecruitmentFormService> logger,
        IMessageFactoryService messageFactoryService)
        : base(formEmailService, adfGroup, adfPosition, adfUser,
            adfGroupMember, formInfoRepository, formRefStatus, formHistoryRepository, permissionManager, formInfoService,
            configuration, taskManager, formHistoryService, requestingUserProvider, employeeService, workflowBtnRepository)
    {
        _logger = logger;
        _messageFactoryService = messageFactoryService;
    }

    private async Task SetButton(FormInfo formInfo,
            WorkflowModel workFlow)
    {
        if (formInfo.FormStatusId != (int)FormStatus.Unsubmitted)
        {
            await SetNextApproverBtn(formInfo.FormInfoId, workFlow.StatusBtnData);
        }
    }

    public async Task<RequestResult> HomeGaragingApprovalProcess(HomeGaragingInfoInsertModel formInfoInsertModel)
    {
        await SetupBaseApprovalService(formInfoInsertModel.FormDetails.FormInfoID);
        var permissions = new List<FormPermission>();
        var homeGaragingModel = JsonConvert.DeserializeObject<HomeGaragingModel>(formInfoInsertModel.FormDetails.Response);
        var formStatus = formInfoInsertModel.FormAction.GetParseEnum<FormStatus>();
        homeGaragingModel.ReasonForDecision = formInfoInsertModel.RejectionReason;
        var workFlow = new WorkflowModel();
        var request = new FormInfoUpdate
        { FormAction = formInfoInsertModel.FormAction, FormDetails = formInfoInsertModel.ToFormDetailsRequest(), UserId = "" };
        var dbForm = await FormInfoService.GetExistingOrNewFormInfoAsync(request);
        var group = await GetAdfGroupById(HomeGaraging.LEASE_AND_FLEET_FORM_ADMIN_GROUP_ID);

        dbForm.FormStatusId = (int)formStatus;
        dbForm.FormSubStatus = formStatus.ToString();
        var statusBtnData = new StatusBtnData();
        switch (formStatus)
        {
            case FormStatus.Unsubmitted:
                dbForm.Response = JsonConvert.SerializeObject(homeGaragingModel);
                dbForm.FormApprovers = "";
                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, userId: RequestingUser.ActiveDirectoryId, isOwner: true));
                break;
            case FormStatus.Rejected:
                var originalResponse = JsonConvert.DeserializeObject<HomeGaragingModel>(dbForm.Response);
                originalResponse.ReasonForDecision = homeGaragingModel.ReasonForDecision;
                dbForm.Response = JsonConvert.SerializeObject(originalResponse);
                goto case FormStatus.Recall;
            case FormStatus.Recall:
                dbForm.FormApprovers = "";
                dbForm.FormSubStatus = formStatus == FormStatus.Rejected ? "Rejected" : formStatus.ToString();
                dbForm.FormStatusId = (int)FormStatus.Unsubmitted;
                dbForm.NextApprover = null;
                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, userId: FormOwner.ActiveDirectoryId, isOwner: true));
                break;
            case FormStatus.Submitted:
                homeGaragingModel.ReasonForDecision = string.Empty;
                dbForm.Response = JsonConvert.SerializeObject(homeGaragingModel);
                dbForm.SubmittedDate = DateTime.Now;
                var approver = await EmployeeService.GetEmployeeByEmailAsync(RequestingUser.EmployeeEmail);

                switch (RequestingUser.EmployeeManagementTier)
                {
                    case 1:
                        permissions.Add(new FormPermission((byte)PermissionFlag.View, userId: RequestingUser.ActiveDirectoryId, isOwner: true));
                        dbForm.NextApprover = "";//need to put the acctual approver
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: HomeGaraging.DIRECTOR_PFM_POSITION_NUMBER));
                        dbForm.NextApprovalLevel = approver.EmployeePreferredFullName;
                        statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                                {
                                     SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                                     SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                                     SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                                };

                        break;
                    case 2:
                        var tierOneApprover = approver.Managers.Any()
                                             ? approver?.Managers.FirstOrDefault()
                                            : approver.ExecutiveDirectors.FirstOrDefault();
                        permissions.Add(new FormPermission((byte)PermissionFlag.View, userId: RequestingUser.ActiveDirectoryId, isOwner: true));
                        dbForm.NextApprover = tierOneApprover.EmployeeEmail;
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: tierOneApprover.EmployeePositionId));
                        dbForm.NextApprovalLevel = tierOneApprover.EmployeePositionTitle;
                        statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                                {
                                     SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                                     SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                                     SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                                };

                        break;
                    case 3:

                        var tierTwoApprover = approver.Managers.Any()
                                            ? approver?.Managers.FirstOrDefault()
                                            : approver.ExecutiveDirectors.FirstOrDefault();
                        permissions.Add(new FormPermission((byte)PermissionFlag.View, userId: RequestingUser.ActiveDirectoryId, isOwner: true));
                        dbForm.NextApprover = tierTwoApprover.EmployeeEmail;
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: tierTwoApprover.EmployeePositionId));
                        dbForm.NextApprovalLevel = tierTwoApprover.EmployeePositionTitle;
                        statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                                {
                                     SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                                     SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                                     SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                                };

                        break;
                    case 4:
                        var tierThreeApprover = approver.Managers.Any()
                                            ? approver?.Managers.FirstOrDefault()
                                            : approver.ExecutiveDirectors.FirstOrDefault();
                        permissions.Add(new FormPermission((byte)PermissionFlag.View, userId: RequestingUser.ActiveDirectoryId, isOwner: true));
                        dbForm.NextApprover = tierThreeApprover.EmployeeEmail;
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: tierThreeApprover.EmployeePositionId));
                        dbForm.NextApprovalLevel = approver.EmployeePositionTitle;
                        statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                                {
                                     SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                                     SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                                     SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                                };

                        break;
                    default:
                        var specification = new FormPermissionSpecification(formId: dbForm.FormInfoId, addUserInfo: true);
                        var userPermissions = await _permissionManager.GetPermissionsBySpecificationAsync(specification);
                        var approvalPermission = userPermissions.Single(x => x.IsOwner);
                        var formOwnerManagers = await getUserManager(approvalPermission.Email);
                        permissions.Add(new FormPermission((byte)PermissionFlag.View, userId: RequestingUser.ActiveDirectoryId, isOwner: true));
                        dbForm.NextApprover = formOwnerManagers.First().EmployeeEmail;  ///await getUserManager(RequestingUser.EmployeeEmail); // userManager.EmployeeEmail;
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: formOwnerManagers.First().EmployeePositionId));
                        dbForm.NextApprovalLevel = formOwnerManagers.First().EmployeePositionTitle;
                        statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                                {
                                     SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                                     SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                                     SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                                };

                        break;
                }
                break;
            case FormStatus.Approved:
                permissions.Add(new FormPermission((byte)PermissionFlag.View, positionId: RequestingUser.EmployeePositionId));
                if (RequestingUser.EmployeePositionId == HomeGaraging.DIRECTOR_PFM_POSITION_NUMBER)
                {
                    dbForm.NextApprover = group.GroupName;
                    permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, groupId: HomeGaraging.LEASE_AND_FLEET_FORM_ADMIN_GROUP_ID));
                    dbForm.NextApprovalLevel = group.GroupName;
                    statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                            {
                                SetStatusBtnData(FormStatus.Completed, FormStatus.Complete.ToString(), true),
                                SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                                SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString()),
                                SetStatusBtnData(FormStatus.Delegated, FormStatus.Delegate.ToString(), true),
                            };

                }
                else if ((RequestingUser.EmployeePositionId == FormOwner.ManagerPositionId || FormOwner.EmployeePositionId is null) &&  RequestingUser.EmployeeManagementTier!=1 
                    && RequestingUser.EmployeeManagementTier!=2 && RequestingUser.EmployeeManagementTier != 3)
                {
                    permissions.Add(new FormPermission((byte)PermissionFlag.View, positionId: RequestingUser.EmployeePositionId));
                    permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: RequestingUser.ExecutiveDirectorPositionId));
                    dbForm.NextApprover = RequestingUser.ExecutiveDirectorIdentifier;
                    dbForm.NextApprovalLevel = EmployeeTitle.Tier3.ToString();
                    statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                    {
                        SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                        SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                        SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                    };
                }
                else
                {
                    var directorPefInfo = await EmployeeService.GetEmployeeByPositionNumberAsync(HomeGaraging.DIRECTOR_PFM_POSITION_NUMBER);
                    dbForm.NextApprover = directorPefInfo.First().EmployeeEmail;//need to put the acctual approver
                    permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: HomeGaraging.DIRECTOR_PFM_POSITION_NUMBER));
                    dbForm.NextApprovalLevel = directorPefInfo.First().EmployeePositionTitle;//need to put the acctual approver level
                    statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                    {
                            SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                            SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                            SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                    };

                }
                break;
            case FormStatus.Delegated:
                var userId = Guid.Parse(formInfoInsertModel.FormDetails.NextApprover);
                var manualNextApprover =
                    await EmployeeService.GetEmployeeByAzureIdAsync(userId);
                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, userId: manualNextApprover.ActiveDirectoryId));
                var nextApprovalLevel = Enum.Parse<EmployeeTitle>(formStatus.ToString());
                dbForm.NextApprover = manualNextApprover.EmployeeEmail;
                dbForm.NextApprovalLevel = formStatus.ToString();
                dbForm.FormSubStatus = nextApprovalLevel.GetEmployeeTitle();
                statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                {
                    SetStatusBtnData(FormStatus.Completed, FormStatus.Complete.ToString(), true),
                    SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
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
            await UpdateHistoryAsync(request, formInfo, ActioningGroup?.Id, formInfoInsertModel.AdditionalInfo, homeGaragingModel.ReasonForDecision);
            workFlow = new WorkflowModel()
            {
                StatusBtnData = statusBtnData,
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