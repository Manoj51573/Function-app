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

public class LeaveWithoutPayFormService : BaseApprovalService
{
    private readonly ILogger<LeaveWithoutPayFormService> _logger;
    private readonly IMessageFactoryService _messageFactoryService;
    public IList<IUserInfo> ExecutiveDirectorPeopleAndCulture { get; set; }

    public LeaveWithoutPayFormService(IFormEmailService formEmailService, IRepository<AdfGroup> adfGroup,
        IRepository<AdfPosition> adfPosition, IRepository<AdfUser> adfUser,
        IRepository<AdfGroupMember> adfGroupMember, IRepository<FormInfo> formInfoRepository,
        IRepository<RefFormStatus> formRefStatus, IRepository<FormHistory> formHistoryRepository,
        IPermissionManager permissionManager, IFormInfoService formInfoService, IConfiguration configuration,
        ITaskManager taskManager, IFormHistoryService formHistoryService, IRepository<WorkflowBtn> workflowBtnRepository,
        IRequestingUserProvider requestingUserProvider, IEmployeeService employeeService, ILogger<LeaveWithoutPayFormService> logger,
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

    public async Task<RequestResult> LeaveWithoutPayApprovalProcess(LeaveInfoInsertModel formInfoInsertModel)
    {
        try
        {
        await SetupBaseApprovalService(formInfoInsertModel.FormDetails.FormInfoID);
        var permissions = new List<FormPermission>();
        var leaveWithoutPayModel = JsonConvert.DeserializeObject<LeaveWithoutPayModel>(formInfoInsertModel.FormDetails.Response);
        ExecutiveDirectorPeopleAndCulture = await GetEDPeopleAndCulture();
        var formStatus = formInfoInsertModel.FormAction.GetParseEnum<FormStatus>();
        leaveWithoutPayModel.ReasonForDecision = formInfoInsertModel.RejectionReason;
        var workFlow = new WorkflowModel();
        var request = new FormInfoUpdate
        { FormAction = formInfoInsertModel.FormAction, FormDetails = formInfoInsertModel.ToFormDetailsRequest(), UserId = "" };
        var dbForm = await FormInfoService.GetExistingOrNewFormInfoAsync(request);
        var group = await GetAdfGroupById(LeaveForms.POD_ESO_GROUP_ID);

        dbForm.FormStatusId = (int)formStatus;
        dbForm.FormSubStatus = formStatus.ToString();
        var statusBtnData = new StatusBtnData();
        var invalidRequest = string.Empty;
        switch (formStatus)
        {
            case FormStatus.Unsubmitted:
                dbForm.Response = JsonConvert.SerializeObject(leaveWithoutPayModel);
                dbForm.FormApprovers = "";
                if (leaveWithoutPayModel.IsLeaveRequesterIsManager == "Yes")
                {
                    permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: RequestingUser.EmployeePositionId, isOwner: true));
                }
                else
                {
                    permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, userId: RequestingUser.ActiveDirectoryId, isOwner: true));
                }
                break;
            case FormStatus.Rejected:
                var originalResponse = JsonConvert.DeserializeObject<LeaveWithoutPayModel>(dbForm.Response);
                originalResponse.ReasonForDecision = leaveWithoutPayModel.ReasonForDecision;
                dbForm.Response = JsonConvert.SerializeObject(originalResponse);
                goto case FormStatus.Recall;
            case FormStatus.Recall:
                dbForm.FormApprovers = "";
                dbForm.FormSubStatus = formStatus == FormStatus.Rejected ? "Rejected" : formStatus.ToString();
                dbForm.FormStatusId = (int)FormStatus.Unsubmitted;
                dbForm.NextApprover = null;
                var specification = new FormPermissionSpecification(formId: dbForm.FormInfoId, addUserInfo: true);
                var userPermissions = await _permissionManager.GetPermissionsBySpecificationAsync(specification);
                var approvalPermission = userPermissions.Where(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable).ToList();
                LeaveForms.LAST_APPROVER_EMAILID = approvalPermission.First().Email;
                if (leaveWithoutPayModel.IsLeaveRequesterIsManager == "Yes")
                {
                    permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: OwingPositionUsers.First().EmployeePositionId, isOwner: true));
                }
                else
                {
                    permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, userId: FormOwner.ActiveDirectoryId, isOwner: true));
                }
                break;
            case FormStatus.Submitted:
                leaveWithoutPayModel.ReasonForDecision = string.Empty;
                dbForm.Response = JsonConvert.SerializeObject(leaveWithoutPayModel);
                dbForm.SubmittedDate = DateTime.Now;


                if (RequestingUser.EmployeeManagementTier is 1 || RequestingUser.EmployeeManagementTier is 2)
                {
                    if (leaveWithoutPayModel.IsLeaveRequesterIsManager is "Yes")
                        permissions.Add(new FormPermission((byte)PermissionFlag.View, positionId: RequestingUser.EmployeePositionId, isOwner: true));
                    else
                        permissions.Add(new FormPermission((byte)PermissionFlag.View, userId: RequestingUser.ActiveDirectoryId, isOwner: true));

                    goto case FormStatus.AssignToBackup;
                }
                else if (RequestingUser.Managers.Any() || RequestingUser.ExecutiveDirectors.Any())
                {
                    if (leaveWithoutPayModel.IsLeaveRequesterIsManager is "Yes")
                    {
                        var onBehalfOf = await EmployeeService.GetEmployeeByAzureIdAsync(leaveWithoutPayModel.allEmployees[0].ActiveDirectoryId);
                        permissions.Add(new FormPermission((byte)PermissionFlag.View, userId: leaveWithoutPayModel.allEmployees[0].ActiveDirectoryId));
                        permissions.Add(new FormPermission((byte)PermissionFlag.View, userId: RequestingUser.ActiveDirectoryId));
                        if (onBehalfOf.ManagerPositionId != RequestingUser.EmployeePositionId && onBehalfOf.ExecutiveDirectorPositionId != RequestingUser.EmployeePositionId)
                        {
                            invalidRequest = "You are not allowed to make this request";
                            break;
                        }
                        else
                        {
                            var approver = await EmployeeService.GetEmployeeByEmailAsync(RequestingUser.EmployeeEmail);
                            var approverManager = approver.Managers.Any()
                                                ? approver?.Managers.FirstOrDefault()
                                                : approver?.ExecutiveDirectors.FirstOrDefault();
                            

                            if (approverManager.EmployeeManagementTier == 4)
                            {
                                dbForm.NextApprover = approverManager.ManagerIdentifier;
                                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: approverManager.EmployeePositionId));
                                // permission = new FormPermission((byte)PermissionFlag.UserActionable, positionId: approverManager.EmployeePositionId);
                                permissions.Add(new FormPermission((byte)PermissionFlag.View, positionId: RequestingUser.EmployeePositionId, isOwner: true));
                                dbForm.NextApprovalLevel = approver.EmployeePreferredFullName;
                                statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                                {
                                     SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                                     SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                                     SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                                };  
                            }
                            else if(approverManager.EmployeeManagementTier == 3)
                            {
                                dbForm.NextApprover = approverManager.EmployeeEmail;
                                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: approverManager.EmployeePositionId));
                                // permission = new FormPermission((byte)PermissionFlag.UserActionable, positionId: approverManager.EmployeePositionId);
                                permissions.Add(new FormPermission((byte)PermissionFlag.View, positionId: RequestingUser.EmployeePositionId, isOwner: true));
                                dbForm.NextApprovalLevel = approverManager.EmployeePreferredFullName;
                                statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                                {
                                     SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                                     SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                                     SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                                };
                            }
                        }
                    }
                    else
                    {

                        permissions.Add(new FormPermission((byte)PermissionFlag.View, userId: RequestingUser.ActiveDirectoryId, isOwner: true));

                        if (RequestingUser.Managers.Any())
                        {
                            dbForm.NextApprover = RequestingUser.ManagerIdentifier;
                            permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: RequestingUser.ManagerPositionId));
                            dbForm.NextApprovalLevel = RequestingUser.Managers.FirstOrDefault()!.EmployeePositionTitle;
                        }
                        else if (RequestingUser.ExecutiveDirectors.Any())
                        {
                            permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: RequestingUser.ExecutiveDirectorPositionId));
                            dbForm.NextApprover = RequestingUser.ExecutiveDirectorIdentifier;
                            dbForm.NextApprovalLevel = EmployeeTitle.Tier3.ToString();
                        }
                        statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                        {
                            SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                            SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                            SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                        };
                    }
                }
                else
                {
                    goto case FormStatus.AssignToBackup;

                }

                break;
            case FormStatus.Approved:
                permissions.Add(new FormPermission((byte)PermissionFlag.View, positionId: RequestingUser.EmployeePositionId));
                if (RequestingUser.EmployeeManagementTier > 4)
                {
                    var approver = await EmployeeService.GetEmployeeByEmailAsync(RequestingUser.EmployeeEmail);
                    var approverManager = approver.Managers.Any()
                                        ? approver?.Managers.FirstOrDefault()
                                        : approver?.ExecutiveDirectors.FirstOrDefault();
                   // if (approverManager.EmployeeManagementTier == 4)
                    //{
                        dbForm.NextApprover = approver.ManagerIdentifier;
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: approverManager.EmployeePositionId));
                        // permission = new FormPermission((byte)PermissionFlag.UserActionable, positionId: approverManager.EmployeePositionId);
                        //permissions.Add(new FormPermission((byte)PermissionFlag.View, positionId: RequestingUser.EmployeePositionId, isOwner: true));
                        dbForm.NextApprovalLevel = approver.EmployeePreferredFullName;
                        statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                                {
                                     SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                                     SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                                     SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                                };
                   // }
                }
                else if (RequestingUser.EmployeeManagementTier == 4)
                {
                    permissions.Add(new FormPermission
                    {
                        PermissionFlag = (byte)PermissionFlag.UserActionable,
                        PositionId = RequestingUser.ExecutiveDirectorPositionId
                    });
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
                    if (leaveWithoutPayModel.DifferenceInDays > 90 && RequestingUser.EmployeePositionTitle != "Executive Director People and Culture")
                    {
                        dbForm.NextApprover = ExecutiveDirectorPeopleAndCulture.GetDescription();
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: ExecutiveDirectorPeopleAndCulture.First().EmployeePositionId));
                        dbForm.NextApprovalLevel = ExecutiveDirectorPeopleAndCulture.FirstOrDefault()?.EmployeePositionTitle;
                        statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                        {
                            SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                            SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                            SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                        };
                    }
                    else
                    {
                        dbForm.NextApprover = group.GroupName;
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, groupId: LeaveForms.POD_ESO_GROUP_ID));
                        //permissions.Add(new FormPermission((byte)PermissionFlag.View, positionId: RequestingUser.EmployeePositionId, isOwner: true));
                        dbForm.NextApprovalLevel = group.GroupName;
                        statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                            {
                                SetStatusBtnData(FormStatus.Completed, FormStatus.Complete.ToString(), true),
                                SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                                SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                            };
                    }
                }
                
                break;
            case FormStatus.Delegated:
            case FormStatus.IndependentReview:
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
                    SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                    SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                    SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                };
                break;
            case FormStatus.Completed:
                dbForm.FormApprovers = "";
                dbForm.FormSubStatus = formStatus.ToString();
                dbForm.FormStatusId = (int)FormStatus.Completed;
                dbForm.NextApprover = null;
                break;
            case FormStatus.AssignToBackup:
                dbForm.NextApprover = LeaveForms.POD_EFFORMS_BUSINESS_ADMIN_GROUP_EMAIL; //await GetPodEformsBusinessAdminGroup();
                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, groupId: LeaveForms.POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID));
                dbForm.NextApprovalLevel = LeaveForms.POD_EFFORMS_BUSINESS_ADMIN_GROUP_NAME;
                statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                {
                    SetStatusBtnData(FormStatus.Delegated, FormStatus.Delegate.ToString(), true),
                       SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),

                };
                break;
        }

      

            if (invalidRequest is "")
            {
                var formInfo = await FormInfoService.SaveLeaveFormsInfoAsync(request, dbForm);

                await UpdateTasksAsyncLeaveCashOut(formInfo, FormType.LcR_LWP, formInfoInsertModel.FormDetails.FormStatusID);
                await _permissionManager.UpdateFormPermissionsAsync(formInfo.FormInfoId, permissions);
                await UpdateHistoryAsync(request, formInfo, ActioningGroup?.Id, formInfoInsertModel.AdditionalInfo, leaveWithoutPayModel.ReasonForDecision);
                if (request.FormAction != "Completed")
                {
                    workFlow = new WorkflowModel()
                    {
                        StatusBtnData = statusBtnData,
                    };
                    await SetButton(formInfo, workFlow);
                }
                await _messageFactoryService.SendEmailAsync(formInfo, request);
                
                return RequestResult.SuccessfulFormInfoRequest(formInfo);
            }
            else
            {

                return RequestResult.SuccessRequestWithErrorMessage(new
                {
                    outcome = "Failed",
                    outcomeValues = new
                    {
                        error = "The employee does not directly report to you"
                    }
                });
            }
        }
        catch (Exception e)
        {
            return RequestResult.FailedRequest(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

}