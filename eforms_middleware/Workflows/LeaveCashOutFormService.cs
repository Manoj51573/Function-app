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
using eforms_middleware.Specifications;

namespace eforms_middleware.Workflows
{
    public class LeaveCashOutFormService : BaseApprovalService
    {
        private readonly ILogger<LeaveCashOutFormService> _logger;
        private readonly IMessageFactoryService _messageFactoryService;
        public IList<IUserInfo> ExecutiveDirectorPeopleAndCulture { get; set; }


        public LeaveCashOutFormService(IFormEmailService formEmailService, IRepository<AdfGroup> adfGroup,
            IRepository<AdfPosition> adfPosition, IRepository<AdfUser> adfUser,
            IRepository<AdfGroupMember> adfGroupMember, IRepository<FormInfo> formInfoRepository,
            IRepository<RefFormStatus> formRefStatus, IRepository<FormHistory> formHistoryRepository,
            IPermissionManager permissionManager, IFormInfoService formInfoService, IConfiguration configuration,
            ITaskManager taskManager, IFormHistoryService formHistoryService, IRepository<WorkflowBtn> workflowBtnRepository,
            IRequestingUserProvider requestingUserProvider, IEmployeeService employeeService, ILogger<LeaveCashOutFormService> logger,
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
        public Task<IApprovalInfo> GetFinalApprovers()
        {
            var approvalInfo = new ApprovalInfo
            {
                NextApprover = "",
                GroupId = LeaveForms.POD_ESO_GROUP_ID
            };
            return Task.FromResult((IApprovalInfo)approvalInfo);
        }
        public async Task<RequestResult> LeaveCashOutApprovalProcess(LeaveInfoInsertModel formInfoInsertModel)
        {
            await SetupBaseApprovalService(formInfoInsertModel.FormDetails.FormInfoID);
            var permissions = new List<FormPermission>();
            var leaveCashOutModel = JsonConvert.DeserializeObject<LeaveCashOutModel>(formInfoInsertModel.FormDetails.Response);
            ExecutiveDirectorPeopleAndCulture = await GetEDPeopleAndCulture();
            var formStatus = formInfoInsertModel.FormAction.GetParseEnum<FormStatus>();
            leaveCashOutModel.ReasonForDecision = formInfoInsertModel.RejectionReason;
            var workFlow = new WorkflowModel();
            var request = new FormInfoUpdate
            { FormAction = formInfoInsertModel.FormAction, FormDetails = formInfoInsertModel.ToFormDetailsRequest(), UserId = "" };
            var dbForm = await FormInfoService.GetExistingOrNewFormInfoAsync(request);
            var group = await GetAdfGroupById(LeaveForms.POD_ESO_GROUP_ID);

            dbForm.FormStatusId = (int)formStatus;
            dbForm.FormSubStatus = formStatus.ToString();
            var statusBtnData = new StatusBtnData();
            var invalidRequest = string.Empty;
            var IsRequestorDVS = false;
            switch (formStatus)
            {
                case FormStatus.Unsubmitted:
                    dbForm.Response = JsonConvert.SerializeObject(leaveCashOutModel);
                    dbForm.FormApprovers = "";
                    if (leaveCashOutModel.IsLeaveRequesterIsManager == "Yes")
                    {
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: RequestingUser.EmployeePositionId, isOwner: true));
                    }
                    else
                    {
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, userId: RequestingUser.ActiveDirectoryId, isOwner: true));
                    }
                    break;
                case FormStatus.Rejected:
                    var originalResponse = JsonConvert.DeserializeObject<LeaveCashOutModel>(dbForm.Response);
                    originalResponse.ReasonForDecision = leaveCashOutModel.ReasonForDecision;
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
                    if (leaveCashOutModel.IsLeaveRequesterIsManager == "Yes")
                    {
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: OwingPositionUsers.First().EmployeePositionId, isOwner: true));
                    }
                    else
                    {
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, userId: FormOwner.ActiveDirectoryId, isOwner: true));
                    }
                    break;
                case FormStatus.Submitted:
                    IsRequestorDVS = RequestingUser != null && RequestingUser.Directorate
                     .ToUpper().Equals(LeaveForms.DVS_EFFORMS_DIRECTORATE_NAME.ToUpper());
                    leaveCashOutModel.ReasonForDecision = string.Empty;
                    dbForm.Response = JsonConvert.SerializeObject(leaveCashOutModel);
                    dbForm.SubmittedDate = DateTime.Now;
                    if (RequestingUser.EmployeeManagementTier is 1 || RequestingUser.EmployeeManagementTier is 2)
                    {
                        goto case FormStatus.AssignToBackup;
                    }
                    else if (RequestingUser.Managers.Any() || RequestingUser.ExecutiveDirectors.Any())
                    {
                        if (leaveCashOutModel.IsLeaveRequesterIsManager == "Yes")
                        {
                            permissions.Add(new FormPermission((byte)PermissionFlag.View, positionId: RequestingUser.EmployeePositionId, isOwner: true));
                            var onBehalfOf = await EmployeeService.GetEmployeeByAzureIdAsync(leaveCashOutModel.allEmployees[0].ActiveDirectoryId);
                            permissions.Add(new FormPermission((byte)PermissionFlag.View, userId: leaveCashOutModel.allEmployees[0].ActiveDirectoryId));

                            if (onBehalfOf.ManagerPositionId != RequestingUser.EmployeePositionId && onBehalfOf.ExecutiveDirectorPositionId != RequestingUser.EmployeePositionId)
                            {
                                invalidRequest = "You are not allowed to make this request";
                                break;
                            }

                            if (IsRequestorDVS)
                            {
                                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, groupId: LeaveForms.DOT_DVS_FIXED_TERM_APPOINTMENT_QA_GROUP_ID));
                                dbForm.NextApprovalLevel = LeaveForms.DOT_DVS_FIXED_TERM_APPOINTMENT_QA_GROUP_NAME;
                                dbForm.NextApprover = LeaveForms.DOT_DVS_FIXED_TERM_APPOINTMENT_QA_GROUP_NAME;
                                statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                                {
                                    SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                                    SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                                    SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                                };
                            }
                            else
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
                        }
                        else  //ToDo check form types on set up
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
                    var formOwner = await EmployeeService.GetEmployeeByEmailAsync(Permissions.Single(x => x.IsOwner).Email);
                    permissions.Add(new FormPermission((byte)PermissionFlag.View, positionId: RequestingUser.EmployeePositionId));
                    if (formOwner != null)
                    {
                        IsRequestorDVS = formOwner.Directorate
                              .ToUpper().Equals(LeaveForms.DVS_EFFORMS_DIRECTORATE_NAME.ToUpper());
                    }
                    if (IsRequestorDVS && dbForm.NextApprover != LeaveForms.DOT_DVS_FIXED_TERM_APPOINTMENT_QA_GROUP_NAME && dbForm.NextApprover != LeaveForms.TIER_THREE)
                    {
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, groupId: LeaveForms.DOT_DVS_FIXED_TERM_APPOINTMENT_QA_GROUP_ID));
                        dbForm.NextApprovalLevel = LeaveForms.DOT_DVS_FIXED_TERM_APPOINTMENT_QA_GROUP_NAME;
                        dbForm.NextApprover = LeaveForms.DOT_DVS_FIXED_TERM_APPOINTMENT_QA_GROUP_NAME;
                        statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                        {
                            SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                            SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                            SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                        };
                    }
                    else if (RequestingUser.EmployeeManagementTier is <= 3)
                    {
                        dbForm.NextApprover = group.GroupName; //await GetPodEformsBusinessAdminGroup();
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, groupId: LeaveForms.POD_ESO_GROUP_ID));
                        dbForm.NextApprovalLevel = group.GroupName;
                        dbForm.NextApprover = group.GroupName;
                        statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                        {
                            SetStatusBtnData(FormStatus.Completed, FormStatus.Complete.ToString(), true),
                            SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                            SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                        };
                    }
                    else
                    {
                        permissions.Add(new FormPermission
                        {
                            PermissionFlag = (byte)PermissionFlag.UserActionable,
                            PositionId = RequestingUser.ExecutiveDirectorPositionId
                        });
                        dbForm.NextApprover = RequestingUser.ExecutiveDirectorIdentifier;
                        dbForm.NextApprovalLevel = EmployeeTitle.Tier3.ToString();
                        dbForm.NextApprover = EmployeeTitle.Tier3.ToString();
                        statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                        {
                            SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                            SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                            SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                        };
                    }
                    break;
                case FormStatus.Delegated:
                case FormStatus.IndependentReview:
                    permissions.Add(new FormPermission((byte)PermissionFlag.View, groupId: LeaveForms.POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID));
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
                    if (leaveCashOutModel.IsLeaveRequesterIsManager == "Yes")
                    {
                        permissions.Add(new FormPermission((byte)PermissionFlag.View, positionId: RequestingUser.EmployeePositionId, isOwner: true));
                    }
                    else
                    {
                        permissions.Add(new FormPermission((byte)PermissionFlag.View, userId: RequestingUser.ActiveDirectoryId, isOwner: true));
                    }
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


            try
            {

                if (invalidRequest is "")
                {
                    var formInfo = await FormInfoService.SaveLeaveFormsInfoAsync(request, dbForm);

                    await UpdateTasksAsyncLeaveCashOut(formInfo, FormType.LcR_LCO, formInfoInsertModel.FormDetails.FormStatusID);
                    await _permissionManager.UpdateFormPermissionsAsync(formInfo.FormInfoId, permissions);
                    await UpdateHistoryAsync(request, formInfo, ActioningGroup?.Id, formInfoInsertModel.AdditionalInfo, leaveCashOutModel.ReasonForDecision);
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
}
