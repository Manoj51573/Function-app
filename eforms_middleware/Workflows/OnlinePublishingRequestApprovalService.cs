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

namespace eforms_middleware.Workflows
{
    public class OnlinePublishingRequestApprovalService : BaseApprovalService
    {
        private readonly IRepository<AllForm> _allFormRepository;

        public OnlinePublishingRequestApprovalService(
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
            
            var reqManagers = user.Managers;

            if (IsUserContractor)
            {
                var adfUser = await GetUserByEmail(user.EmployeeEmail);
                reqManagers = await GetAdfUserByPositionId((int)adfUser.ReportsToPositionId);
            }
            
            return reqManagers;

        }

        public async Task<JsonResult> OPRApproval(FormInfoRequest oprFormInfo)
        {
            var result = new JsonResult(null);
            try
            {
                var workFlow = new WorkflowModel();

                var formInfo = new FormInfo();

                string otherRequestorEmail = "";
                string otherRequestorManagerEmail = "";

                var reqManagers = await getUserManager(oprFormInfo.FormOwnerEmail);

  
                var formInfoExisting = await _formInfoRepository.FirstOrDefaultAsync(x => x.FormInfoId == oprFormInfo.FormId);
                List<int?> adHocPermissionEmployees = new List<int?>();
                List<Guid> adHocPermissionContractors = new List<Guid>();

                switch (oprFormInfo.StatusId)
                {
                    case (int)FormStatus.Submitted:
                        workFlow = await OPRSubmitted(oprFormInfo);

                        dynamic responseData = JsonConvert.DeserializeObject(oprFormInfo.Response);
                        if (responseData.otherRequestorFlag == true && !object.ReferenceEquals(null, responseData.otherRequestor))
                        {
                            var otherRequestor = await GetAdfUserByEmail((string)responseData.otherRequestor[0].employeeEmail);
                            var otherRequestorManagers = await getUserManager(otherRequestor.EmployeeEmail);
                            otherRequestorEmail = otherRequestor.EmployeeEmail;

                            if (string.IsNullOrEmpty(otherRequestor.EmployeeNumber))
                                adHocPermissionContractors.Add(otherRequestor.ActiveDirectoryId);
                            else
                                adHocPermissionEmployees.Add((int?)otherRequestor.EmployeePositionId);


                            foreach (var otherRequestorManager in otherRequestorManagers)
                            {
                                if (string.IsNullOrEmpty(otherRequestorManager.EmployeeNumber))
                                    adHocPermissionContractors.Add(otherRequestorManager.ActiveDirectoryId);
                                else
                                    adHocPermissionEmployees.Add((int?)otherRequestorManager.EmployeePositionId);

                                otherRequestorManagerEmail += otherRequestorManager.EmployeeEmail + ";";
                            }
                        }


                        foreach (var manager in reqManagers)
                        {
                            adHocPermissionEmployees.Add((int?)manager.EmployeePositionId);
                        }

                        break;

                    case (int)FormStatus.Delegated:
                        workFlow = await OPRDelegated(oprFormInfo);
                        break;
                    case (int)FormStatus.Unsubmitted:
                    case (int)FormStatus.Completed:
                        oprFormInfo.StatusId = oprFormInfo.FormSubStatus == FormStatus.Recalled.ToString() ? (int)FormStatus.Completed : oprFormInfo.StatusId;
                        workFlow = await SetFormDefault(oprFormInfo);

                        if (oprFormInfo.FormId != 0)
                        {
                            dynamic response = JsonConvert.DeserializeObject(formInfoExisting.Response);
                            if (response.otherRequestorFlag == true && !object.ReferenceEquals(null, response.otherRequestor))
                            {
                                var otherRequestor = await GetAdfUserByEmail((string)response.otherRequestor[0].employeeEmail);
                                otherRequestorEmail = otherRequestor.EmployeeEmail;
                                var otherRequestorManagers = await getUserManager(otherRequestor.EmployeeEmail);
                               

                                foreach (var otherRequestorManager in otherRequestorManagers)
                                {
                                    otherRequestorManagerEmail += otherRequestorManager.EmployeeEmail + ";";
                                }
                            }
                        }

                        break;
                }

                formInfo = workFlow.FormInfoRequest.FormId == 0 || workFlow.FormInfoRequest.FormId == null
                                ? await CreateFormInfo(workFlow.FormInfoRequest)
                                : await UpdateFormInfo(workFlow.FormInfoRequest);

                await SetButton(formInfo, workFlow);

                foreach (int? user in adHocPermissionEmployees)
                {
                    await SetAdHocPermission(formInfo.FormInfoId, null, user);
                }

                foreach (Guid user in adHocPermissionContractors)
                {
                    await SetAdHocPermissionWithADId(formInfo.FormInfoId, user);
                }

                await SendEmail(workFlow, formInfo, otherRequestorEmail, otherRequestorManagerEmail, oprFormInfo.FormSubStatus);
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


        private async Task SetButton(FormInfo formInfo,
          WorkflowModel workFlow)
        {
            if (formInfo.FormStatusId == (int)FormStatus.Delegated || formInfo.FormStatusId == (int)FormStatus.Submitted)
            {
                if (workFlow.StatusBtnData != null)
                {
                    await SetNextApproverBtn(formInfo.FormInfoId, workFlow.StatusBtnData);
                }
                await SetOPRTeamPermission(formInfo.FormInfoId);
            }
            else
            {
                await UpdateNextApproverBtn(formInfo.FormInfoId);
            }
        }

        public async Task SetOPRTeamPermission(int formId)
        {
            var permissionId = await SetAdHocPermission(formId
                              , ConflictOfInterest.OPR_GROUP_ID);
            GetDelegateBtn(formId, permissionId);
        }


        public async Task<WorkflowModel> OPRSubmitted(FormInfoRequest oprFormInfo)
        {

            var emailNotificationModel = new EmailNotificationModel();
            emailNotificationModel.EmailSendType = new List<EmailSendType>();
            emailNotificationModel.EmailSendType.Add(EmailSendType.Group);
            emailNotificationModel.EmailSendType.Add(EmailSendType.RequestorManager);

            oprFormInfo.IsUpdateAllowed = true;
            oprFormInfo = await SetToApproverToGroup(oprFormInfo, ConflictOfInterest.OPR_GROUP_ID);
            oprFormInfo.FormSubStatus = ((FormStatus)oprFormInfo.StatusId).ToString();

            return new WorkflowModel()
            {
                FormInfoRequest = oprFormInfo,
                StatusBtnData = null,
                EmailNotificationModel = emailNotificationModel
            };
        }


        public async Task<WorkflowModel> OPRDelegated(FormInfoRequest oprFormInfo)
        {
            var emailNotificationModel = new EmailNotificationModel();
            emailNotificationModel.EmailSendType = new List<EmailSendType>();
            emailNotificationModel.EmailSendType.Add(EmailSendType.Delegated);

            var nextApproverPosition = await GetAdfUserById(oprFormInfo.NextApproverEmail);
            oprFormInfo.NextApproverEmail = nextApproverPosition.EmployeeEmail;
            oprFormInfo.NextApproverTitle = nextApproverPosition.EmployeePositionTitle;
            var statusBtnData = new StatusBtnData();
            statusBtnData.StatusBtnModel = new List<StatusBtnModel>
            {
                SetStatusBtnData(FormStatus.Completed, FormStatus.Complete.ToString()),
                SetStatusBtnData(FormStatus.Completed, FormStatus.Reject.ToString(),false,FormStatus.Rejected.ToString() )

            };
            return new WorkflowModel()
            {
                FormInfoRequest = oprFormInfo,
                StatusBtnData = statusBtnData,
                EmailNotificationModel = emailNotificationModel
            };
        }

        private async Task SendEmail(WorkflowModel workflow, FormInfo formInfo, string otherRequestorEmail, string otherRequestorManagerEmail, string formSubStatus)
        {

            var formOwner = await GetAdfUserByEmail(formInfo.FormOwnerEmail);

            var reqManagers = await getUserManager(formInfo.FormOwnerEmail);
            string managerEmail = "";
            string managersName = "";

            foreach (var reqManager in reqManagers)
            {
                managerEmail += reqManager.EmployeeEmail + ";";
                if(managersName=="")
                    managersName +=  await GetFullName(reqManager.EmployeeEmail);
                else
                    managersName += "," + await GetFullName(reqManager.EmployeeEmail)  ;
            }

            

            var allFormData = await _allFormRepository.FirstOrDefaultAsync(x => x.AllFormsId == formInfo.AllFormsId);

            workflow.EmailNotificationModel.SummaryUrl = $"{workflow.FormInfoRequest.BaseUrl}/scarf/summary/{formInfo.FormInfoId}";
            workflow.EmailNotificationModel.EditFormUrl = $"{workflow.FormInfoRequest.BaseUrl}/scarf/{formInfo.FormInfoId}";
            string subject = "";
            string body = "";
            string cc = "";
            string homeURL = workflow.FormInfoRequest.BaseUrl;
            string SCARFFormURL = workflow.FormInfoRequest.BaseUrl + "/scarf";

            var digiCommsManager = await GetAdfUserByPositionId(25765);
            var digiCommsManagerFullName = await GetFullName(digiCommsManager[0].EmployeeEmail);

            var formOwnerFullName = await GetFullName(formOwner.EmployeeEmail);
            var emailReceiver = workflow.FormInfoRequest.NextApproverEmail;

            

            foreach (var emailType in workflow.EmailNotificationModel.EmailSendType)
            {
                switch (emailType)
                {
                    case EmailSendType.Group:
                        subject = $"SCARF - {allFormData.FormTitle} #{formInfo.FormInfoId} has been submitted";
                        body = GetScarfEmailText(formInfo, allFormData.FormTitle, formOwnerFullName, managersName, formOwner.Directorate, workflow.EmailNotificationModel.SummaryUrl);
                        emailReceiver = ConflictOfInterest.OPR_GROUP_EMAIL; // + ";" + "dharti.patel@transport.wa.gov.au";
                        //emailReceiver =  "dharti.patel@transport.wa.gov.au";
                        break;

                    case EmailSendType.RequestorManager:
                        subject = $"SCARF - {allFormData.FormTitle} #{formInfo.FormInfoId} has been received";
                        body = string.Format(OPREmailTemplate.SCARFRequestor
                            , formOwnerFullName
                            , allFormData.FormTitle
                            , workflow.EmailNotificationModel.SummaryUrl
                            , homeURL
                            , digiCommsManager[0].EmployeeEmail
                            , digiCommsManagerFullName
                            );

                        emailReceiver = formInfo.FormOwnerEmail;
                        cc = managerEmail + ";" + otherRequestorEmail + ";" + otherRequestorManagerEmail;

                        break;

                    case EmailSendType.Completed:

                        if (formSubStatus == FormStatus.Rejected.ToString())
                        {
                            subject = $"SCARF - {allFormData.FormTitle} #{formInfo.FormInfoId} has been cancelled";
                            body = string.Format(OPREmailTemplate.SCARFRejected
                                , formOwnerFullName
                                , allFormData.FormTitle
                                , workflow.EmailNotificationModel.SummaryUrl
                                , formInfo.FormInfoId
                                , SCARFFormURL
                                , workflow.FormInfoRequest.ReasonForRejection
                                , digiCommsManager[0].EmployeeEmail
                                , digiCommsManagerFullName);
                        }

                        if (formSubStatus == FormStatus.Recalled.ToString())
                        {
                            subject = $"SCARF - {allFormData.FormTitle} #{formInfo.FormInfoId} has been recalled";
                            body = string.Format(OPREmailTemplate.SCARFRecalled
                                , formOwnerFullName
                                , allFormData.FormTitle
                                , workflow.EmailNotificationModel.SummaryUrl
                                , formInfo.FormInfoId
                                , SCARFFormURL
                                , digiCommsManager[0].EmployeeEmail
                                , digiCommsManagerFullName
                            );
                        }

                        if (formSubStatus == FormStatus.Completed.ToString())
                        {
                            subject = $"SCARF - {allFormData.FormTitle} #{formInfo.FormInfoId} has been completed";
                            body = string.Format(OPREmailTemplate.SCARFCompleted
                                , formOwnerFullName
                                , allFormData.FormTitle
                                , workflow.EmailNotificationModel.SummaryUrl
                                , formInfo.FormInfoId
                                , homeURL
                                , workflow.FormInfoRequest.AdditionalInfo
                                , digiCommsManager[0].EmployeeEmail
                                , digiCommsManagerFullName);
                        }


                        emailReceiver = formInfo.FormOwnerEmail;
                        cc = managerEmail + ";" + otherRequestorEmail + ";" + otherRequestorManagerEmail + ";" + ConflictOfInterest.OPR_GROUP_EMAIL; ;
                        break;



                }

                if (formSubStatus != FormStatus.Delegated.ToString()) {
                    await _formEmailService.SendEmail(formInfo.FormInfoId
                                , emailReceiver
                                , subject
                                , body
                                , cc);
                }
            }
        }

        public string  GetScarfEmailText(FormInfo formInfo, String formTitle, String formOwnerFullName, String managersName, string formOwnerDirectorate, string SCARFFormURL)
        {

            string emailText = "<div>Hi DigiComms Team,</div>"
                                + "<div><br>SCARF - " + formTitle + " has submitted. View the <a href=\"" + SCARFFormURL + "\">request</a>.</div>"
                                + "<div><br><strong>SCARF request #</strong> <a href=\"" + SCARFFormURL + "\">"+ formInfo.FormInfoId +"</a></div>"
                                + "<div><br><strong>Date submitted: </strong>" + formInfo.SubmittedDate + "</div>"
                                ;



            dynamic responseData = JsonConvert.DeserializeObject(formInfo.Response);

            switch (formInfo.AllFormsId)
            {
                case 24:
                    emailText += "<div><br><strong>When does the request need to be published?</strong> " + responseData.publishedType.Value + "</div>";
                    if (responseData.publishedType.Value == "Specific date")
                    {
                        emailText += "<div><br><strong>Date:</strong> " + responseData.publishDateTime.Value + "</div>";
                    }

                    emailText += "<div><br><strong>Description:</strong>" + responseData.requestDescription.Value + "</div>";
                    break;
                case 25:
                    emailText += "<div><br><strong>Which service do you require?</strong> " + responseData.selectedServices + "</div>";
                    emailText += "<div><br><strong>Description:</strong> " + responseData.requestDescription.Value + "</div>";
                    break;
                case 26:
                    emailText += "<div><br><strong>Description:</strong> " + responseData.initiativeDescription.Value + "</div>";
                    break;
                case 27:
                    var reqType = responseData.typeOfWork.Value;
                    switch (reqType)
                    {
                        case "Advice/consultancy/unsure":
                            emailText += "<div><br><strong>Let us know what you need:</strong>" + responseData.workDescription.Value + "</div>";
                            break;
                        case "Provision of DoT logos":
                            emailText += "<div><br><strong>Please provide details of where you will use the logo:</strong> " + responseData.logoUseDetails.Value + "</div>";
                            break;
                        case "Graphic design":
                            emailText += "<div><br><strong>What outcomes or business objectives do you want to achieve?</strong> " + responseData.achievedOutcome.Value + "</div>";
                            emailText += "<div><br><strong>Is there a hard deadline?</strong> " + responseData.gdIsHardDeadline.Value + "</div>";

                            if (responseData.gdIsHardDeadline.Value == "Yes")
                            {
                                emailText += "<div><br><strong>Deadline date </strong>" + responseData.gdHardDeadlineDate + "</div>";
                                emailText += "<div><br><strong>What materials are you requesting?</strong> " + responseData.requestingMaterial + "</div>";
                                emailText += "<div><br><strong>What are the sizes/specifications?</strong> " + responseData.sizesSpecifications + "</div>";
                            }

                            break;
                    }
                    break;
            }


            emailText += "<div><br><strong>Requestor:</strong> " + formOwnerFullName + "</div>"
                        + "<div><br><strong>Manager:</strong> " + managersName + "</div>"
                        + "<div><br><strong>Directorate:</strong> " + formOwnerDirectorate + "</div>";

            return emailText;
        }

        public async Task<string> GetOPRGroupEmail()
        {
            var result = await GetAdfGroupById(ConflictOfInterest.OPR_GROUP_ID);
            return result.GroupEmail;
        }



    }
}

