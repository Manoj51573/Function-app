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
using static eforms_middleware.Settings.Helper;

namespace eforms_middleware.Services
{
    public class RosterChangeRequestService : BaseApprovalService
    {
        private readonly ILogger<RosterChangeRequestService> _logger;
        private readonly IMessageFactoryService _messageFactoryService;
        public RosterChangeRequestService(IFormEmailService formEmailService,
            IRepository<AdfGroup> adfGroup, IRepository<AdfPosition> adfPosition,
            IRepository<AdfUser> adfUser, IRepository<AdfGroupMember> adfGroupMember,
            IRepository<FormInfo> formInfoRepository, IRepository<RefFormStatus> formRefStatus,
            IRepository<FormHistory> formHistoryRepository, IPermissionManager permissionManager,
            IFormInfoService formInfoService,
            IConfiguration configuration, ITaskManager taskManager,
            IFormHistoryService formHistoryService, IRequestingUserProvider requestingUserProvider,
            IEmployeeService employeeService, IRepository<WorkflowBtn> workflowBtnRepository,
            ILogger<RosterChangeRequestService> logger, IMessageFactoryService messageFactoryService)
            : base(formEmailService, adfGroup, adfPosition, adfUser, adfGroupMember, formInfoRepository, formRefStatus,
                   formHistoryRepository, permissionManager, formInfoService, configuration, taskManager, formHistoryService,
                   requestingUserProvider, employeeService, workflowBtnRepository)
        {
            _logger = logger;
            _messageFactoryService = messageFactoryService;
        }

