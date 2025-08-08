using DoT.Infrastructure;
using DoT.Infrastructure.DbModels;
using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure.Interfaces;
using eforms_middleware.Constants;
using eforms_middleware.Constants.COI;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static eforms_middleware.Settings.Helper;

namespace eforms_middleware.Workflows;

public class SecondaryEmploymentService : BaseApprovalService
{
    private readonly ILogger<SecondaryEmploymentService> _logger;

    public SecondaryEmploymentService(IFormEmailService formEmailService, IRepository<AdfGroup> adfGroup,
        IRepository<AdfPosition> adfPosition, IRepository<AdfUser> adfUser,
        IRepository<AdfGroupMember> adfGroupMember, IRepository<FormInfo> formInfoRepository,
        IRepository<RefFormStatus> formRefStatus, IRepository<FormHistory> formHistoryRepository,
        IPermissionManager permissionManager, IFormInfoService formInfoService, IConfiguration configuration,
        ITaskManager taskManager, IFormHistoryService formHistoryService,
        IRequestingUserProvider requestingUserProvider, IEmployeeService employeeService, IRepository<WorkflowBtn> workflowBtnRepository,
        ILogger<SecondaryEmploymentService> logger) : base(formEmailService,
        adfGroup, adfPosition, adfUser,
        adfGroupMember, formInfoRepository, formRefStatus, formHistoryRepository, permissionManager,
        formInfoService, configuration, taskManager, formHistoryService, requestingUserProvider, employeeService, workflowBtnRepository)
    {
        _logger = logger;
    }

