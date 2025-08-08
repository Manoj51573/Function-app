using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure;
using eforms_middleware.Interfaces;
using System.Threading.Tasks;
using eforms_middleware.DataModel;
using eforms_middleware.Constants;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using eforms_middleware.Constants.TPAC;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using DoT.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;

namespace eforms_middleware.Workflows
{
    public class TravelProposalApprovalService : BaseApprovalService
    {
        private IRepository<SalaryRate> _SalaryRateRepository;
        private IRepository<RefRegion> _refRegionRepo;
        private IRepository<TravelLocation> _refLocationRepo;

        public TravelProposalApprovalService(IFormEmailService formEmailService
            , IRepository<AdfGroup> adfGroup
            , IRepository<AdfPosition> adfPosition
            , IRepository<AdfUser> adfUser
            , IRepository<AdfGroupMember> adfGroupMember
            , IRepository<FormInfo> formInfoRepository
            , IRepository<RefFormStatus> formRefStatus
            , IRepository<FormHistory> formHistoryRepository
            , IPermissionManager permissionManager
            , IFormInfoService formInfoService
            , IConfiguration configuration
            , ITaskManager taskManager
            , IFormHistoryService formHistoryService
            , IRequestingUserProvider requestingUserProvider
            , IEmployeeService employeeService
            , IRepository<WorkflowBtn> workflowBtnRepository
            , IRepository<SalaryRate> salaryRateRepository
            , IRepository<RefRegion> refRegionRepo
            , IRepository<TravelLocation> refLocationRepo
            ) : base(formEmailService
                      , adfGroup
                      , adfPosition
                      , adfUser
                      , adfGroupMember
                      , formInfoRepository
                      , formRefStatus
                      , formHistoryRepository
                      , permissionManager, formInfoService, configuration, taskManager, formHistoryService, requestingUserProvider, employeeService, workflowBtnRepository)
        {
            _SalaryRateRepository = salaryRateRepository;
            _refRegionRepo = refRegionRepo;
            _refLocationRepo = refLocationRepo;
        }