        public async Task<RequestResult> RosterChangeRequestApprovalProcess(RosterChangeInsertModel formInfoInsertModel)
        {
            await SetupBaseApprovalService(formInfoInsertModel.FormDetails.FormInfoID);
            var permissions = new List<FormPermission>();
            var rosterChangeModel = JsonConvert.DeserializeObject<RosterChangeModel>(formInfoInsertModel.FormDetails.Response);
            var formStatus = formInfoInsertModel.FormAction.GetParseEnum<FormStatus>();
            var workflow = new WorkflowModel();
            var statusBtnData = new StatusBtnData();
            var request = new FormInfoUpdate
            {
                FormAction = formInfoInsertModel.FormAction,
                FormDetails = formInfoInsertModel.ToFormDetailRequest(),
                UserId = string.Empty
            };
            var dbForm = await FormInfoService.GetExistingOrNewFormInfoAsync(request);
            var empServiceGroup = await GetAdfGroupById(RosterChangeRequest.EMP_SERVICE_GROUP_ID);
            var IsRequestorDVS = RequestingUser != null &&
                                    RequestingUser.Directorate.ToUpper().Equals(RosterChangeRequest.DVS_EFFORMS_DIRECTORATE_NAME.ToUpper());
            var NextApproverGroupId = IsRequestorDVS ? RosterChangeRequest.DVS_EFFORMS_BUSINESS_ADMIN_GROUP_ID : RosterChangeRequest.EMP_SERVICE_GROUP_ID;

            dbForm.FormStatusId = (int)formStatus;
            dbForm.FormSubStatus = formStatus.ToString();
            var isRequestorOnBehalf = !string.IsNullOrEmpty(rosterChangeModel.RosterChangeRequestGroup.OtherRequestorFlag)
                                    && bool.Parse(rosterChangeModel.RosterChangeRequestGroup.OtherRequestorFlag);
            switch (formStatus)
            {
                case FormStatus.Unsubmitted:
                    dbForm.Response = JsonConvert.SerializeObject(rosterChangeModel);
                    dbForm.FormApprovers = string.Empty;

                    if (isRequestorOnBehalf)
                    {
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable,
                            positionId: RequestingUser.EmployeePositionId, isOwner: true));
                    }
                    else
                    {
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable,
                            userId: RequestingUser.ActiveDirectoryId, isOwner: true));
                    }

                    break;
                case FormStatus.Rejected:
                    var originalResponse = JsonConvert.DeserializeObject<RosterChangeModel>(dbForm.Response);
                    originalResponse.ReasonForDecision = rosterChangeModel.ReasonForDecision;
                    dbForm.Response = JsonConvert.SerializeObject(originalResponse);
                    goto case FormStatus.Recall;
                case FormStatus.Recall:
                    if (isRequestorOnBehalf)
                    {

                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable,
                                        positionId: OwingPositionUsers.First().EmployeePositionId, isOwner: true));


                    }
                    else
                    {
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable,
                                  userId: FormOwner.ActiveDirectoryId, isOwner: true));

                    }
                    dbForm.FormStatusId = (int)FormStatus.Unsubmitted;
                    dbForm.FormSubStatus = formStatus == FormStatus.Recall ? FormStatus.Recalled.ToString() : formStatus.ToString();
                    dbForm.FormStatusId = (int)FormStatus.Unsubmitted;
                    break;
                case FormStatus.Submitted:
                    rosterChangeModel.ReasonForDecision = string.Empty;
                    dbForm.Response = JsonConvert.SerializeObject(rosterChangeModel);
                    dbForm.FormApprovers = string.Empty;
                    dbForm.SubmittedDate = DateTime.Now;

                    var additionalRecipients = rosterChangeModel.SupportingInfoGroup.AdditionalNotification.ToList();
                    var NextApprovalFormStatus = IsRequestorDVS ? FormStatus.Approved : FormStatus.Completed;
                    var NextApprovalButtonText = IsRequestorDVS ? FormStatus.Approve.ToString() : FormStatus.Complete.ToString();
                    foreach (var recipient in additionalRecipients)
                    {
                        permissions.Add(new FormPermission((byte)PermissionFlag.View, userId: recipient.ActiveDirectoryId, isOwner: false));
                    }

                    if (RequestingUser.EmployeeManagementTier is 1 or 2)
                    {
                        goto case FormStatus.AssignToBackup;
                    }
                    else if (RequestingUser.Managers.Any() || RequestingUser.ExecutiveDirectors.Any())
                    {
                        if (isRequestorOnBehalf)
                        {
                            var requestor = rosterChangeModel.RosterChangeRequestGroup.otherRequestor.First();
                            var isValidEmployee = await IsValidEmployee(requestor.EmployeeEmail);
                            if (!isValidEmployee)
                            {
                                return RequestResult.SuccessRequestWithErrorMessage(new
                                {
                                    Outcome = "Failed",
                                    OutcomeValues = new
                                    {
                                        Error = "Please note this form is only applicable to Employees within Department of Transport. " +
                                        "The form will not be relevant to the employee you have selected."
                                    }
                                });
                            }
                            else
                            {
                                var isValidDirectReport = await this.IsValidDirectReport(rosterChangeModel.RosterChangeRequestGroup
                                                                                            .otherRequestor.First().ActiveDirectoryId);

                                if (!isValidDirectReport)
                                {
                                    return RequestResult.SuccessRequestWithErrorMessage(new
                                    {
                                        outcome = "Failed",
                                        outcomeValues = new
                                        {
                                            error = "Sorry, as you are not the current manager for the selected Employee you are unable to submit this form on their behalf."
                                        }
                                    });
                                }
                            }

                            permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable,
                                                    groupId: NextApproverGroupId));
                            permissions.Add(new FormPermission((byte)PermissionFlag.View, positionId: RequestingUser.EmployeePositionId, isOwner: true));
                            dbForm.NextApprovalLevel = IsRequestorDVS ? RosterChangeRequest.DVS_EFFORMS_BUSINESS_ADMIN_GROUP_NAME : empServiceGroup.GroupName;
                            dbForm.NextApprover = RequestingUser.ExecutiveDirectorIdentifier;
                            dbForm.NextApprovalLevel = EmployeeTitle.Tier3.ToString();

                            statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                            {
                                SetStatusBtnData(NextApprovalFormStatus, NextApprovalButtonText, true),
                                SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                                SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                            };
                        }
                        else
                        {
                            permissions.Add(new FormPermission((byte)PermissionFlag.View, userId: RequestingUser.ActiveDirectoryId, isOwner: true));

                            if (RequestingUser.Managers.Any())
                            {
                                dbForm.NextApprover = RequestingUser.Managers.FirstOrDefault()!.EmployeeEmail;
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
                    var isNextApproverNotDVSGroup = !(dbForm.NextApprover.ToUpper().Equals(RosterChangeRequest.DVS_EFFORMS_BUSINESS_ADMIN_GROUP_NAME.ToUpper())
                                                && dbForm.NextApprovalLevel.ToUpper().Equals(RosterChangeRequest.DVS_EFFORMS_BUSINESS_ADMIN_GROUP_NAME.ToUpper()));
                    var isNextApproverNotFromDvsGroup = isNextApproverNotDVSGroup && IsRequestorDVS && !isRequestorOnBehalf;
                    NextApprovalFormStatus = isNextApproverNotFromDvsGroup ? FormStatus.Approved : FormStatus.Completed;
                    NextApprovalButtonText = isNextApproverNotFromDvsGroup ? FormStatus.Approve.ToString() : FormStatus.Complete.ToString();
                    NextApproverGroupId = isNextApproverNotFromDvsGroup ? RosterChangeRequest.DVS_EFFORMS_BUSINESS_ADMIN_GROUP_ID : RosterChangeRequest.EMP_SERVICE_GROUP_ID;

                    permissions.Add(new FormPermission((byte)PermissionFlag.View, positionId: RequestingUser.EmployeePositionId));
                    permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, groupId: NextApproverGroupId));
                    dbForm.NextApprover = isNextApproverNotFromDvsGroup ? RosterChangeRequest.DVS_EFFORMS_BUSINESS_ADMIN_GROUP_NAME : empServiceGroup.GroupName;
                    dbForm.NextApprovalLevel = isNextApproverNotFromDvsGroup ? RosterChangeRequest.DVS_EFFORMS_BUSINESS_ADMIN_GROUP_NAME : empServiceGroup.GroupName;
                    statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                        {
                            SetStatusBtnData(NextApprovalFormStatus, NextApprovalButtonText, true),
                            SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                            SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                        };
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
                    dbForm.FormSubStatus = FormStatus.Submitted.GetFormStatusTitle();
                    if (isRequestorOnBehalf)
                    {
                        permissions.Add(new FormPermission((byte)PermissionFlag.View, positionId: RequestingUser.EmployeePositionId, isOwner: true));
                    }
                    else
                    {
                        permissions.Add(new FormPermission((byte)PermissionFlag.View, userId: RequestingUser.ActiveDirectoryId, isOwner: true));
                    }
                    if (RequestingUser.EmployeeManagementTier is 1)
                    {
                        var executiveDirectorsOperationsAndGovernance = await EmployeeService.GetEmployeeByPositionNumberAsync(RosterChangeRequest.ED_ODG_POSITION_ID);
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: RosterChangeRequest.ED_ODG_POSITION_ID));
                        dbForm.NextApprover = executiveDirectorsOperationsAndGovernance.GetDescription();
                        dbForm.NextApprovalLevel = EmployeeTitle.ExecutiveDirectorODG.ToString();
                        statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                            {
                                SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                                SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),
                                SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                            };
                    }
                    else if (RequestingUser.EmployeeManagementTier is 2)
                    {
                        dbForm.NextApprover = RosterChangeRequest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_NAME;
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, groupId: RosterChangeRequest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID));
                        dbForm.NextApprovalLevel = RosterChangeRequest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_NAME;
                        statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                            {
                                SetStatusBtnData(FormStatus.Delegated, FormStatus.Delegate.ToString(), true),
                                   SetStatusBtnData(FormStatus.Cancelled, FormStatus.Cancel.ToString(), true),

                            };
                    }
                    break;
            }
            try
            {
                var formInfo = await FormInfoService.SaveFormInfoAsync(request, dbForm);
                await UpdateTasksAsync(formInfo, true);
                await _permissionManager.UpdateFormPermissionsAsync(formInfo.FormInfoId, permissions);
                await UpdateHistoryAsync(request, formInfo, ActioningGroup?.Id, formInfoInsertModel.AdditionalInfo,
                                    rosterChangeModel?.ReasonForDecision ?? string.Empty);

                workflow = new WorkflowModel()
                {
                    StatusBtnData = statusBtnData,
                };

                await SetButton(formInfo, workflow);

                var accessToPodTeam = formStatus.Equals(FormStatus.Submitted)
                                      || formStatus.Equals(FormStatus.Unsubmitted);

                if (accessToPodTeam)
                {
                    var permissionId = await SetAdHocPermission(formInfo.FormInfoId,
                                                RosterChangeRequest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID,
                                                permissionFlag: PermissionFlag.GroupFallback);

                    GetDelegateBtn(formInfo.FormInfoId, permissionId);
                }
                if (Enum.Parse<FormStatus>(request.FormAction) is not FormStatus.Approved)
                {
                    await _messageFactoryService.SendEmailAsync(formInfo, request);
                }

                var isAutoApprove = isRequestorOnBehalf && formStatus.Equals(FormStatus.Submitted);
                if (isAutoApprove)
                {
                    formInfo.FormStatusId = (int)FormStatus.Approved;
                    await UpdateHistoryAsync(request, formInfo, ActioningGroup?.Id, formInfoInsertModel.AdditionalInfo,
                              rosterChangeModel?.ReasonForDecision ?? string.Empty);
                }


                return RequestResult.SuccessfulFormInfoRequest(formInfo);
            }
            catch (Exception e)
            {
                return RequestResult.FailedRequest(StatusCodes.Status500InternalServerError, e.Message);
            }
        }

        public async Task<bool> IsValidEmployee(string email)
        {
            var result = await EmployeeService.GetEmployeeByEmailAsync(email);
            return !string.IsNullOrEmpty(result.EmployeeNumber);
        }

        public async Task<bool> IsValidDirectReport(Guid ActiveDirectoryId)
        {
            var employee = await EmployeeService.GetEmployeeByAzureIdAsync(ActiveDirectoryId);
            var result = employee.ManagerPositionId != RequestingUser.EmployeePositionId
                            && employee.ExecutiveDirectorPositionId != RequestingUser.EmployeePositionId;
            return !result;

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
