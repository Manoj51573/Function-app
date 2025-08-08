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
using eforms_middleware.Constants.OPR;
using DoT.Infrastructure.Interfaces;
using Newtonsoft.Json.Linq;
using Microsoft.Identity.Client;
using Microsoft.SqlServer.Server;
using Microsoft.Azure.KeyVault.Models;

namespace eforms_middleware.Workflows
{
    public class WHSIncidentReportApprovalService : BaseApprovalService
    {
        private readonly IRepository<AllForm> _allFormRepository;

        public WHSIncidentReportApprovalService(
            IFormEmailService formEmailService,
            IRepository<AdfGroup> adfGroup,
            IRepository<AdfPosition> adfPosition,
            IRepository<AdfUser> adfUser,
            IRepository<AdfGroupMember> adfGroupMember,
            IRepository<FormInfo> formInfoRepository,
            IRepository<RefFormStatus> formRefStatus,
            IRepository<FormHistory> formHistoryRepository,
            IRequestingUserProvider requestingUserProvider,
            IPermissionManager permissionManager,
            IFormInfoService formInfoService
            , IConfiguration configuration
            , ITaskManager taskManager
            , IFormHistoryService formHistoryService
            , IEmployeeService employeeService
            , IRepository<WorkflowBtn> workflowBtnRepository
            , IRepository<AllForm> allFormRepository
            ) : base(
                formEmailService
                      , adfGroup
                      , adfPosition
                      , adfUser
                      , adfGroupMember
                      , formInfoRepository
                      , formRefStatus
                      , formHistoryRepository
                      , permissionManager
                , formInfoService
                , configuration
                , taskManager
                , formHistoryService
                , requestingUserProvider
                , employeeService
                , workflowBtnRepository)
        {
            _allFormRepository = allFormRepository;
        }

        public async Task<IList<IUserInfo>> getUserManager(string userEmail)
        {
            var user = await GetAdfUserByEmail(userEmail);
            bool IsUserContractor = false;

            if (string.IsNullOrEmpty(user.EmployeePositionNumber))
                IsUserContractor = true;

            dynamic reqManagers;

            reqManagers = user.Managers;
            //reqManagers = user.Managers.Any() ? user.Managers : user.ExecutiveDirectors;

            if (IsUserContractor)
            {
                var adfUser = await GetUserByEmail(user.EmployeeEmail);
                reqManagers = await GetAdfUserByPositionId((int)adfUser.ReportsToPositionId);
            }

            if(user.EmployeeManagementTier == 4)
            {
                reqManagers = user.ExecutiveDirectors;
            }

            return reqManagers;

        }