        public async Task<int> TravelWorkFlow(FormInfoRequest travelFormInfo)
        {
            try
            {
                var workFlow = new WorkflowModel();
                var responseData = JsonConvert.DeserializeObject<TravelFormResponseModel>(travelFormInfo.Response);

                var currentForm = travelFormInfo.FormId == 0 ? null : await _formInfoRepository.FirstOrDefaultAsync(x => x.FormInfoId == travelFormInfo.FormId);

                var formOwner = currentForm == null
                    ? await GetAdfUserByEmail(travelFormInfo.FormOwnerEmail)
                    : await GetAdfUserByEmail(currentForm.FormOwnerEmail);

                workFlow = travelFormInfo.StatusId switch
                {
                    (int)FormStatus.Unsubmitted or (int)FormStatus.Completed => await SetToDefault(travelFormInfo, responseData),
                    (int)FormStatus.Submitted when formOwner.EmployeeManagementTier == 1 => await AssignToEDODG(travelFormInfo, responseData),
                    (int)FormStatus.Submitted when formOwner.EmployeeManagementTier == 2
                                        || (!formOwner.Managers.Any() && formOwner.EmployeeManagementTier != 4) => await AssignToPTSTeam(travelFormInfo, responseData),
                    (int)FormStatus.Submitted when travelFormInfo.ActionBy != travelFormInfo.FormOwnerEmail => await SubmittedSplit(travelFormInfo, responseData),
                    (int)FormStatus.Submitted => await TravelSubmitted(travelFormInfo, responseData),
                    (int)FormStatus.Delegated => await TravelDelegated(travelFormInfo, responseData),
                    (int)FormStatus.Approved when currentForm.FormStatusId == (int)FormStatus.Delegated =>
                        await TravelDelegationApproved(travelFormInfo, responseData),
                    (int)FormStatus.Approved when responseData.HasReconsiled
                                        || travelFormInfo.AllFormsId == (int)FormType.TPAC_SDTC => await TravelReadyToComplete(travelFormInfo),
                    (int)FormStatus.Approved when formOwner.EmployeeManagementTier == 1
                                            && !responseData.HasReconsiled => await TravelApproved(travelFormInfo, responseData),
                    (int)FormStatus.Approved when travelFormInfo.AllFormsId == (int)FormType.TPAC_MDTC
                                        && !responseData.HasReconsiled
                                        && !responseData.ReadyForReconsiliation
                                        && responseData.DestinationType.ToLower() == "interstate" => await TravelMultiDayApproval(travelFormInfo, responseData),
                    (int)FormStatus.Approved => await TravelApproved(travelFormInfo, responseData),
                    _ => throw new ArgumentOutOfRangeException()
                };

                travelFormInfo.Response = JsonConvert.SerializeObject(responseData);
                var formInfo = workFlow.FormInfoRequest.FormId is 0 or null
                                ? await CreateFormInfo(workFlow.FormInfoRequest)
                                : await UpdateFormInfo(workFlow.FormInfoRequest);
                await SetButton(formInfo, workFlow);
                await SendEmail(workFlow, formInfo);
                return travelFormInfo.ActionBy == travelFormInfo.FormOwnerEmail
                    ? formInfo.FormInfoId
                    : 0;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<WorkflowModel> SetToDefault(FormInfoRequest travelFormInfo
                        , TravelFormResponseModel responseData)
        {
            responseData.ShowActualCost = false;
            responseData.HasReconsiled = false;
            return await SetFormDefault(travelFormInfo);
        }

        public async Task<JsonResult> TravelApprovalWorkFlow(FormInfoRequest travelFormInfo)
        {
            var result = new JsonResult(null);
            var formInfo = await _formInfoRepository.FirstOrDefaultAsync(x => x.FormInfoId == travelFormInfo.FormId);
            travelFormInfo.Response = formInfo.Response;
            travelFormInfo.AllFormsId = formInfo.AllFormsId;
            await TravelWorkFlow(travelFormInfo);

            result.Value = new
            {
                outcome = "Success",
                formInfoId = formInfo.FormInfoId
            };
            result.StatusCode = StatusCodes.Status200OK;
            return result;
        }

        public async Task<JsonResult> TravelApproval(FormInfoRequest travelFormInfo)
        {
            var result = new JsonResult(null);
            try
            {
                List<int> formInfoIds = new List<int>();
                List<string> formOwnersLst = new List<string>();
                var responseData = JsonConvert.DeserializeObject<TravelFormResponseModel>(travelFormInfo.Response);

                if (responseData.IsMainTraveller == "Yes"
                    || travelFormInfo.StatusId == (int)FormStatus.Unsubmitted)
                {
                    formOwnersLst.Add(travelFormInfo.ActionBy);
                }
                if (responseData.IsAdditionalTravellerAllowed
                    && travelFormInfo.StatusId == (int)FormStatus.Submitted
                    && responseData.AllTravellers != null)
                {
                    responseData.AllTravellers.ForEach(x =>
                    {
                        formOwnersLst.Add(x.EmployeeEmail);
                    });
                }

                var actualStatus = travelFormInfo.StatusId;
                travelFormInfo.AllowPersonalInfo = formOwnersLst.Count == 1;
                foreach (var (formOwners, i) in formOwnersLst.Select((value, i) => (value, i)))
                {
                    travelFormInfo.FormOwnerEmail = formOwners;
                    travelFormInfo.StatusId = actualStatus;
                    travelFormInfo.FormId = i == 0 ? travelFormInfo.FormId : 0;
                    formInfoIds.Add(await TravelWorkFlow(travelFormInfo));
                }

                formInfoIds = formInfoIds.Where(x => x != 0).ToList();
                result.Value = new
                {
                    outcome = "Success",
                    formInfoId = formInfoIds.Any() ? formInfoIds.First() : 0,
                };
                result.StatusCode = StatusCodes.Status200OK;
                return result;
            }
            catch (Exception ex)
            {
                result.Value = new
                {
                    outcome = "Error: Bad request, update your form."
                };
                result.StatusCode = StatusCodes.Status400BadRequest;
                return result;
            }
        }

        //This is when the form splits and new user only needs form to be created.
        public async Task<WorkflowModel> SubmittedSplit(FormInfoRequest travelFormInfo
            , TravelFormResponseModel responseData)
        {
            var emailNotificationModel = new EmailNotificationModel();
            emailNotificationModel.EmailSendType = new List<EmailSendType>() {
                EmailSendType.OnBehalf
            };
            travelFormInfo.StatusId = (int)FormStatus.Unsubmitted;
            travelFormInfo.FormSubStatus = "Created";
            travelFormInfo.IsUpdateAllowed = true;
            var wrkFlow = await SetFormDefault(travelFormInfo);
            wrkFlow.EmailNotificationModel = emailNotificationModel;
            responseData.IsMainTraveller = "Yes";
            responseData.AllTravellers = null;
            responseData.IsAdditionalTravellerAllowed = false;
            if (!travelFormInfo.AllowPersonalInfo)
            {
                responseData.EmailAddress = "";
                responseData.PersonalMobileNumber = "";
            }

            await UpdateOnBehalfUser(travelFormInfo.FormId, travelFormInfo.FormOwnerEmail);
            return wrkFlow;
        }

        public async Task<WorkflowModel> TravelSubmitted(FormInfoRequest travelFormInfo, TravelFormResponseModel responseData)
        {
            var emailNotificationModel = new EmailNotificationModel();
            emailNotificationModel.EmailSendType = new List<EmailSendType>();
            responseData.IsAdditionalTravellerAllowed = false;
            travelFormInfo.IsUpdateAllowed = true;
            if (travelFormInfo.FormId != 0)
            {
                var formData = await _formInfoRepository.FirstOrDefaultAsync(x => x.FormInfoId == travelFormInfo.FormId);
                var currentResponse = JsonConvert.DeserializeObject<TravelFormResponseModel>(formData.Response);
                responseData.HasReconsiled = currentResponse.ReadyForReconsiliation;
            }

            travelFormInfo.FormSubStatus = ((FormStatus)travelFormInfo.StatusId).ToString();
            var formOwner = await GetAdfUserByEmail(travelFormInfo.FormOwnerEmail);
            var nextApprover = formOwner.Managers.Any() ? formOwner.Managers.FirstOrDefault() :
                formOwner.ExecutiveDirectors.FirstOrDefault();
            travelFormInfo.AllowReminder = true;
            travelFormInfo.NextApproverEmail = nextApprover.EmployeeEmail;
            travelFormInfo.NextApproverTitle = nextApprover.EmployeePositionTitle;

            if (responseData.HasReconsiled)
            {
                emailNotificationModel.EmailSendType.Add(EmailSendType.RequestorManagerReconsile);
            }
            else if (nextApprover != null && formOwner.EmployeeManagementTier != 2 && !responseData.ReadyForReconsiliation)
            {
                emailNotificationModel.EmailSendType.Add(EmailSendType.RequestorManager);
            }

            if (responseData.VehicleType == "Private" && !responseData.ReadyForReconsiliation)
            {
                emailNotificationModel.EmailSendType.Add(EmailSendType.PrivateVehicle);
            }

            if (travelFormInfo.AllFormsId == (int)FormType.TPAC_RC && !responseData.ReadyForReconsiliation)
            {
                emailNotificationModel.EmailSendType.Add(EmailSendType.FinanceTeam);
            }

            if (travelFormInfo.AllFormsId is (int)FormType.TPAC_MDTC
                or (int)FormType.TPAC_RC
                && !responseData.ReadyForReconsiliation)
            {
                emailNotificationModel.EmailSendType.Add(EmailSendType.TravelHelpfulTips);
            }

            AllowEscaltion(travelFormInfo, nextApprover);
            var statusBtnData = new StatusBtnData();
            if (formOwner.EmployeeManagementTier == 1 ||
                (nextApprover != null && formOwner.EmployeeManagementTier != 2))
            {
                statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                {
                    SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                    SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                };
            }
            else
            {
                statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                {
                    SetStatusBtnData(FormStatus.Delegated, FormStatus.Delegate.ToString(), true)
                };
            }

            return new WorkflowModel()
            {
                FormInfoRequest = travelFormInfo,
                StatusBtnData = statusBtnData,
                EmailNotificationModel = emailNotificationModel
            };
        }

        public async Task<WorkflowModel> TravelDelegated(FormInfoRequest travelFormInfo
            , TravelFormResponseModel responseData)
        {
            var currentForm = await _formInfoRepository.FirstOrDefaultAsync(x => x.FormInfoId == travelFormInfo.FormId);

            if (currentForm.FormStatusId != (int)FormStatus.Delegated)
            {
                responseData.DelegatedFromEmail = currentForm.NextApprover;
                travelFormInfo.IsUpdateAllowed = true;
            }

            var nextApproverPosition = await GetAdfUserById(travelFormInfo.NextApproverEmail);

            travelFormInfo.NextApproverEmail = nextApproverPosition.EmployeeEmail;
            travelFormInfo.NextApproverTitle = nextApproverPosition.EmployeePositionTitle;
            travelFormInfo.FormSubStatus = ((FormStatus)travelFormInfo.StatusId).ToString();

            travelFormInfo.AllowReminder = true;

            var statusBtnData = new StatusBtnData();
            statusBtnData.StatusBtnModel = new List<StatusBtnModel>
            {
                SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
            };
            var emailNotificationModel = new EmailNotificationModel();
            emailNotificationModel.EmailSendType = new List<EmailSendType>()
            {
                EmailSendType.Delegated
            };

            return new WorkflowModel()
            {
                FormInfoRequest = travelFormInfo,
                StatusBtnData = statusBtnData,
                EmailNotificationModel = emailNotificationModel
            };
        }

        public async Task<WorkflowModel> AssignToPTSTeam(FormInfoRequest travelFormInfo
            , TravelFormResponseModel responseData)
        {
            var emailNotificationModel = new EmailNotificationModel();

            travelFormInfo.AllowReminder = true;
            travelFormInfo = await SetToApproverToGroup(travelFormInfo
                   , ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID);
            emailNotificationModel.EmailSendType = new List<EmailSendType>()
            {
                EmailSendType.PODEscalate
            };
            responseData.HasReconsiled = responseData.ReadyForReconsiliation;
            travelFormInfo.IsUpdateAllowed = true;

            var statusBtnData = new StatusBtnData();
            statusBtnData.StatusBtnModel = new List<StatusBtnModel>
            {
                SetStatusBtnData(FormStatus.Delegated, FormStatus.Delegate.ToString(), true)
            };

            return new WorkflowModel()
            {
                FormInfoRequest = travelFormInfo,
                EmailNotificationModel = emailNotificationModel,
                StatusBtnData = statusBtnData
            };
        }

        public async Task<WorkflowModel> AssignToEDODG(FormInfoRequest travelFormInfo
            , TravelFormResponseModel responseData)
        {
            var emailNotificationModel = new EmailNotificationModel();
            var statusBtnData = new StatusBtnData();
            travelFormInfo = await SetApproverToEdODG(travelFormInfo);

            emailNotificationModel.EmailSendType = new List<EmailSendType> { EmailSendType.RequestorManager };

            if (responseData.VehicleType == "Private")
            {
                emailNotificationModel.EmailSendType.Add(EmailSendType.PrivateVehicle);
            }

            travelFormInfo.AllowReminder = true;
            responseData.HasReconsiled = responseData.ReadyForReconsiliation;
            travelFormInfo.IsUpdateAllowed = true;

            statusBtnData.StatusBtnModel = new List<StatusBtnModel>
            {
                SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                SetStatusBtnData(FormStatus.Unsubmitted,FormStatus.Reject.ToString(), false, FormStatus.Rejected.ToString())
            };

            return new WorkflowModel()
            {
                FormInfoRequest = travelFormInfo,
                StatusBtnData = statusBtnData,
                EmailNotificationModel = emailNotificationModel
            };
        }

        public async Task<WorkflowModel> TravelDelegationApproved(FormInfoRequest travelFormInfo
            , TravelFormResponseModel responseData)
        {
            var currentForm = await _formInfoRepository.FirstOrDefaultAsync(x => x.FormInfoId == travelFormInfo.FormId);
            var formOwner = await GetAdfUserByEmail(currentForm.FormOwnerEmail);

            var currentReponse = JsonConvert.DeserializeObject<TravelFormResponseModel>(currentForm.Response);

            var actionBy = travelFormInfo.ActionBy;
            travelFormInfo.ActionBy = currentReponse.DelegatedFromEmail;

            var workflow = travelFormInfo.StatusId switch
            {
                (int)FormStatus.Approved when responseData.HasReconsiled
                                            || travelFormInfo.AllFormsId == (int)FormType.TPAC_SDTC => await TravelReadyToComplete(travelFormInfo),
                (int)FormStatus.Approved when formOwner.EmployeeManagementTier is 1 or 2
                                        && !responseData.HasReconsiled => await TravelApproved(travelFormInfo, responseData),
                (int)FormStatus.Approved when travelFormInfo.AllFormsId == (int)FormType.TPAC_MDTC
                                    && !responseData.HasReconsiled
                                    && !responseData.ReadyForReconsiliation
                                    && responseData.DestinationType.ToLower() == "interstate" => await TravelMultiDayApproval(travelFormInfo, responseData),
                (int)FormStatus.Approved => await TravelApproved(travelFormInfo, responseData),
                _ => throw new ArgumentOutOfRangeException()
            };

            travelFormInfo.ActionBy = actionBy;
            return workflow;
        }

        public async Task<WorkflowModel> TravelReadyToComplete(FormInfoRequest travelFormInfo)
        {
            var emailNotificationModel = new EmailNotificationModel();
            emailNotificationModel.EmailSendType = new List<EmailSendType>();
            var statusBtnData = new StatusBtnData();
            travelFormInfo = await SetToApproverToGroup(travelFormInfo, ConflictOfInterest.POD_ESO_GROUP_ID);
            travelFormInfo.FormSubStatus = ((FormStatus)travelFormInfo.StatusId).ToString();
            travelFormInfo.AllowReminder = true;

            statusBtnData.StatusBtnModel = new List<StatusBtnModel>
            {
                SetStatusBtnData(FormStatus.Completed, FormStatus.Complete.ToString(), true),
                SetStatusBtnData(FormStatus.Unsubmitted,FormStatus.Reject.ToString(), false, FormStatus.Rejected.ToString())
            };

            emailNotificationModel.EmailSendType.Add(EmailSendType.Approved);
            return new WorkflowModel()
            {
                FormInfoRequest = travelFormInfo,
                StatusBtnData = statusBtnData,
                EmailNotificationModel = emailNotificationModel
            };
        }

        public async Task<WorkflowModel> TravelMultiDayApproval(FormInfoRequest travelFormInfo
            , TravelFormResponseModel responseData)
        {
            var emailNotificationModel = new EmailNotificationModel();
            emailNotificationModel.EmailSendType = new List<EmailSendType>();
            var statusBtnData = new StatusBtnData();
            travelFormInfo.AllowReminder = true;
            travelFormInfo.FormSubStatus = FormStatus.Approved.ToString();
            travelFormInfo.StatusId = (int)FormStatus.Approved;

            var actionBy = await GetAdfUserByEmail(travelFormInfo.ActionBy);
            var approver = actionBy.Managers.Any()
                ? actionBy.Managers.FirstOrDefault()
                : actionBy.ExecutiveDirectors.FirstOrDefault();

            if (approver.EmployeeManagementTier > 3)
            {
                approver = actionBy.ExecutiveDirectors.FirstOrDefault();
            }

            if (approver.EmployeeManagementTier == 2)
            {
                emailNotificationModel.EmailSendType.Add(EmailSendType.Tier2Manager);
            }
            else
            {
                emailNotificationModel.EmailSendType.Add(EmailSendType.RequestorManager);
            }

            travelFormInfo.NextApproverEmail = approver.EmployeeEmail;
            travelFormInfo.NextApproverTitle = approver.EmployeePositionTitle;
            responseData.ReadyForReconsiliation = approver.EmployeeManagementTier == 2;
            travelFormInfo.IsUpdateAllowed = responseData.ReadyForReconsiliation;

            statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                    {
                        SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                        SetStatusBtnData(FormStatus.Unsubmitted,FormStatus.Reject.ToString(), false, FormStatus.Rejected.ToString())
                    };
            emailNotificationModel.EmailSendType.Add(EmailSendType.Approved);

            if (responseData.ReadyForReconsiliation)
            {
                emailNotificationModel.EmailSendType.Add(EmailSendType.Reconsile);
            }

            return new WorkflowModel()
            {
                FormInfoRequest = travelFormInfo,
                StatusBtnData = statusBtnData,
                EmailNotificationModel = emailNotificationModel
            };
        }

        public async Task<WorkflowModel> TravelApproved(FormInfoRequest travelFormInfo
            , TravelFormResponseModel responseData)
        {
            var emailNotificationModel = new EmailNotificationModel();
            var statusBtnData = new StatusBtnData();
            emailNotificationModel.EmailSendType = new List<EmailSendType>();

            travelFormInfo.FormSubStatus = FormStatus.Approved.ToString();
            travelFormInfo.StatusId = (int)FormStatus.Unsubmitted;

            var workFlow = await SetFormDefault(travelFormInfo);
            travelFormInfo = workFlow.FormInfoRequest;

            responseData.ReadyForReconsiliation = true;
            emailNotificationModel.EmailSendType = new List<EmailSendType> {
                EmailSendType.Approved,
                EmailSendType.Reconsile,
                EmailSendType.ReadyToReconsileOwner
            };
            travelFormInfo.IsUpdateAllowed = true;

            if (travelFormInfo.AllFormsId is (int)FormType.TPAC_MDTC or (int)FormType.TPAC_RC
                && responseData.DestinationType.ToLower() == "intrastate")
            {
                emailNotificationModel.EmailSendType.Add(EmailSendType.RegionalManager);
            }

            return new WorkflowModel()
            {
                FormInfoRequest = travelFormInfo,
                StatusBtnData = statusBtnData,
                EmailNotificationModel = emailNotificationModel
            };
        }

        void AllowEscaltion(FormInfoRequest travelForm, IUserInfo nextApprover)
        {
            try
            {
                travelForm.AllowEscalation = nextApprover?.EmployeeManagementTier > 3;
            }
            catch
            {
                travelForm.AllowEscalation = false;
            }
        }

        public async Task SetPTSTeamPermission(int formId)
        {
            var permissionId = await SetAdHocPermission(formId
                              , ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID);
            GetDelegateBtn(formId, permissionId);
        }

        public async Task SetFianceGroupPermission(int formId)
        {
            await SetAdHocPermission(formId
                                  , ConflictOfInterest.ASSETS_AND_TAX_GROUP_ID);
        }

        public async Task<int> GetSalaryRateIdByEmail(string email)
        {
            try
            {
                var position = await GetAdfUserByEmail(email);
                var dt = await _SalaryRateRepository.StoredProc("USP_GET_SALARY_RATE_BY_POSITION @PositionNumber", new SqlParameter("@PositionNumber", position.EmployeePositionId));
                return dt.FirstOrDefault().Id;
            }
            catch
            {

            }
            return 0;
        }

        public async Task<bool> IsRateViewAllowed(string user)
        {
            var showActualCost = false;
            try
            {
                var group = await GetAdfGroupByMemberEmail(user);

                showActualCost = group.Where(x => x.Id == ConflictOfInterest.POD_ESO_GROUP_ID).Any();
            }
            catch
            {
            }
            return showActualCost;
        }

        private async Task SetButton(FormInfo formInfo,
            WorkflowModel workFlow)
        {
            try
            {
                if (formInfo.FormStatusId != (int)FormStatus.Unsubmitted
                    && formInfo.FormStatusId != (int)FormStatus.Completed)
                {
                    await SetNextApproverBtn(formInfo.FormInfoId, workFlow.StatusBtnData);
                    var group = await GetAdfGroupById(ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID);
                    if (formInfo.FormStatusId == (int)FormStatus.Submitted
                         && formInfo.NextApprover != group.GroupEmail)
                    {
                        await SetPTSTeamPermission(formInfo.FormInfoId);
                    }

                    if (formInfo.FormStatusId == (int)FormStatus.Submitted
                         && formInfo.AllFormsId == (int)FormType.TPAC_RC)
                    {
                        await SetFianceGroupPermission(formInfo.FormInfoId);
                    }
                }
            }
            catch
            {

            }
        }

        private async Task SendEmail(WorkflowModel workflow, FormInfo formInfo)
        {
            try
            {
                workflow.EmailNotificationModel.SummaryUrl = $"{workflow.FormInfoRequest.BaseUrl}/travel-proposal/summary/{formInfo.FormInfoId}";
                workflow.EmailNotificationModel.EditFormUrl = $"{workflow.FormInfoRequest.BaseUrl}/travel-proposal/{formInfo.FormInfoId}";

                var claimsLink = $"{workflow.FormInfoRequest.BaseUrl}/allowance-and-claims-request";
                string subject = "";
                string body = "";
                string cc = "";
                var responseData = JsonConvert.DeserializeObject<TravelFormResponseModel>(workflow.FormInfoRequest.Response);

                var formOwner = await GetAdfUserByEmail(formInfo.FormOwnerEmail);
                var actionBy = await GetAdfUserByEmail(workflow.FormInfoRequest.ActionBy);

                var formOwnerFullName = formOwner.EmployeePreferredFullName;
                var actionByFullName = actionBy.EmployeePreferredFullName;

                foreach (var emailType in workflow.EmailNotificationModel.EmailSendType)
                {
                    var emailReceiver = new List<string>();
                    bool isNextApprover = false;
                    switch (emailType)
                    {
                        case EmailSendType.RegionalManager:
                            await RegionalManagerEmail(responseData, formInfo, formOwnerFullName, workflow.EmailNotificationModel.SummaryUrl);
                            break;
                        case EmailSendType.RequestorManagerReconsile:
                            subject = $"Travel Proposal and Allowance eForm #{formInfo.FormInfoId} Reconciliation has been submitted for your approval";
                            body = string.Format(TravelProposalTemplate.RequestManagerReconsile
                                , formOwnerFullName
                                , formInfo.FormInfoId
                                , workflow.EmailNotificationModel.SummaryUrl);
                            isNextApprover = true;
                            break;
                        case EmailSendType.RequestorManager:
                            subject = $"Travel Proposal and Allowance Claim eForm #{formInfo.FormInfoId} requires your approval";
                            body = string.Format(TravelProposalTemplate.RequestManager
                                , formOwnerFullName
                                , formInfo.FormInfoId
                                , responseData.DestinationType
                                , responseData.TravelStartDate?.ToString("dd/MM/yyyy")
                                , responseData.TravelEndDate?.ToString("dd/MM/yyyy")
                                , responseData.TotalIncidentals
                                , responseData.TotalMeal      
                                ,responseData.TotalEstimatedTravelCosts
                                , workflow.EmailNotificationModel.SummaryUrl);
                            isNextApprover = true;
                            break;
                        case EmailSendType.Tier2Manager:
                            var tier2 = await GetAdfUserByEmail(formInfo.NextApprover);
                            subject = $"Travel Proposal and Allowance Claim eForm #{formInfo.FormInfoId} requires your approval";
                            body = string.Format(TravelProposalTemplate.Tier2Manager
                                , tier2.EmployeePreferredName
                                , formOwnerFullName
                                , formInfo.FormInfoId
                                , responseData.DestinationType
                                , responseData.TravelStartDate?.ToString("dd/MM/yyyy")
                                , responseData.TravelEndDate?.ToString("dd/MM/yyyy")
                                , responseData.TotalIncidentals
                                , responseData.TotalMeal
                                , responseData.TotalEstimatedTravelCosts);
                            isNextApprover = true;
                            break;
                        case EmailSendType.OnBehalf:
                            subject = $"Travel Proposal and Allowance eForm #{formInfo.FormInfoId} requires your action";
                            body = string.Format(TravelProposalTemplate.SubmittedOnBehalf
                                , formOwnerFullName
                                , actionByFullName
                                , workflow.EmailNotificationModel.EditFormUrl);
                            emailReceiver.Add(formInfo.FormOwnerEmail);
                            cc = workflow.FormInfoRequest.ActionBy;
                            break;
                        case EmailSendType.PrivateVehicle:
                            subject = $"Motor Vehicle Information for Travel Proposal and Allowance Claim eForm #{formInfo.FormInfoId}";
                            body = string.Format(TravelProposalTemplate.PrivateVehicleNotification
                                , formOwnerFullName
                                , claimsLink);
                            emailReceiver.Add(formInfo.FormOwnerEmail);
                            break;
                        case EmailSendType.Completed:
                            subject = $"Travel Proposal and Allowance Claim eForm #{formInfo.FormInfoId} has been processed";
                            body = string.Format(TravelProposalTemplate.Completed
                                , formOwnerFullName
                                , formInfo.FormInfoId
                                , responseData.DestinationType
                                , responseData.TravelStartDate?.ToString("dd/MM/yyyy")
                                , responseData.TravelEndDate?.ToString("dd/MM/yyyy")
                                , responseData.TotalIncidentals
                                , responseData.TotalMeal
                                , responseData.TotalEstimatedTravelCosts
                                , workflow.EmailNotificationModel.SummaryUrl);
                            emailReceiver.Add(formInfo.FormOwnerEmail);
                            break;
                        case EmailSendType.Rejected:
                            subject = $"Travel Proposal and Allowance eForm #{formInfo.FormInfoId} has been rejected";
                            body = string.Format(TravelProposalTemplate.Rejected
                                , formOwnerFullName
                                , actionByFullName
                                , formInfo.FormInfoId
                                , workflow.FormInfoRequest.ReasonForRejection
                                , workflow.EmailNotificationModel.EditFormUrl);
                            emailReceiver.Add(formInfo.FormOwnerEmail);
                            break;
                        case EmailSendType.Escalated:
                            subject = $"Travel Proposal and Allowance eForm #{formInfo.FormInfoId} has escalated for your approval";
                            body = string.Format(TravelProposalTemplate.Escalated
                                , formOwnerFullName
                                , formInfo.FormInfoId
                                , workflow.EmailNotificationModel.SummaryUrl);
                            isNextApprover = true;
                            break;
                        case EmailSendType.Group:
                            subject = $"Travel Proposal and Allowance eForm #{formInfo.FormInfoId} has escalated for your action";
                            body = string.Format(TravelProposalTemplate.PTSGroup
                                , formOwnerFullName
                                , formInfo.FormInfoId
                                , workflow.EmailNotificationModel.SummaryUrl);
                            var ptsGroup = await GetAdfGroupById(ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID);
                            emailReceiver.Add(ptsGroup.GroupEmail);
                            break;
                        case EmailSendType.Delegated:
                            subject = $"Travel Proposal and Allowance eForm #{formInfo.FormInfoId} has been delegated for your approval";
                            body = string.Format(TravelProposalTemplate.Delegated
                                , formOwnerFullName
                                , formInfo.FormInfoId
                                , workflow.EmailNotificationModel.SummaryUrl);
                            isNextApprover = true;
                            break;
                        case EmailSendType.Cancelled:
                            subject = $"Travel Proposal and Allowance eForm #{formInfo.FormInfoId} has been cancelled";
                            body = string.Format(TravelProposalTemplate.Cancelled
                                , formOwnerFullName
                                , formInfo.FormInfoId
                                , workflow.EmailNotificationModel.EditFormUrl);
                            cc = await GetPodEformsBusinessAdminGroup();
                            isNextApprover = true;
                            break;
                        case EmailSendType.Recalled:
                            subject = $"Travel Proposal and Allowance eForm #{formInfo.FormInfoId} has been recalled";
                            body = string.Format(TravelProposalTemplate.Recalled
                                , formOwnerFullName
                                , formInfo.FormInfoId
                                , workflow.EmailNotificationModel.EditFormUrl);
                            cc = workflow.FormInfoRequest.PreviousApprover;
                            emailReceiver.Add(formInfo.FormOwnerEmail);
                            break;
                        case EmailSendType.Reconsile:
                            var endDate = Convert.ToDateTime(responseData.TravelEndDate);
                            var dateDiff = DateTime.Now - endDate;
                            var reminderDay = responseData.TravelEndDate <= DateTime.Today ? 1 : (int)dateDiff.TotalDays;
                            await UpdateTasksAsync(formInfo
                                 , isEscalationAllowed: false
                                 , remiderDayInterval: reminderDay);
                            workflow.FormInfoRequest.AllowReminder = false;
                            break;
                        case EmailSendType.PODEscalate:
                            subject = $"Travel Proposal and Allowance eForm #{formInfo.FormInfoId} has escalated for your action";
                            body = string.Format(TravelProposalTemplate.PODEscalate
                                , formOwnerFullName
                                , formInfo.FormInfoId
                                , workflow.EmailNotificationModel.SummaryUrl);
                            isNextApprover = true;
                            break;
                        case EmailSendType.ReadyToReconsileOwner:
                            subject = $"Travel Proposal and Allowance eForm #{formInfo.FormInfoId} has been approved";
                            body = string.Format(TravelProposalTemplate.ReadyToReconsileOwner
                                                , formOwnerFullName
                                                , formInfo.FormInfoId
                                                , workflow.EmailNotificationModel.SummaryUrl);
                            emailReceiver.Add(formInfo.FormOwnerEmail);
                            break;
                        case EmailSendType.FinanceTeam:
                            subject = $"Relieving Allowance Claim eForm #{formInfo.FormInfoId} requires your investigation";
                            body = string.Format(TravelProposalTemplate.FinanceTeamMail
                                                , formOwnerFullName
                                                , workflow.EmailNotificationModel.SummaryUrl
                                                , formOwner.EmployeeFirstName
                                                , responseData.DestinationType
                                                , responseData.TravelStartDate?.ToString("dd/MM/yyyy")
                                                , responseData.TravelEndDate?.ToString("dd/MM/yyyy")
                                                , responseData.TotalIncidentals
                                                , responseData.TotalMeal
                                                , responseData.TotalEstimatedTravelCosts);
                            var financeGroup = await GetAdfGroupById(ConflictOfInterest.ASSETS_AND_TAX_GROUP_ID);
                            emailReceiver.Add(financeGroup.GroupEmail);
                            break;
                        case EmailSendType.TravelHelpfulTips:
                            subject = $"Helpful Information for Travel Proposal and Allowance Claim eForm #{formInfo.FormInfoId}";
                            body = string.Format(TravelProposalTemplate.HelpfulInfo
                                , formOwnerFullName
                                , formInfo.FormInfoId
                                , workflow.EmailNotificationModel.SummaryUrl);
                            emailReceiver.Add(formInfo.FormOwnerEmail);
                            break;
                    }

                    if (isNextApprover)
                    {
                        var isGroup = await GetAdfGroupByEmail(workflow.FormInfoRequest.NextApproverEmail);
                        if (isGroup == null)
                        {
                            var approvers = await GetAdfUserByEmail(workflow.FormInfoRequest.NextApproverEmail);
                            var position = await GetAdfUserByPositionId(approvers.EmployeePositionId ?? 0);
                            emailReceiver = position.Select(x => x.EmployeeEmail).ToList();
                        }
                        else
                        {
                            emailReceiver.Add(workflow.FormInfoRequest.NextApproverEmail);
                        }
                    }

                    if (!string.IsNullOrEmpty(body))
                    {
                        foreach (var email in emailReceiver)
                        {
                            await _formEmailService.SendEmail(formInfo.FormInfoId
                                    , email
                                    , subject
                                    , body
                                    , cc);
                        }
                    }
                }
                if (workflow.FormInfoRequest.AllowReminder)
                {
                    await UpdateTasksAsync(formInfo, workflow.FormInfoRequest.AllowEscalation, 2, 3);
                }
            }
            catch
            {
            }
        }

        public async Task RegionalManagerEmail(TravelFormResponseModel responseData
            , FormInfo formInfo
            , string formOwnerFullName
            , string summaryUrl)
        {
            var sentManager = new List<string>();
            foreach (var dt in responseData.ItineraryDetails)
            {
                try
                {
                    IList<IUserInfo> managers = new List<IUserInfo>();

                    var location = await _refLocationRepo.FirstOrDefaultAsync(x => x.Location == dt.Location);
                    var region = await _refRegionRepo.FirstOrDefaultAsync(x => x.Id == location.RefRegionalId);

                    if (region != null && !string.IsNullOrEmpty(region?.RegionalOpsPositionTitle))
                    {
                        managers = await GetAdfUserByPositionId(Convert.ToInt32(region.RegionalOpsPositionNumber));
                    }
                    else
                    {
                        managers = await GetAdfUserByPositionId(ConflictOfInterest.REGIONAL_ED_POSITION_ID);
                    }

                    if (!sentManager.Where(x => x == managers.FirstOrDefault().EmployeePositionId.ToString()).Any())
                    {
                        foreach (var manager in managers)
                        {
                            string subject = $"Approved Travel for your Region";
                            string body = string.Format(TravelProposalTemplate.RegionalManager
                                  , manager.EmployeePreferredFullName
                                  , formInfo.FormInfoId
                                  , formOwnerFullName
                                  , dt.Location
                                  , dt.Date
                                  , summaryUrl);

                            await _formEmailService.SendEmail(formInfo.FormInfoId
                                , manager.EmployeeEmail
                                , subject
                                , body
                                , string.Empty);
                        }

                        await SetAdHocPermission(formInfo.FormInfoId
                                , positionId: managers.FirstOrDefault().EmployeePositionId);
                        sentManager.Add(managers.FirstOrDefault().EmployeePositionId.ToString());
                    }
                }
                catch
                {

                }
            }

        }
    }
}

