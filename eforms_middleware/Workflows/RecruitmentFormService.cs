using DoT.Infrastructure;
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

public class RecruitmentFormService : BaseApprovalService
{
    private readonly ILogger<RecruitmentFormService> _logger;
    public IList<IUserInfo> ExecutiveDirectorPeopleAndCulture { get; set; }

    public RecruitmentFormService(IFormEmailService formEmailService, IRepository<AdfGroup> adfGroup,
        IRepository<AdfPosition> adfPosition, IRepository<AdfUser> adfUser,
        IRepository<AdfGroupMember> adfGroupMember, IRepository<FormInfo> formInfoRepository,
        IRepository<RefFormStatus> formRefStatus, IRepository<FormHistory> formHistoryRepository,
        IPermissionManager permissionManager, IFormInfoService formInfoService, IConfiguration configuration,
        ITaskManager taskManager, IFormHistoryService formHistoryService, IRepository<WorkflowBtn> workflowBtnRepository,
        IRequestingUserProvider requestingUserProvider, IEmployeeService employeeService, ILogger<RecruitmentFormService> logger)
        : base(formEmailService, adfGroup, adfPosition, adfUser,
            adfGroupMember, formInfoRepository, formRefStatus, formHistoryRepository, permissionManager, formInfoService,
            configuration, taskManager, formHistoryService, requestingUserProvider, employeeService, workflowBtnRepository)
    {
        _logger = logger;
    }