    public async Task<RequestResult> SEApprovalProcess(FormInfoInsertModel formInfoInsertModel)
    {
        await SetupBaseApprovalService(formInfoInsertModel.FormDetails.FormInfoID);
        var secondaryData = JsonConvert.DeserializeObject<SecondaryEmploymentModel>(formInfoInsertModel.FormDetails.Response);
        var isRequestOnBehalf = secondaryData.IsRequestOnBehalf;

        if (isRequestOnBehalf)
        {
            var isUserPodGroupMember = await EmployeeService.IsPodGroupUser(FormOwner.ActiveDirectoryId);
            if (!isUserPodGroupMember)
            {
                return RequestResult.SuccessRequestWithErrorMessage(new
                {
                    Outcome = "Failed",
                    OutcomeValues = new
                    {
                        Error = "You must be a member of POD Business Admins to submit eForm on behalf of other employee."
                    }
                });
            }
        }

        var permissions = new List<FormPermission>();
        var executiveDirectorPeopleAndCulture =
            await EmployeeService.GetEmployeeByPositionNumberAsync(ConflictOfInterest
                .ED_PEOPLE_AND_CULTURE_POSITION_ID);
        var executiveDirectorOdg =
            await EmployeeService.GetEmployeeByPositionNumberAsync(ConflictOfInterest
                .ED_ODG_POSITION_ID);
        var formStatus = formInfoInsertModel.FormAction.GetParseEnum<FormStatus>();
        var usersToEmail = new List<IUserInfo>();
        var usersCcEmail = new List<IUserInfo>();
        var isFormOwnerEdODG = FormOwner.EmployeePositionId == ConflictOfInterest.ED_ODG_POSITION_ID;

        var emails = new List<NotificationInfo>();

        var request = new FormInfoUpdate
        { FormAction = formInfoInsertModel.FormAction, FormDetails = formInfoInsertModel.ToFormDetailsRequest(), UserId = "" };
        var dbForm = await FormInfoService.GetExistingOrNewFormInfoAsync(request);

        dbForm.FormStatusId = (int)formStatus;
        dbForm.FormSubStatus = formStatus.ToString();

        var isFormOwnerReportToT1 = false;
        if (dbForm.FormOwnerEmail != null)
        {
            var formOwmer = await EmployeeService.GetEmployeeDetailsAsync(dbForm.FormOwnerEmail);
            isFormOwnerReportToT1 = formOwmer.Managers.Any(x => x.EmployeeManagementTier is 1);
        }
        switch (formStatus)
        {
            case FormStatus.Unsubmitted:
                dbForm.Response = JsonConvert.SerializeObject(secondaryData);
                dbForm.FormApprovers = "";
                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, userId: FormOwner.ActiveDirectoryId, isOwner: true));
                permissions.Add(new FormPermission((byte)PermissionFlag.View, groupId: ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID));
                break;
            case FormStatus.Submitted:
                secondaryData.ReasonForDecision = string.Empty;
                dbForm.Response = JsonConvert.SerializeObject(secondaryData);
                emails.Add(new NotificationInfo(FormStatus.Requestor, toEmail: FormOwner.EmployeeEmail));
                permissions.Add(new FormPermission((byte)PermissionFlag.View, userId: FormOwner.ActiveDirectoryId, isOwner: true));
                dbForm.SubmittedDate = DateTime.Now;
                if (FormOwnerReportToT1)
                {
                    var edOdgUserList = await EmployeeService
                                               .GetEmployeeByPositionNumberAsync(positionNumber: ConflictOfInterest.ED_ODG_POSITION_ID);

                    dbForm.FormSubStatus = FormStatus.SubmittedAndEndorsed.GetFormStatusTitle();
                    permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable,
                        positionId: !isFormOwnerEdODG ? ConflictOfInterest.ED_ODG_POSITION_ID : null,
                        groupId: isFormOwnerEdODG ? ConflictOfInterest.ODG_AUDIT_GROUP_ID : null));
                    dbForm.NextApprover = isFormOwnerEdODG ? ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL : edOdgUserList.GetDescription();
                    dbForm.NextApprovalLevel = isFormOwnerEdODG ? EmployeeTitle.GovAuditGroup.GetEmployeeTitle() : EmployeeTitle.ExecutiveDirectorODG.ToString();
                    usersToEmail = edOdgUserList.ToList();
                    emails.Add(new NotificationInfo("Requester", toEmail: edOdgUserList.FirstOrDefault()?.EmployeeEmail));
                }
                else
                {
                    if (!isRequestOnBehalf)
                    {
                        if (FormOwner.ManagerPositionId is ConflictOfInterest.DIRECTOR_GENERAL_POSITION_NUMBER)
                        {
                            dbForm.FormSubStatus = FormStatus.Escalated.ToString();
                            permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, groupId: ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID));
                            emails.Add(new NotificationInfo(formStatus, toEmail: ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_EMAIL));
                            usersToEmail.Add(new EmployeeDetailsDto { EmployeeEmail = ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_EMAIL });
                            dbForm.NextApprover = ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_NAME;
                            dbForm.NextApprovalLevel = ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_NAME;
                        }
                        else if (FormOwner.ManagerPositionId is ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID ||
                             FormOwner.EmployeePositionId is ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID)
                        {
                            dbForm.FormSubStatus = "Submitted and Endorsed";
                            if (!FormOwner.Managers.Any()) goto case FormStatus.AssignToBackup;
                            usersToEmail.AddRange(FormOwner.Managers);
                            permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: FormOwner.ManagerPositionId));
                            emails.Add(new NotificationInfo(FormStatus.Submitted));
                            dbForm.NextApprover = FormOwner.ManagerIdentifier;
                        }
                        else if (FormOwner.EmployeeManagementTier is > 1 && !FormOwner.Managers.Any())
                        {
                            dbForm.FormSubStatus = $"Submitted and {FormStatus.Escalated.ToString()}";
                            if (!executiveDirectorPeopleAndCulture.Any()) goto case FormStatus.AssignToBackup;
                            usersToEmail.AddRange(executiveDirectorPeopleAndCulture);
                            permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: FormOwner.ExecutiveDirectorPositionId));
                            dbForm.NextApprover = FormOwner.ExecutiveDirectorIdentifier;
                            emails.Add(new NotificationInfo(FormStatus.Escalated, toEmail: string.Join(';', FormOwner.ExecutiveDirectors.Select(x => x.EmployeeEmail))));
                        }
                        else if (FormOwner.EmployeeManagementTier is 1)
                        {
                            dbForm.FormSubStatus = "Submitted and Endorsed";
                            if (!executiveDirectorOdg.Any()) goto case FormStatus.AssignToBackup;
                            usersToEmail.AddRange(executiveDirectorOdg);
                            permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: ConflictOfInterest
                                .ED_ODG_POSITION_ID));
                            emails.Add(new NotificationInfo(FormStatus.Submitted));
                            dbForm.NextApprover = executiveDirectorOdg.GetDescription();
                        }
                        else
                        {
                            if (!FormOwner.Managers.Any()) goto case FormStatus.AssignToBackup;
                            permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: FormOwner.ManagerPositionId));
                            usersToEmail = FormOwner.Managers.ToList();
                            emails.Add(new NotificationInfo(FormStatus.Submitted));
                            dbForm.NextApprover = FormOwner.ManagerIdentifier;
                        }
                    }
                    else
                    {
                        dbForm.FormSubStatus = "Submitted and Endorsed";
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable,
                            positionId: ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID));
                        var EdPcUserList = await EmployeeService.GetEmployeeByPositionNumberAsync(ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID);
                        usersToEmail.AddRange(EdPcUserList);
                        emails.Add(new NotificationInfo(FormStatus.Submitted));
                        dbForm.NextApprover = EdPcUserList.GetDescription();
                    }
                }
                break;
            case FormStatus.Rejected:
                var originalResponse = JsonConvert.DeserializeObject<SecondaryEmploymentModel>(dbForm.Response);
                originalResponse.ReasonForDecision = secondaryData.ReasonForDecision;
                dbForm.Response = JsonConvert.SerializeObject(originalResponse);
                goto case FormStatus.Recall;
            case FormStatus.Recall:
                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, userId: FormOwner.ActiveDirectoryId, isOwner: true));
                dbForm.FormSubStatus = formStatus == FormStatus.Recall ? FormStatus.Recalled.ToString() : formStatus.ToString();
                emails.Add(new NotificationInfo(formStatus));
                dbForm.FormApprovers = "";
                dbForm.NextApprover = null;
                dbForm.FormStatusId = (int)FormStatus.Unsubmitted;
                break;
            case FormStatus.Endorsed:
                var employeeDetail = await EmployeeService.GetEmployeeByEmailAsync(RequestingUser.EmployeeEmail);
                usersCcEmail = employeeDetail.Managers.ToList();
                if (CurrentApprovers.FirstOrDefault()?.PositionId is ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID)
                {
                    dbForm.FormSubStatus = "Approved and Completed";
                    dbForm.FormStatusId = (int)FormStatus.Completed;
                    emails.Add(new NotificationInfo(FormStatus.Completed));
                }
                else if (CurrentApprovers.FirstOrDefault()?.Position.ManagementTier is <= 3)
                {
                    dbForm.FormSubStatus = FormStatus.Approved.ToString();
                    dbForm.FormStatusId = (int)FormStatus.Approved;
                    if (!executiveDirectorPeopleAndCulture.Any()) goto case FormStatus.AssignToBackup;
                    permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID));
                    emails.Add(new NotificationInfo(FormStatus.Approved));
                    usersToEmail = executiveDirectorPeopleAndCulture.ToList();
                    dbForm.NextApprover = executiveDirectorPeopleAndCulture.GetDescription();
                }
                else
                {
                    var hasExecutiveDirector = CurrentApprovers.Single().Position.AdfUserPositions.Any();
                    formStatus = !hasExecutiveDirector ? FormStatus.Escalated : formStatus;
                    dbForm.FormStatusId = (int)formStatus;
                    dbForm.FormSubStatus = formStatus.ToString();
                    if (!hasExecutiveDirector) goto case FormStatus.AssignToBackup;
                    permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: FormOwner.ExecutiveDirectorPositionId));
                    emails.Add(new NotificationInfo(formStatus));
                    usersToEmail = FormOwner.ExecutiveDirectors.ToList();
                    dbForm.NextApprover = FormOwner.ExecutiveDirectorIdentifier;
                }
                break;
            case FormStatus.Approved:
                if (CurrentApprovers.FirstOrDefault()?.PositionId == ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID)
                {
                    dbForm.NextApprover = null;
                    dbForm.FormSubStatus = "Approved and Completed";
                    dbForm.FormStatusId = (int)FormStatus.Completed;
                    emails.Add(new NotificationInfo(FormStatus.Completed));
                }
                else
                {
                    if (!executiveDirectorPeopleAndCulture.Any()) goto case FormStatus.AssignToBackup;
                    permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID));
                    dbForm.NextApprover = executiveDirectorPeopleAndCulture.GetDescription();
                    emails.Add(new NotificationInfo(formStatus));
                    usersToEmail = executiveDirectorPeopleAndCulture.ToList();
                }
                break;
            case FormStatus.Completed:
                dbForm.FormSubStatus = "Approved and Completed";
                dbForm.NextApprover = null;
                emails.Add(new NotificationInfo(formStatus));
                break;
            case FormStatus.Delegated:
                var delegatedUser =
                    await EmployeeService.GetEmployeeByAzureIdAsync(new Guid(formInfoInsertModel.FormDetails.NextApprover));
                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, userId: delegatedUser.ActiveDirectoryId));
                emails.Add(new NotificationInfo(formStatus));
                dbForm.NextApprover = delegatedUser.EmployeeEmail;
                dbForm.FormSubStatus = FormStatus.Delegated.ToString();
                dbForm.FormStatusId = (int)FormStatus.Delegated;
                usersToEmail.Add(delegatedUser);
                break;
            case FormStatus.AssignToBackup:
                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, groupId: ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID));
                emails.Add(new NotificationInfo(formStatus, toEmail: ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_EMAIL));
                usersToEmail.Add(new EmployeeDetailsDto { EmployeeEmail = ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_EMAIL });
                dbForm.NextApprover = ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_NAME;
                break;
        }

        if (dbForm.FormStatusId != (int)FormStatus.Unsubmitted)
        {
            formInfoInsertModel.FormDetails.FormApprovers += RequestingUser.EmployeeEmail + ";";
        }

        try
        {
            var formInfo = await FormInfoService.SaveFormInfoAsync(request, dbForm);
            await UpdateTasksAsync(formInfo);
            await _permissionManager.UpdateFormPermissionsAsync(formInfo.FormInfoId, permissions);
            await UpdateHistoryAsync(request, formInfo, ActioningGroup?.Id, formInfoInsertModel.AdditionalInfo, secondaryData.ReasonForDecision);
            foreach (var email in emails)
            {
                await SendEmailAsync(formInfo, formInfoInsertModel, email.EmailType, usersToEmail, email.ToGuid, usersCcEmail, isFormOwnerEdODG);
            }
            return RequestResult.SuccessfulFormInfoRequest(formInfo);
        }
        catch (Exception e)
        {
            return RequestResult.FailedRequest(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    private string GetAllEmailsForRecall()
    {
        var emails = new HashSet<string>();
        foreach (var permission in Permissions)
        {
            if (permission.UserId.HasValue && !permission.IsOwner)
            {
                emails.Add(permission.User.EmployeeEmail);
            }

            if (permission.PositionId.HasValue)
            {
                foreach (var positionUser in permission.Position.AdfUserPositions)
                {
                    if (!string.Equals(positionUser.EmployeeEmail, FormOwner.EmployeeEmail, StringComparison.InvariantCultureIgnoreCase))
                    {
                        emails.Add(positionUser.EmployeeEmail);
                    }
                }
            }

            if (permission.GroupId.HasValue)
            {
                foreach (var groupMember in permission.Group.AdfGroupMembers)
                {
                    if (!string.Equals(groupMember.Member.EmployeeEmail, FormOwner.EmployeeEmail, StringComparison.InvariantCultureIgnoreCase))
                    {
                        emails.Add(groupMember.Member.EmployeeEmail);
                    }
                }
            }
        }

        return string.Join(';', emails);
    }

    private async Task SendEmailAsync(FormInfo dbForm
        , FormInfoInsertModel formInfoInsertModel
        , FormStatus emailType
        , IList<IUserInfo> usersToEmail
        , Guid? toUser = null
        , IList<IUserInfo> usersCcEmail = null
        , bool IsRequestorEdODG = false)
    {
        try
        {
            var toEmail = string.Empty;
            var ccEmail = string.Empty;
            var reqUrl = $"{BaseUrl}/conflict-of-interest/summary/{dbForm.FormInfoId}";
            var editUrl = $"{BaseUrl}/conflict-of-interest/{dbForm.FormInfoId}";
            var SummaryHref = $"<a href={reqUrl}>click here</a>";
            var editHref = $"<a href={editUrl}>click here</a>";

            var subject = "Secondary Employment request ";
            var body = "";
            var secondaryData = JsonConvert.DeserializeObject<SecondaryEmploymentModel>(formInfoInsertModel.FormDetails.Response);
            var emailsToSend = new List<Tuple<string, string, string, string>>();
            switch (emailType)
            {
                case FormStatus.Requestor:
                    if (!FormOwnerReportToT1)
                    {
                        toEmail = FormOwner.EmployeeEmail;
                        subject += "received";
                        body = $"<div>Dear {FormOwner.EmployeePreferredFullName}</div>" +
                               $"<br/><div>I am writing to inform you that your Conflict of interest declaration form regarding your Secondary Employment has been received.</div>" +
                               $"<br/><div>The following details of your request have been recorded:</div>" +
                               $"<br/><div>Reference Number: {dbForm.FormInfoId}</div>" +
                               "<br/><div>Once your application has been assessed you will receive further email communications. If you have any further questions please contact the Employee Services Branch on (08) 6551 6888, or via email employeeservices@transport.wa.gov.au.</div>";
                        emailsToSend.Add(new Tuple<string, string, string, string>(toEmail, null, subject, body));
                    }
                    else
                    {
                        subject = $"Secondary Employment request - Conflict of interest declaration - for approval";
                        toEmail = IsRequestorEdODG ? ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL : usersToEmail.FirstOrDefault()?.EmployeeEmail;
                        ccEmail = FormOwner.EmployeeEmail;
                        body = string.Format(CoITemplates.SUBMITTED_DG_TEMPLATE, FormOwner.EmployeePreferredFullName, SummaryHref);
                        emailsToSend.Add(new Tuple<string, string, string, string>(toEmail, FormOwner.EmployeeEmail, subject, body));
                    }
                    break;
                case FormStatus.Rejected:
                    toEmail = FormOwner.EmployeeEmail;
                    subject += FormOwner.ManagerPositionId == RequestingUser.EmployeePositionId ? "not endorsed" : "has been rejected";
                    body = $"<div>Dear {FormOwner.EmployeePreferredFullName}</div>" +
                           $"<br/><div>Your declaration for Secondary Employment has now been assessed and the outcome of your request has not been supported. You can view your form <a href=\"{editUrl}\">here</a>.</div>" +
                           $"<br/><div>The following reasons were given by {RequestingUser.EmployeePreferredFullName}: {secondaryData.ReasonForDecision}</div>" +
                           $"<br/><div>Should you require any additional information in relation to this matter, please contact the Employee Services Branch on (08) 6551 6888, or via email employeeservices@transport.wa.gov.au to discuss further.</div>";
                    emailsToSend.Add(new Tuple<string, string, string, string>(toEmail, null, subject, body));
                    break;
                case FormStatus.Recall:
                    var emailsToInform = GetAllEmailsForRecall();
                    toEmail = FormOwner.EmployeeEmail;
                    ccEmail = emailsToInform;
                    subject += "Conflict of Interest declaration recalled";
                    body += $"<div>Hi {FormOwner.EmployeePreferredFullName}</div>" +
                            $"<br/><div>The conflict of interest request {dbForm.FormInfoId} has been recalled by {FormOwner.EmployeePreferredFullName} and does not require immediate action.</div>" +
                            $"<br/><div>{FormOwner.EmployeePreferredFullName}, if you would like to re-submit your eForm, please <a href=\"{reqUrl}\">click here</a> to review the declaration. </div>" +
                            $"<br/><div>Should you require any additional information in relation to this matter, please contact the Employee Services Branch on (08) 6551 6888, or via email employeeservices@transport.wa.gov.au to discuss further.</div>" +
                            $"<br/><div>Thank you</div>";
                    emailsToSend.Add(new Tuple<string, string, string, string>(toEmail, ccEmail, subject, body));
                    break;
                case FormStatus.Endorsed:
                case FormStatus.Approved:
                case FormStatus.Submitted:
                    subject += "for your review";

                    if (usersCcEmail != null && usersCcEmail.Any())
                    {
                        ccEmail = string.Join(",", usersCcEmail.Select(x => x.EmployeeEmail));
                    }

                    foreach (var userInfo in usersToEmail)
                    {
                        toEmail = userInfo.EmployeeEmail;
                        body = $"<div>Dear {userInfo.EmployeePreferredFullName}</div>" +
                               $"<br/><div>A conflict of interest declaration regarding secondary employment has been submitted by {FormOwner.EmployeePreferredFullName}.</div>" +
                               $"<br/><div>Please <a href=\"{reqUrl}\">click here</a> to review the declaration.</div>" +
                               "<br/><div>Once your application has been assessed you will receive further email communications. If you have any further questions please contact the Employee Services Branch on (08) 6551 6888, or via email employeeservices@transport.wa.gov.au.</div>";
                        emailsToSend.Add(new Tuple<string, string, string, string>(toEmail, ccEmail, subject, body));
                    }
                    break;
                case FormStatus.Completed:
                    if (!FormOwnerReportToT1)
                    {
                        subject += "supported";
                        toEmail = FormOwner.EmployeeEmail;
                        ccEmail = string.Join(';', FormOwner.Managers.Select(x => x.EmployeeEmail));
                        body = $"<div>Dear {FormOwner.EmployeePreferredFullName}</div>" +
                               $"<br/><div>Your declaration has now been assessed and the outcome of your request has been supported by senior management.</div>" +
                               $"<br/><div>Should you require any additional information in relation to this matter, please contact the Employee Services Branch on (08) 6551 6888, or via email employeeservices@transport.wa.gov.au</div>";
                        emailsToSend.Add(new Tuple<string, string, string, string>(toEmail, ccEmail, subject, body));
                    }
                    else
                    {
                        subject = $"Secondary Employment request - Conflict of interest declaration outcome";
                        body = string.Format(CoITemplates.APPROVED_DG_TEMPLATE, FormOwner.EmployeePreferredFullName, SummaryHref);
                        toEmail = FormOwner.EmployeeEmail;
                        ccEmail = ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL;
                        if (!IsRequestorEdODG)
                        {
                            var eDOdgUserList = await EmployeeService.GetEmployeeByPositionNumberAsync(ConflictOfInterest.ED_ODG_POSITION_ID);
                            ccEmail = eDOdgUserList.GetDescription();
                        }
                        emailsToSend.Add(new Tuple<string, string, string, string>(toEmail, ccEmail, subject, body));
                    }
                    break;
                case FormStatus.Escalated:
                case FormStatus.Delegated:
                    foreach (var userInfo in usersToEmail)
                    {
                        subject += "delegated";
                        body = $"<div>Dear {userInfo.EmployeePreferredFullName} </div>" +
                               $"<br/><div>A conflict of interest declaration regarding secondary employment has been submitted by {FormOwner.EmployeePreferredFullName}. This task has escalated to you for review due to one or more of the reasons below.</div>" +
                               $"<br/><div><ul>" +
                               $"<li>Tier 3's position is currently vacant.</li>" +
                               $"<li>Both Requester’s line manager's and Tier 3's positions are currently vacant.</li>" +
                               $"<li>Requester’s line manager did not action the form in the required timeframe and the form escalated to Tier 3, whose position is currently vacant.</li>" +
                               $"</ul></div>" +
                               $"<br/><div>Please <a href=\"{reqUrl}\">click here</a> to review the declaration.</div>" +
                               $"<br/><div>By completing this form, you will be performing the final approval for {FormOwner.EmployeePreferredFullName}'s secondary employment request. You can also reject the form and send it back to {FormOwner.EmployeePreferredFullName} for re-submission at a more appropriate time.</div>" +
                               $"<br/><div>If you have any further questions please contact the Employee Services Branch on (08) 6551 6888, or via email employeeservices@transport.wa.gov.au.</div>";
                        emailsToSend.Add(new Tuple<string, string, string, string>(userInfo.EmployeeEmail, null, subject, body));
                    }

                    break;
            }

            foreach (var emailParams in emailsToSend)
            {
                await _formEmailService.SendEmail(dbForm.FormInfoId, emailParams.Item1, emailParams.Item3,
                     emailParams.Item4, emailParams.Item2);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error thrown from {nameof(SecondaryEmploymentService)}");
        }
    }

}