        public async Task<JsonResult> WHSIRApproval(FormInfoRequest whsirFormInfo)
        {
            var result = new JsonResult(null);
            try
            {
                var workFlow = new WorkflowModel();

                var formInfo = new FormInfo();

                dynamic responseData;
                dynamic managerFormData;

                var formInfoExisting = await _formInfoRepository.FirstOrDefaultAsync(x => x.FormInfoId == whsirFormInfo.FormId);

                var formOwner = formInfoExisting == null
                   ? await GetAdfUserByEmail(whsirFormInfo.FormOwnerEmail)
                   : await GetAdfUserByEmail(formInfoExisting.FormOwnerEmail);

                var formOwnerManagers = await getUserManager(formOwner.EmployeeEmail);

                switch (whsirFormInfo.StatusId)
                {
                    
                    case (int)FormStatus.Submitted when formOwner.EmployeeManagementTier == 1:

                        workFlow = await AssignToEDODG(whsirFormInfo);
                        break;

                    case (int)FormStatus.Submitted when (formOwner.EmployeeManagementTier == 2 || formOwnerManagers.Count == 0):
                        workFlow = await AssignToPTSTeam(whsirFormInfo);
                        break;
                    case (int)FormStatus.Submitted:

                        workFlow = await WHSIRSubmitted(whsirFormInfo);

                        break;

                    case (int)FormStatus.Approved when whsirFormInfo.FormSubStatus == "Approved":

                        workFlow = await WHSIRApproved(whsirFormInfo);

                        break;
                    case (int)FormStatus.Approved when whsirFormInfo.FormSubStatus == "Saved":

                        workFlow = await WHSIRTeamFormSaved(whsirFormInfo);

                        break;

                    case (int)FormStatus.Delegated:
                        workFlow = await WHSIRDelegated(whsirFormInfo);
                        break;
                    case (int)FormStatus.Unsubmitted when (whsirFormInfo.FormSubStatus == "Recalled" || whsirFormInfo.FormSubStatus == "Rejected"):
                        whsirFormInfo.Response = formInfoExisting.Response;
                        workFlow = await SetFormDefault(whsirFormInfo);
                        break;
                    case (int)FormStatus.Unsubmitted when whsirFormInfo.FormSubStatus == "Saved":
                        // whsirFormInfo.Response = whsirFormInfo.Response;
                        workFlow = await SetFormDefault(whsirFormInfo);
                        break;
                    case (int)FormStatus.Completed:

                        managerFormData = JsonConvert.DeserializeObject(whsirFormInfo.AdditionalInfo);
                        responseData = JsonConvert.DeserializeObject(formInfoExisting.Response);
                        responseData.whsTeamReviewForm = JsonConvert.DeserializeObject(whsirFormInfo.AdditionalInfo);
                        whsirFormInfo.AdditionalInfo = "";
                        whsirFormInfo.Response = JsonConvert.SerializeObject(responseData);
                        workFlow = await SetFormDefault(whsirFormInfo);
                        whsirFormInfo.IsUpdateAllowed = true;

                        break;
                }

                formInfo = workFlow.FormInfoRequest.FormId == 0 || workFlow.FormInfoRequest.FormId == null
                                ? await CreateFormInfo(workFlow.FormInfoRequest)
                                : await UpdateFormInfo(workFlow.FormInfoRequest);


                await SetButton(formInfo, workFlow);

                workFlow = await SetExtraPermissions(formInfo.FormInfoId, workFlow);

                await SendEmail(workFlow, formInfo, whsirFormInfo.FormSubStatus);

                result.Value = new
                {
                    outcome = "Success",
                    formInfoId = formInfo.FormInfoId.ToString()
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

        private async Task<WorkflowModel> SetExtraPermissions(int formId, WorkflowModel workFlow)
        {

            //formId = (int) workFlow.FormInfoRequest.FormId;
            if (workFlow.FormInfoRequest.Response != null)
            {
                dynamic responseData = JsonConvert.DeserializeObject(workFlow.FormInfoRequest.Response);

                if (workFlow.FormInfoRequest.StatusId == (int)FormStatus.Submitted)
                {

                    if (responseData.onBehalfFlag == "Yes" && !object.ReferenceEquals(null, responseData.onBehalfEmployee) )
                    {
                        var onBehalfEmployee = await GetAdfUserByEmail((string)responseData.onBehalfEmployee[0].employeeEmail);
                        workFlow.EmailNotificationModel.EmailSendType.Add(EmailSendType.OnBehalf);
                        await SetAdHocPermission(formId, null, onBehalfEmployee.EmployeePositionId);
                    }

                    await SetPTSTeamPermission(formId);

                    if(!object.ReferenceEquals(null, responseData.additionalNotifiers) && responseData.additionalNotifiers is JArray)
                    {
                        

                        foreach (var an in responseData.additionalNotifiers)
                        {
                            var anUser = await GetAdfUserByEmail((string)an.employeeEmail);
                            await SetAdHocPermission(formId, null, anUser.EmployeePositionId);
                        }
                    }

                }
            }

            return workFlow;

        }


        public async Task<WorkflowModel> AssignToPTSTeam(FormInfoRequest whsirFormInfo)
        {
            var emailNotificationModel = new EmailNotificationModel();

            whsirFormInfo.AllowReminder = false;
            whsirFormInfo = await SetToApproverToGroup(whsirFormInfo
                   , ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID);
            emailNotificationModel.EmailSendType = new List<EmailSendType>()
            {
                EmailSendType.PODEscalate
            };
           // responseData.HasReconsiled = responseData.ReadyForReconsiliation;
            whsirFormInfo.IsUpdateAllowed = true;

            var statusBtnData = new StatusBtnData();
            statusBtnData.StatusBtnModel = new List<StatusBtnModel>
            {
                SetStatusBtnData(FormStatus.Delegated, FormStatus.Delegate.ToString(), true)
            };

            return new WorkflowModel()
            {
                FormInfoRequest = whsirFormInfo,
                EmailNotificationModel = emailNotificationModel,
                StatusBtnData = statusBtnData
            };
        }

        public async Task<WorkflowModel> AssignToEDODG(FormInfoRequest whsirFormInfo
            )
        {
            var emailNotificationModel = new EmailNotificationModel();
            var statusBtnData = new StatusBtnData();
            whsirFormInfo = await SetApproverToEdODG(whsirFormInfo);

            var nextApprover = await GetEDODG();

            emailNotificationModel.EmailSendType = new List<EmailSendType> { EmailSendType.RequestorManager };

            whsirFormInfo.AllowReminder = true;
           // responseData.HasReconsiled = responseData.ReadyForReconsiliation;
            whsirFormInfo.IsUpdateAllowed = true;

            statusBtnData.StatusBtnModel = new List<StatusBtnModel>
            {
                SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                SetStatusBtnData(FormStatus.Unsubmitted,FormStatus.Reject.ToString(), false, FormStatus.Rejected.ToString())
            };

            AllowEscalation(whsirFormInfo, nextApprover);

            return new WorkflowModel()
            {
                FormInfoRequest = whsirFormInfo,
                StatusBtnData = statusBtnData,
                EmailNotificationModel = emailNotificationModel
            };
        }



        public async Task SetPTSTeamPermission(int formId)
        {
            var permissionId = await SetAdHocPermission(formId
                              , ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID);
            GetDelegateBtn(formId, permissionId);
        }

        private async Task SetButton(FormInfo formInfo,
          WorkflowModel workFlow)
        {
         
            await SetNextApproverBtn(formInfo.FormInfoId, workFlow.StatusBtnData);

        }

        public async Task SetWHSIRTeamPermission(int formId)
        {
            var permissionId = await SetAdHocPermission(formId
                              , ConflictOfInterest.OPR_GROUP_ID);
            GetDelegateBtn(formId, permissionId);
        }

        public async Task<WorkflowModel> WHSIRTeamFormSaved(FormInfoRequest whsirFormInfo)
        {
            var emailNotificationModel = new EmailNotificationModel();
            emailNotificationModel.EmailSendType = new List<EmailSendType>();

            var statusBtnData = new StatusBtnData();

            dynamic managerFormData;
            dynamic responseData;

            whsirFormInfo.IsUpdateAllowed = true;

            var formInfoExisting = await _formInfoRepository.FirstOrDefaultAsync(x => x.FormInfoId == whsirFormInfo.FormId);

            managerFormData = JsonConvert.DeserializeObject(whsirFormInfo.AdditionalInfo);
            responseData = JsonConvert.DeserializeObject(formInfoExisting.Response);
            responseData.whsTeamReviewForm = JsonConvert.DeserializeObject(whsirFormInfo.AdditionalInfo);
            whsirFormInfo.AdditionalInfo = "";
            whsirFormInfo.Response = JsonConvert.SerializeObject(responseData);

            whsirFormInfo = await SetToApproverToGroup(whsirFormInfo, WHSIREmailTemplate.WHS_GROUP_ID);


            return new WorkflowModel()
            {
                FormInfoRequest = whsirFormInfo,
                StatusBtnData = statusBtnData,
                EmailNotificationModel = emailNotificationModel
            };

        }

        public async Task<WorkflowModel> WHSIRApproved(FormInfoRequest whsirFormInfo)
        {
            var emailNotificationModel = new EmailNotificationModel();
            emailNotificationModel.EmailSendType = new List<EmailSendType>();
            emailNotificationModel.EmailSendType.Add(EmailSendType.Approved);
            emailNotificationModel.EmailSendType.Add(EmailSendType.Group);
            whsirFormInfo.IsUpdateAllowed = true;
            whsirFormInfo.AllowReminder = false;
            int formId = (int)whsirFormInfo.FormId;

            dynamic managerFormData;


            managerFormData = JsonConvert.DeserializeObject(whsirFormInfo.AdditionalInfo);
            var formInfoExisting = await _formInfoRepository.FirstOrDefaultAsync(x => x.FormInfoId == whsirFormInfo.FormId);
            dynamic responseData = JsonConvert.DeserializeObject(formInfoExisting.Response);
            responseData.managerForm = JsonConvert.DeserializeObject(whsirFormInfo.AdditionalInfo);

            whsirFormInfo.AdditionalInfo = "";

            whsirFormInfo.Response = JsonConvert.SerializeObject(responseData);

            var statusBtnData = new StatusBtnData();


            if (managerFormData.additionalApprovalLineManager == "Yes")
            {
                //var managers = await getUserManager(whsirFormInfo.ActionBy);
                //var reqMangersManager = await getUserManager((managers.FirstOrDefault()).EmployeeEmail);
                var reqMangersManager = await getUserManager(whsirFormInfo.ActionBy);
               // await SetAdHocPermission(formId, null, reqMangersManager.FirstOrDefault().EmployeePositionId);
            }

            whsirFormInfo = await SetToApproverToGroup(whsirFormInfo, WHSIREmailTemplate.WHS_GROUP_ID);

            whsirFormInfo.FormSubStatus = ((FormStatus)whsirFormInfo.StatusId).ToString();
            statusBtnData.StatusBtnModel = new List<StatusBtnModel>
            {
                SetStatusBtnData(FormStatus.Completed, FormStatus.Complete.ToString(), true),
                SetStatusBtnData(FormStatus.Approved, FormStatus.Save.ToString(),false,FormStatus.Saved.ToString() ),
            };

            whsirFormInfo.FormSubStatus = ((FormStatus)whsirFormInfo.StatusId).ToString();

            return new WorkflowModel()
            {
                FormInfoRequest = whsirFormInfo,
                StatusBtnData = statusBtnData,
                EmailNotificationModel = emailNotificationModel
            };

        }
        void AllowEscalation(FormInfoRequest whsirForm, IUserInfo nextApprover)
        {
            try
            {
                whsirForm.AllowEscalation = nextApprover?.EmployeeManagementTier > 1;
            }
            catch
            {
                whsirForm.AllowEscalation = false;
            }
        }
        public async Task<WorkflowModel> WHSIRSubmitted(FormInfoRequest whsirFormInfo)
        {

            var emailNotificationModel = new EmailNotificationModel();
            emailNotificationModel.EmailSendType = new List<EmailSendType>();
            
            int formId = (int)whsirFormInfo.FormId;

            whsirFormInfo.IsUpdateAllowed = true;
            // whsirFormInfo = await SetToApproverToGroup(whsirFormInfo, ConflictOfInterest.OPR_GROUP_ID);

            var formOwner = await GetAdfUserByEmail(whsirFormInfo.FormOwnerEmail);
            // var nextApprover = formOwner.Managers.Any() ? formOwner.Managers.FirstOrDefault() : formOwner.ExecutiveDirectors.FirstOrDefault();
            var managers = await getUserManager(whsirFormInfo.FormOwnerEmail);
            var nextApprover = managers.FirstOrDefault();

            dynamic responseData = JsonConvert.DeserializeObject(whsirFormInfo.Response);
            var statusBtnData = new StatusBtnData();

            

            if (responseData.lmInformed == "No")
            {
                whsirFormInfo.AllowReminder = true;
                emailNotificationModel.EmailSendType.Add(EmailSendType.Group);
                whsirFormInfo = await SetToApproverToGroup(whsirFormInfo, WHSIREmailTemplate.WHS_GROUP_ID);
                whsirFormInfo.AdditionalInfo = "Line manager approval skipped";

                statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                {
                    SetStatusBtnData(FormStatus.Completed, FormStatus.Complete.ToString(), true),
                    SetStatusBtnData(FormStatus.Approved, FormStatus.Save.ToString(),false,FormStatus.Saved.ToString() ),
                };

                whsirFormInfo.FormSubStatus = ((FormStatus)FormStatus.Approved).ToString();

            }
            else
            {

                whsirFormInfo.AllowReminder = true;
                emailNotificationModel.EmailSendType.Add(EmailSendType.RequestorManager);
                whsirFormInfo.NextApproverEmail = nextApprover.EmployeeEmail;
                whsirFormInfo.NextApproverTitle = nextApprover.EmployeePositionTitle;

                statusBtnData.StatusBtnModel = new List<StatusBtnModel>
                {
                    SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
                    SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )
                };

                whsirFormInfo.FormSubStatus = ((FormStatus)whsirFormInfo.StatusId).ToString();
            }

            AllowEscalation(whsirFormInfo, nextApprover);

            return new WorkflowModel()
            {
                FormInfoRequest = whsirFormInfo,
                StatusBtnData = statusBtnData,
                EmailNotificationModel = emailNotificationModel
            };
        }


        public async Task<WorkflowModel> WHSIRDelegated(FormInfoRequest whsirFormInfo)
        {
            var emailNotificationModel = new EmailNotificationModel();
            emailNotificationModel.EmailSendType = new List<EmailSendType>();
            emailNotificationModel.EmailSendType.Add(EmailSendType.Delegated);

            var nextApproverPosition = await GetAdfUserById(whsirFormInfo.NextApproverEmail);
            whsirFormInfo.NextApproverEmail = nextApproverPosition.EmployeeEmail;
            whsirFormInfo.NextApproverTitle = nextApproverPosition.EmployeePositionTitle;
            var statusBtnData = new StatusBtnData();

            whsirFormInfo.AllowReminder = true;
        //    AllowEscalation(whsirFormInfo, nextApproverPosition);
            // whsirFormInfo.IsUpdateAllowed = true;

            statusBtnData.StatusBtnModel = new List<StatusBtnModel>
            {
               SetStatusBtnData(FormStatus.Approved, FormStatus.Approve.ToString(), true),
               SetStatusBtnData(FormStatus.Unsubmitted, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )

            };
            return new WorkflowModel()
            {
                FormInfoRequest = whsirFormInfo,
                StatusBtnData = statusBtnData,
                EmailNotificationModel = emailNotificationModel
            };
        }

        private async Task SendEmail(WorkflowModel workflow, FormInfo formInfo, string formSubStatus)
        {

            var formOwner = await GetAdfUserByEmail(formInfo.FormOwnerEmail);

            var reqManagers = await getUserManager(formInfo.FormOwnerEmail);
            string managerEmail = "";
            string managerManagerEmail = "";
            string additionalNotifiersEmail = "";
            string completedEmaiReceiver = "";

            dynamic responseData = JsonConvert.DeserializeObject(formInfo.Response);
            dynamic managerForm = responseData.managerForm;
            string onBehalfEmployeeEmail = "";

            if (responseData.onBehalfFlag == "Yes" && !object.ReferenceEquals(null, responseData.onBehalfEmployee) )
            {
                var onBehalfEmployee = await GetAdfUserByEmail((string)responseData.onBehalfEmployee[0].employeeEmail);
                onBehalfEmployeeEmail = onBehalfEmployee.EmployeeEmail;

            }

            if(!object.ReferenceEquals(null, responseData.additionalNotifiers) && responseData.additionalNotifiers is JArray)
            {
                foreach (var an in responseData.additionalNotifiers)
                {
                    additionalNotifiersEmail += an.employeeEmail + ";";
                }
            }

            foreach (var reqManager in reqManagers)
            {
                managerEmail += reqManager.EmployeeEmail + ";";
            }

            completedEmaiReceiver = managerEmail;


            if (!object.ReferenceEquals(null, managerForm) && managerForm.additionalApprovalLineManager == "Yes")
            {
              //  var reqMan = reqManagers.FirstOrDefault();
              //  var reqManMan = await getUserManager(reqMan.EmployeeEmail);
                var reqManMan = await getUserManager(workflow.FormInfoRequest.ActionBy);
                foreach (var man in reqManMan)
                {
                    managerManagerEmail += man.EmployeeEmail + ";";

                }

            }

            if((responseData.lmInformed == "No"))
            {
                completedEmaiReceiver = "";
            }

            if(formOwner.EmployeeManagementTier == 1)
            {
                managerEmail = "";
                var EDODG = await GetAdfUserByPositionId((int)ConflictOfInterest.ED_ODG_POSITION_ID);
                foreach (var user in EDODG)
                {
                    managerEmail += user.EmployeeEmail + ";";

                }
            }

            workflow.EmailNotificationModel.SummaryUrl = $"{workflow.FormInfoRequest.BaseUrl}/whs-incident-report/summary/{formInfo.FormInfoId}";
            workflow.EmailNotificationModel.EditFormUrl = $"{workflow.FormInfoRequest.BaseUrl}/whs-incident-report/{formInfo.FormInfoId}";
            string subject = "";
            string body = "";
            string cc = "";
            string homeURL = workflow.FormInfoRequest.BaseUrl;
            string FormURL = workflow.FormInfoRequest.BaseUrl + "/whs-incident-report";

            var formOwnerFullName = await GetFullName(formOwner.EmployeeEmail);
            var emailReceiver = workflow.FormInfoRequest.NextApproverEmail;



            foreach (var emailType in workflow.EmailNotificationModel.EmailSendType)
            {
                cc = "";
                emailReceiver = "";
                switch (emailType)
                {
                    case EmailSendType.Group:
                        subject = $"Work Health and Safety Incident Report #{formInfo.FormInfoId} requires your action";
                        body = string.Format(WHSIREmailTemplate.Group
                            , formOwnerFullName
                            , workflow.EmailNotificationModel.SummaryUrl
                            , formInfo.FormInfoId
                            );
                        emailReceiver = "dharti.patel@transport.wa.gov.au;" + WHSIREmailTemplate.WHS_GROUP_EMAIL;
                        break;
                    case EmailSendType.Approved:
                        subject = $"Work Health and Safety Incident Report #{formInfo.FormInfoId} has progressed";
                        body = string.Format(WHSIREmailTemplate.Approved
                            , formOwnerFullName
                            , workflow.EmailNotificationModel.SummaryUrl
                            , formInfo.FormInfoId
                            );
                        emailReceiver = formOwner.EmployeeEmail;
                        cc = managerEmail + ";" + managerManagerEmail + additionalNotifiersEmail + onBehalfEmployeeEmail;
                        break;
                    case EmailSendType.OnBehalf:
                        subject = $"Work Health and Safety Incident Report  #{formInfo.FormInfoId} been submitted on your behalf";
                        body = string.Format(WHSIREmailTemplate.OnBehalf
                            , formOwnerFullName
                            , workflow.EmailNotificationModel.SummaryUrl
                            );
                        emailReceiver = onBehalfEmployeeEmail;
                        cc = formOwner.EmployeeEmail;

                        break;

                    case EmailSendType.RequestorManager:
                        subject = $"Work Health and Safety Incident Report #{formInfo.FormInfoId} has been submitted for your review";
                        body = string.Format(WHSIREmailTemplate.RequestorManager
                            , formOwnerFullName
                            , workflow.EmailNotificationModel.SummaryUrl
                            );

                        emailReceiver = managerEmail;
                        cc = additionalNotifiersEmail;

                        break;

                    case EmailSendType.Recalled:
                        subject = $"Work Health and Safety Incident Report #{formInfo.FormInfoId} has been recalled";
                        body = string.Format(WHSIREmailTemplate.Recalled
                            , formOwnerFullName
                            , workflow.EmailNotificationModel.SummaryUrl
                            , formInfo.FormInfoId
                            );

                        emailReceiver = formOwner.EmployeeEmail;
                        cc = workflow.FormInfoRequest.PreviousApprover + ";" + additionalNotifiersEmail + onBehalfEmployeeEmail; 

                        break;

                    case EmailSendType.Delegated:
                        subject = $"Work Health and Safety Incident Report #{formInfo.FormInfoId} has been delegated for your review";
                        body = string.Format(WHSIREmailTemplate.Delegated
                            , formOwnerFullName
                            , workflow.EmailNotificationModel.SummaryUrl
                            );

                        emailReceiver = formInfo.NextApprover;
                        // cc = ;

                        break;

                    case EmailSendType.Completed:

                        subject = $"Work Health and Safety Incident Report #{formInfo.FormInfoId} has been  investigated and recorded";
                        body = string.Format(WHSIREmailTemplate.Completed
                            , formInfo.FormInfoId
                            , workflow.EmailNotificationModel.SummaryUrl);

                        emailReceiver = formOwner.EmployeeEmail;
                          cc = completedEmaiReceiver + ";" + additionalNotifiersEmail + onBehalfEmployeeEmail;
                        break;

                    case EmailSendType.Rejected:

                        subject = $"Work Health and Safety Incident Report #{formInfo.FormInfoId} has been rejected";
                        body = string.Format(WHSIREmailTemplate.Rejected
                            , formOwnerFullName
                            , await GetFullName(workflow.FormInfoRequest.ActionBy)
                             , formInfo.FormInfoId
                            , workflow.FormInfoRequest.ReasonForRejection
                            , workflow.EmailNotificationModel.SummaryUrl);

                        emailReceiver = formInfo.FormOwnerEmail;
                        //  cc = managerEmail + ";" + otherRequestorEmail + ";" + otherRequestorManagerEmail + ";" + ConflictOfInterest.OPR_GROUP_EMAIL; ;
                        break;
                    case EmailSendType.PODEscalate:

                        subject = $"Work Health and Safety Incident Report #{formInfo.FormInfoId} has escalated for your review";
                        body = string.Format(WHSIREmailTemplate.PODEscalate
                            , formOwnerFullName
                            ,  workflow.EmailNotificationModel.SummaryUrl);

                        emailReceiver = "dharti.patel@transport.wa.gov.au;" + formInfo.NextApprover;
                        //  cc = managerEmail + ";" + otherRequestorEmail + ";" + otherRequestorManagerEmail + ";" + ConflictOfInterest.OPR_GROUP_EMAIL; ;
                        break;

                }

                await _formEmailService.SendEmail(formInfo.FormInfoId
                            , emailReceiver
                            , subject
                            , body
                            , cc);
            }

            if (workflow.FormInfoRequest.AllowReminder)
            {
                await UpdateTasksAsync(formInfo, workflow.FormInfoRequest.AllowEscalation, 2, 3);
            }
        }

    }
}