    public async Task<RequestResult> RecruitmentApprovalProcess(FormInfoInsertModel formInfoInsertModel)
    {
        await SetupBaseApprovalService(formInfoInsertModel.FormDetails.FormInfoID);
        var permissions = new List<FormPermission>();
        var recruitmentModel = JsonConvert.DeserializeObject<RecruitmentModel>(formInfoInsertModel.FormDetails.Response);
        var isRequestOnBehalf = recruitmentModel.IsRequestOnBehalf;
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

        ExecutiveDirectorPeopleAndCulture = await GetEDPeopleAndCulture();
        var formStatus = formInfoInsertModel.FormAction.GetParseEnum<FormStatus>();
        var formSubStatus = formInfoInsertModel.FormAction;
        var emailType = string.Empty;
        var emails = new List<NotificationInfo>();
        var setAdhocPemission = false;
        var request = new FormInfoUpdate
        { FormAction = formInfoInsertModel.FormAction, FormDetails = formInfoInsertModel.ToFormDetailsRequest(), UserId = "" };
        var dbForm = await FormInfoService.GetExistingOrNewFormInfoAsync(request);
        dbForm.FormStatusId = (int)formStatus;
        dbForm.FormSubStatus = formStatus.ToString();
        var isFormOwnerEdODG = FormOwner.EmployeePositionId == ConflictOfInterest.ED_ODG_POSITION_ID;
        switch (formStatus)
        {
            case FormStatus.Unsubmitted:
                dbForm.Response = JsonConvert.SerializeObject(recruitmentModel);
                dbForm.FormApprovers = "";
                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, userId: FormOwner.ActiveDirectoryId, isOwner: true));
                break;
            case FormStatus.Rejected:
                var originalResponse = JsonConvert.DeserializeObject<RecruitmentModel>(dbForm.Response);
                originalResponse.ReasonForDecision = recruitmentModel.ReasonForDecision;
                dbForm.Response = JsonConvert.SerializeObject(originalResponse);
                goto case FormStatus.Recall;
            case FormStatus.Recall:
                dbForm.FormApprovers = "";
                dbForm.FormSubStatus = formStatus == FormStatus.Rejected ? "Not Endorsed" : formStatus.ToString();
                dbForm.FormStatusId = (int)FormStatus.Unsubmitted;
                dbForm.NextApprover = null;
                emailType = formStatus.ToString();
                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, userId: FormOwner.ActiveDirectoryId, isOwner: true));
                break;
            case FormStatus.Delegated:
                var userId = Guid.Parse(formInfoInsertModel.FormDetails.NextApprover);
                var manualNextApprover =
                    await EmployeeService.GetEmployeeByAzureIdAsync(userId);
                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, userId: manualNextApprover.ActiveDirectoryId));
                emails.Add(new NotificationInfo(formStatus, toUser: manualNextApprover));
                var nextApprovalLevel = Enum.Parse<EmployeeTitle>(formStatus.ToString());
                dbForm.FormStatusId = (int)FormStatus.Delegated;
                dbForm.NextApprover = manualNextApprover.EmployeeEmail;
                dbForm.NextApprovalLevel = nextApprovalLevel.GetEmployeeTitle();
                dbForm.FormSubStatus = FormStatus.Delegated.GetFormStatusTitle();
                emails.Add(new NotificationInfo("Panel Member", toGuid: userId));
                break;
            case FormStatus.Submitted:
                recruitmentModel.ReasonForDecision = string.Empty;
                dbForm.Response = JsonConvert.SerializeObject(recruitmentModel);
                dbForm.SubmittedDate = DateTime.Now;
                permissions.Add(new FormPermission((byte)PermissionFlag.View, userId: FormOwner.ActiveDirectoryId, isOwner: true));
                permissions.Add(new FormPermission((byte)PermissionFlag.View, groupId: ConflictOfInterest.TALENT_TEAM_GROUP_ID));

                if (recruitmentModel.HasConflictOfInterest == "No")
                {
                    dbForm.FormSubStatus = FormStatus.Completed.ToString();
                    dbForm.FormStatusId = (int)FormStatus.Completed;
                    dbForm.NextApprover = string.Empty;
                    dbForm.NextApprovalLevel = string.Empty;

                    emails.Add(new NotificationInfo("Requester", null));
                    setAdhocPemission = true;
                }
                else
                {
                    if (FormOwnerReportToT1 && isFormOwnerEdODG)
                    {
                        dbForm.FormSubStatus = FormStatus.SubmittedAndEndorsed.GetFormStatusTitle();
                        var edOdgUserList = await EmployeeService
                                                  .GetEmployeeByPositionNumberAsync(positionNumber: ConflictOfInterest.ED_ODG_POSITION_ID);
                        permissions.Add(new FormPermission
                        {
                            PermissionFlag = (byte)PermissionFlag.UserActionable,
                            GroupId = isFormOwnerEdODG ? ConflictOfInterest.ODG_AUDIT_GROUP_ID : null,
                            PositionId = !isFormOwnerEdODG ? ConflictOfInterest.ED_ODG_POSITION_ID : null
                        });
                        emails.Add(new NotificationInfo("Requester", toUser: edOdgUserList.FirstOrDefault()));
                        dbForm.NextApprover = isFormOwnerEdODG ? ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL : edOdgUserList.GetDescription();
                        dbForm.NextApprovalLevel = isFormOwnerEdODG ? EmployeeTitle.GovAuditGroup.GetEmployeeTitle() : EmployeeTitle.ExecutiveDirectorODG.ToString();

                    }
                    else
                    {
                        if (!isRequestOnBehalf)
                        {
                            UserIdentifier nextUserIdentifier;
                            if (recruitmentModel.PanelChair.ActiveDirectoryId == FormOwner.ActiveDirectoryId)
                            {
                                nextUserIdentifier = recruitmentModel.PanelMembers[0];
                                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, userId: nextUserIdentifier.ActiveDirectoryId));
                                dbForm.NextApprover = recruitmentModel.PanelMembers[0].EmployeeEmail;
                                dbForm.NextApprovalLevel = "Panel Member 1";
                                emails.Add(new NotificationInfo("Panel Member", toGuid: nextUserIdentifier.ActiveDirectoryId));
                            }
                            else
                            {
                                nextUserIdentifier = recruitmentModel.PanelChair;
                                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, userId: nextUserIdentifier.ActiveDirectoryId));
                                dbForm.NextApprover = recruitmentModel.PanelChair.EmployeeEmail;
                                dbForm.NextApprovalLevel = "Panel Chair";
                                emails.Add(new NotificationInfo("Panel Chair", toGuid: nextUserIdentifier.ActiveDirectoryId));
                            }

                            if (recruitmentModel.HasConflictOfInterest == "External")
                            {
                                if (recruitmentModel.HasExternalConflict == "No")
                                {
                                    dbForm.FormSubStatus = FormStatus.Completed.ToString();
                                    dbForm.FormStatusId = (int)FormStatus.Completed;
                                    emails.Add(new NotificationInfo("Requester External", recruitmentModel.SelectedExternalMember));
                                    dbForm.NextApprover = string.Empty;
                                    dbForm.NextApprovalLevel = string.Empty;
                                    emailType = string.Empty;
                                }
                                else
                                {
                                    emails.Add(new NotificationInfo("External", recruitmentModel.SelectedExternalMember));
                                }
                            }
                        }
                        else
                        {
                            dbForm.FormSubStatus = "Submitted and Endorsed";
                            permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable,
                                positionId: ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID));
                            var EdPcUserList = await EmployeeService.GetEmployeeByPositionNumberAsync(ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID);
                            foreach (var EdPcUser in EdPcUserList)
                            {
                                emails.Add(new NotificationInfo("RequestOnBehalf", EdPcUser));
                            }
                            dbForm.NextApprover = EdPcUserList.GetDescription();
                        }
                    }
                }
                break;
            case FormStatus.Endorsed:
                var finalPanelMember = recruitmentModel.PanelMembers.Length > 0 ? recruitmentModel.PanelMembers[^1] : null;
                var IsTalentGroupEndorsed = ActioningGroup != null
                                            && ActioningGroup.Id == ConflictOfInterest.TALENT_TEAM_GROUP_ID;

                if (IsTalentGroupEndorsed)
                {
                    if (FormOwnerReportToT1 && !isFormOwnerEdODG)
                    {

                        permissions.Add(new FormPermission
                        {
                            PermissionFlag = (byte)PermissionFlag.UserActionable,
                            PositionId = ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID,
                        });
                        dbForm.NextApprover = ExecutiveDirectorPeopleAndCulture.GetDescription();
                        dbForm.NextApprovalLevel = ExecutiveDirectorPeopleAndCulture.FirstOrDefault()?.EmployeePositionTitle;
                        emails.Add(new NotificationInfo("Ready For Approval", toUser: ExecutiveDirectorPeopleAndCulture.FirstOrDefault()));
                    }
                    else
                    {
                        if (FormOwner.EmployeeManagementTier is <= 3 and not 1) // Edge case flow Any user who is director level or above except the DG
                        {
                            FormOwner = await EmployeeService.GetEmployeeByAzureIdAsync(FormOwner.ActiveDirectoryId);
                            dbForm.NextApprover = FormOwner.ManagerIdentifier;
                            permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: FormOwner.ManagerPositionId));
                            dbForm.NextApprovalLevel = FormOwner.Managers.FirstOrDefault()!.EmployeePositionTitle;
                        }
                        else // Normal flow here
                        {
                            dbForm.NextApprover = ExecutiveDirectorPeopleAndCulture.GetDescription();
                            permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID));
                            dbForm.NextApprovalLevel = ExecutiveDirectorPeopleAndCulture.FirstOrDefault()?.EmployeePositionTitle;
                        }
                        dbForm.NextApprovalLevel += " Final";
                        emailType = "Ready For Approval";
                    }
                }
                else if (finalPanelMember != null && CurrentApprovers.Any(x => x.UserId == finalPanelMember.ActiveDirectoryId))
                {
                    dbForm.NextApprover = await GetTalentTeamGroup();
                    permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, groupId: ConflictOfInterest.TALENT_TEAM_GROUP_ID));
                    dbForm.NextApprovalLevel = ConflictOfInterest.TALENT_TEAM_GROUP_NAME;
                    emailType = ConflictOfInterest.TALENT_TEAM_GROUP_NAME;
                }
                else
                {
                    var memberNumber = formInfoInsertModel.FormDetails.NextApprovalLevel.GetNumberFromString();

                    if (recruitmentModel.PanelMembers[memberNumber].ActiveDirectoryId != FormOwner.ActiveDirectoryId)
                    {
                        emails.Add(new NotificationInfo("Panel Member", toGuid: recruitmentModel.PanelMembers[memberNumber].ActiveDirectoryId));
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, userId: recruitmentModel.PanelMembers[memberNumber].ActiveDirectoryId));
                    }
                    else if (recruitmentModel.PanelMembers.Length == memberNumber + 1)
                    {
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, groupId: ConflictOfInterest.TALENT_TEAM_GROUP_ID));
                        emailType = ConflictOfInterest.TALENT_TEAM_GROUP_NAME;
                    }
                    else
                    {
                        emails.Add(new NotificationInfo("Panel Member", toGuid: recruitmentModel.PanelMembers[memberNumber + 1].ActiveDirectoryId));
                        permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, userId: recruitmentModel.PanelMembers[memberNumber + 1].ActiveDirectoryId));
                    }
                    var panelMember = recruitmentModel.PanelMembers[memberNumber].ActiveDirectoryId == FormOwner.ActiveDirectoryId
                        ? recruitmentModel.PanelMembers.Length == memberNumber + 1
                            ? await GetTalentTeamGroup()
                            : recruitmentModel.PanelMembers[memberNumber + 1].EmployeeEmail
                        : recruitmentModel.PanelMembers[memberNumber].EmployeeEmail;

                    var panelMemberText = recruitmentModel.PanelMembers[memberNumber].ActiveDirectoryId == FormOwner.ActiveDirectoryId
                        ? recruitmentModel.PanelMembers.Length == memberNumber + 1
                            ? ConflictOfInterest.TALENT_TEAM_GROUP_NAME
                            : $"Panel Member {memberNumber + 2}"
                        : $"Panel Member {memberNumber + 1}";
                    dbForm.NextApprover = panelMember;
                    dbForm.NextApprovalLevel = panelMemberText;
                }
                break;
            case FormStatus.Approved:
                emailType = formSubStatus;
                dbForm.FormSubStatus = FormStatus.Completed.ToString();
                dbForm.FormStatusId = (int)FormStatus.Completed;
                dbForm.CompletedDate = DateTime.Now;
                dbForm.NextApprover = string.Empty;
                dbForm.NextApprovalLevel = string.Empty;
                break;
        }

        if (!string.IsNullOrWhiteSpace(emailType))
        {
            emails.Add(new NotificationInfo(emailType, null));
        }

        if (dbForm.FormStatusId != (int)FormStatus.Unsubmitted)
        {
            formInfoInsertModel.FormDetails.FormApprovers += RequestingUser.EmployeeEmail + ";";
        }

        try
        {
            var formInfo = await FormInfoService.SaveFormInfoAsync(request, dbForm);
            await UpdateTasksAsync(formInfo, true, 2);
            await _permissionManager.UpdateFormPermissionsAsync(formInfo.FormInfoId, permissions);
            await UpdateHistoryAsync(request, formInfo, ActioningGroup?.Id, formInfoInsertModel.AdditionalInfo, recruitmentModel.ReasonForDecision);
            foreach (var email in emails)
            {
                await SendRecruitmentEmail(formInfo, formInfoInsertModel, recruitmentModel,
                    email.Type, email.ToEmail, isFormOwnerEdODG, email.ToGuid, email.ToUser);
            }
            if (setAdhocPemission)
            {
                await SetAdHocPermission(formInfo.FormInfoId, ConflictOfInterest.TALENT_TEAM_GROUP_ID);
            }
            return RequestResult.SuccessfulFormInfoRequest(formInfo);
        }
        catch (Exception e)
        {
            return RequestResult.FailedRequest(StatusCodes.Status500InternalServerError, e.Message);
        }


    }

    private string GetRequesterEmailIfRequired(RecruitmentModel recruitmentModel)
    {
        var result = string.Empty;
        if (RequestingUser.ActiveDirectoryId != recruitmentModel.PanelChair.ActiveDirectoryId
            && recruitmentModel.PanelMembers.All(x => x.ActiveDirectoryId != RequestingUser.ActiveDirectoryId))
        {
            result = RequestingUser.EmployeeEmail + ";";
        }
        return result;
    }

    private async Task SendRecruitmentEmail(
        FormInfo dbRecord
        , FormInfoInsertModel formInfo
        , RecruitmentModel recruitmentModel
        , string type
        , string toEmail = ""
        , bool IsFormOwnerEdODG = false
        , Guid? toGuid = null
        , IUserInfo userInfo = null)
    {
        try
        {
            var emailSubject = $"Recruitment Process {dbRecord.FormInfoId} - {recruitmentModel.PositionTitle}";
            var emailBody = string.Empty;
            const string emailFooter = $"<br/><div>Thank You</div>" +
                                       $"<br/><div>The Recruitment Team</div>";
            var formUrl = $"{BaseUrl}/conflict-of-interest/summary/{dbRecord.FormInfoId}";
            var clickHere = $"<a href=\"{formUrl}\">Click Here</a>";

            IUserInfo employeeData;
            var externalEmailList = recruitmentModel.ExternalPanelMember.Any() ? string.Join(";", recruitmentModel.ExternalPanelMember.Select(x => x.Email)) + ";" : "";
            var requester = GetRequesterEmailIfRequired(recruitmentModel);
            var emailsToSend = new List<Tuple<string, string, string, string, string>>();

            var hasNoConflict = recruitmentModel.HasConflictOfInterest.Equals("No");

            switch (type)
            {
                case "Requester":
                    if (!FormOwnerReportToT1 || hasNoConflict)
                    {
                        var externalEmail = recruitmentModel.ExternalPanelMember.Any() ? string.Join(";", recruitmentModel.ExternalPanelMember.Select(x => x.Email)) + ";" : "";
                        emailBody = $"<div>Dear {FormOwner.EmployeePreferredFullName}</div>" +
                                    $"<br/><div>Thank you for completing the conflict of interest eForm where you have confirmed that you do not have a conflict of interest regarding this recruitment process.</div>" +
                                    $"<br/><div>You will shortly receive an email from the Recruitment Team containing information on how to log into the Recruitment Advertising Management System (RAMS) to access the applications.</div>";
                        toEmail = requester + recruitmentModel.PanelChair.EmployeeEmail + ";" + string.Join(";", recruitmentModel.PanelMembers.Select(x => x.EmployeeEmail).ToArray()) + ";" + await GetTalentTeamGroup() + ";" + externalEmail;
                        emailsToSend.Add(new Tuple<string, string, string, string, string>(toEmail, null, emailSubject, emailBody, string.Empty));
                    }
                    else
                    {
                        emailSubject = $"Recruitment Process {dbRecord.FormInfoId} - {recruitmentModel.PositionTitle} - Conflict of interest declaration - for approval";
                        emailBody = string.Format(CoITemplates.SUBMITTED_DG_TEMPLATE, FormOwner.EmployeePreferredFullName, clickHere);
                        toEmail = IsFormOwnerEdODG ? ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL : toEmail;
                        emailsToSend.Add(new Tuple<string, string, string, string, string>(toEmail, null, emailSubject, emailBody, FormOwner.EmployeeEmail));
                    }

                    break;
                case "Requester External":
                    var externalUserData = recruitmentModel.ExternalPanelMember.FirstOrDefault(x => x.Email == toEmail);
                    emailBody = $"<div>Dear {externalUserData.Name}</div>" +
                                $"<br/><div>Thank you for completing the conflict of interest eForm where you have confirmed that you do not have a conflict of interest regarding this recruitment process.</div>" +
                                $"<br/><div>You will shortly receive an email from the Recruitment Team containing information on how to log into the Recruitment Advertising Management System (RAMS) to access the applications.</div>";
                    toEmail = requester + recruitmentModel.PanelChair.EmployeeEmail + ";" + string.Join(";", recruitmentModel.PanelMembers.Select(x => x.EmployeeEmail).ToArray()) + ";" + await GetTalentTeamGroup() + ";" + externalEmailList;
                    emailsToSend.Add(new Tuple<string, string, string, string, string>(toEmail, null, emailSubject, emailBody, string.Empty));
                    break;
                case "Panel Chair":
                    employeeData = await EmployeeService.GetEmployeeByAzureIdAsync(toGuid!.Value);
                    emailBody = $"<div>Dear {employeeData.EmployeePreferredFullName}</div>" +
                                "<br/><div>A conflict of interest has been declared by one of the panel members for the above recruitment process.</div>" +
                                $"<br/><div>Please {clickHere} to review the declaration.</div>" +
                                "<br/><div>Further information about conflicts of interest relating to recruitment is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or via your Recruitment Advisor.</div>";
                    toEmail = employeeData.EmployeeEmail;
                    emailsToSend.Add(new Tuple<string, string, string, string, string>(toEmail, null, emailSubject, emailBody, string.Empty));
                    break;
                case "Panel Member":
                    employeeData = await EmployeeService.GetEmployeeByAzureIdAsync(toGuid!.Value);
                    var panelText = recruitmentModel.PanelChair.ActiveDirectoryId == FormOwner.ActiveDirectoryId
                        ? "panel chair"
                        : "one of the panel members";
                    emailBody = $"<div>Dear {employeeData.EmployeePreferredFullName}</div>" +
                                $"<br/><div>A Conflict of Interest Declaration has been submitted by {panelText} in relation to the above recruitment process.</div>" +
                                "<br/><div>Please contact the Chair Person and the other Panel Member/s to discuss this declaration and management strategies to be put in place.</div>" +
                                "<br/><div>Should you require any clarification on this process, please contact your Recruitment Advisor on 6551 6888.</div>" +
                                $"<br/><div>Please {clickHere} to review the declaration.</div>";
                    toEmail = employeeData.EmployeeEmail;
                    emailsToSend.Add(new Tuple<string, string, string, string, string>(toEmail, null, emailSubject, emailBody, string.Empty));
                    break;
                case ConflictOfInterest.TALENT_TEAM_GROUP_NAME:
                    emailBody = "<div>Dear Recruitment Advisor</div>" +
                                "<br/><div>A conflict of interest declaration eForm has been submitted for the above recruitment process and has been endorsed by the Panel Chair.</div>" +
                                $"<br/><div>{clickHere} to view the form and relevant management strategy.</div>";
                    toEmail = await GetTalentTeamGroup();
                    emailsToSend.Add(new Tuple<string, string, string, string, string>(toEmail, null, emailSubject, emailBody, string.Empty));
                    break;
                case "Ready For Approval":
                    if (FormOwner.EmployeeManagementTier is <= 3 and not 1)
                    {
                        foreach (var manager in FormOwner.Managers)
                        {
                            emailBody = $"<div>Dear {manager.EmployeePreferredFullName}</div>" +
                                        $"<br/><div>A conflict of interest declaration has been submitted for the above recruitment process.</div>" +
                                        $"<br/><div>The Panel Members have discussed the declared conflict in consultation with the Recruitment Advisor Team and put in place relevant management strategies.</div>" +
                                        $"<br/><div>Please {clickHere} to view the form and approve accordingly.</div>";
                            toEmail = manager.EmployeeEmail;
                            emailsToSend.Add(new Tuple<string, string, string, string, string>(toEmail, null, emailSubject, emailBody, string.Empty));
                        }
                    }
                    else
                    {
                        foreach (var user in ExecutiveDirectorPeopleAndCulture)
                        {
                            emailBody = $"<div>Dear {user.EmployeePreferredFullName}</div>" +
                                        $"<br/><div>A conflict of interest declaration has been submitted for the above recruitment process.</div>" +
                                        $"<br/><div>The Panel Members have discussed the declared conflict in consultation with the Recruitment Advisor Team and put in place relevant management strategies.</div>" +
                                        $"<br/><div>Please {clickHere} to view the form and approve accordingly.</div>";
                            toEmail = user.EmployeeEmail;
                            emailsToSend.Add(new Tuple<string, string, string, string, string>(toEmail, null, emailSubject, emailBody, string.Empty));
                        }

                    }
                    break;
                case "Rejected":
                    var memberText = recruitmentModel.PanelChair.ActiveDirectoryId == FormOwner.ActiveDirectoryId ? "Panel Member" : "Chairperson";

                    emailBody = $"<div>Dear {FormOwner.EmployeePreferredFullName}</div>" +
                                $"<br/><div>The conflict of interest declaration submitted in relation to the above recruitment process has not been endorsed by either the {memberText} or the Executive Director People and Culture.</div>" +
                                $"<br/><div>Please contact either the Chairperson or the Recruitment Advisor to discuss further.</div>";
                    toEmail = requester + recruitmentModel.PanelChair.EmployeeEmail + ";" + string.Join(";", recruitmentModel.PanelMembers.Select(x => x.EmployeeEmail).ToArray());
                    emailsToSend.Add(new Tuple<string, string, string, string, string>(toEmail, null, emailSubject, emailBody, string.Empty));
                    break;
                case "Approved":
                    if (!FormOwnerReportToT1)
                    {
                        emailBody = "<div>Dear Panel Members and Recruitment Advisor Team</div>" +
                                $"<br/><div>The {RequestingUser.EmployeePositionTitle} has approved the management strategies to be put in place in relation to the declared conflict of interest for the above recruitment process.</div>" +
                                $"<br/><div>The recruitment and selection process can now proceed with all Panel Members being given access to the applications.</div>";
                        toEmail = requester + recruitmentModel.PanelChair.EmployeeEmail + ";" + await GetTalentTeamGroup() + ";" + string.Join(";", recruitmentModel.PanelMembers.Select(x => x.EmployeeEmail).ToArray()) + ";" + externalEmailList;
                        emailsToSend.Add(new Tuple<string, string, string, string, string>(toEmail, null, emailSubject, emailBody, string.Empty));
                    }
                    else
                    {
                        emailSubject = $"Recruitment Process {dbRecord.FormInfoId} - {recruitmentModel.PositionTitle} - Conflict of interest declaration outcome";
                        emailBody = string.Format(CoITemplates.APPROVED_DG_TEMPLATE, FormOwner.EmployeePreferredFullName, clickHere);
                        toEmail = FormOwner.EmployeeEmail;
                        var ccEmail = ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL;
                        if (!IsFormOwnerEdODG)
                        {
                            var eDOdgUserList = await EmployeeService.GetEmployeeByPositionNumberAsync(ConflictOfInterest.ED_ODG_POSITION_ID);
                            ccEmail = eDOdgUserList.GetDescription();
                        }
                        emailsToSend.Add(new Tuple<string, string, string, string, string>(toEmail, null,
                                emailSubject, emailBody, ccEmail));
                    }
                    break;
                case "External":
                    var externalUser = recruitmentModel.ExternalPanelMember.FirstOrDefault(x => x.Email == toEmail);
                    emailBody = $"<div>Dear {externalUser.Name}</div><br/>" +
                                $"<div>A conflict of interest declaration has been submitted by one of the panel members in relation to the above selection process.</div><br/>" +
                                $"<div>Please contact the Chair Person and the other Panel Member/s to discuss this declaration and the management strategies to be put in place.</div><br/>" +
                                $"<div>Should you require any clarification on this process, please contact your Recruitment Advisor on 6551 6888.</div><br/>";
                    emailsToSend.Add(new Tuple<string, string, string, string, string>(toEmail, null, emailSubject, emailBody, string.Empty));
                    break;
                case "Recall":
                    emailBody = $"<div>Hi,</div><br/>" +
                                $"<div>The conflict of interest request {dbRecord.FormInfoId} has been recalled by {FormOwner.EmployeePreferredFullName}.</div><br/>" +
                                $"<div>Please {clickHere} to review the declaration. </div><br/>" +
                                $"<div>Should you require any clarification on this process, please contact your Recruitment Advisor on 6551 6888.</div><br/>";

                    toEmail = recruitmentModel.PanelChair.ActiveDirectoryId == FormOwner.ActiveDirectoryId
                        ? FormOwner.EmployeeEmail
                        : $"{FormOwner.EmployeeEmail};{recruitmentModel.PanelChair.EmployeeEmail};";
                    emailsToSend.Add(new Tuple<string, string, string, string, string>(toEmail, null, emailSubject, emailBody, string.Empty));
                    break;
                case "RequestOnBehalf":
                    emailBody = $"<div>Dear {userInfo.EmployeePreferredFullName}</div>" +
                                     $"<br/><div>A conflict of interest declaration has been submitted for the above recruitment process.</div>" +
                                     $"<br/><div>The Panel Members have discussed the declared conflict in consultation with the Recruitment Advisor Team and put in place relevant management strategies.</div>" +
                                     $"<br/><div>Please {clickHere} to view the form and approve accordingly.</div>";
                    emailsToSend.Add(new Tuple<string, string, string, string, string>(userInfo.EmployeeEmail, null, emailSubject, emailBody, string.Empty));
                    break;
            }

            foreach (var emailParams in emailsToSend)
            {
                emailBody = emailParams.Item4 + emailFooter;
                await _formEmailService.SendEmail(dbRecord.FormInfoId, emailParams.Item1, emailParams.Item3, emailBody, emailParams.Item5);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error thrown from {nameof(RecruitmentFormService)}");
        }
    }
}