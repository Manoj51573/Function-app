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

public class GBHService : BaseApprovalService
{
    private readonly ILogger<GBHService> _logger;

    public GBHService(IFormEmailService formEmailService
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
        , ILogger<GBHService> logger
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
        _logger = logger;
    }

    public async Task<RequestResult> GBHApprovalProcess(FormInfoInsertModel formInfoInsertModel)
    {
        await SetupBaseApprovalService(formInfoInsertModel.FormDetails.FormInfoID);
        var permissions = new List<FormPermission>();
        var formStatus = formInfoInsertModel.FormAction.GetParseEnum<FormStatus>();
        var subStatus = formStatus.ToString();

        var emails = new List<NotificationInfo>();

        var request = new FormInfoUpdate
        { FormAction = formInfoInsertModel.FormAction, FormDetails = formInfoInsertModel.ToFormDetailsRequest(), UserId = "" };
        var dbForm = await FormInfoService.GetExistingOrNewFormInfoAsync(request);
        var requestFormModel = JsonConvert.DeserializeObject<GBHModel>(formInfoInsertModel.FormDetails.Response);
        var isRequestOnBehalf = requestFormModel.IsRequestOnBehalf;
        var isFormOwnerEdODG = FormOwner.EmployeePositionId == ConflictOfInterest.ED_ODG_POSITION_ID;

        if (isRequestOnBehalf)
        {
            var isRequestorODG = await EmployeeService.IsOdgGroupUser(FormOwner.ActiveDirectoryId);
            if (!isRequestorODG)
            {
                return RequestResult.SuccessRequestWithErrorMessage(new
                {
                    Outcome = "Failed",
                    OutcomeValues = new
                    {
                        Error = "You must be a member of ODG Governance and Audit to submit eForm on behalf of other employee."
                    }
                });
            }
        }
        GBHModel gbhModel = requestFormModel;
        if (!string.IsNullOrEmpty(dbForm.Response))
        {
            gbhModel = JsonConvert.DeserializeObject<GBHModel>(dbForm.Response);
        }

        dbForm.FormStatusId = (int)formStatus;
        dbForm.FormSubStatus = formStatus.ToString();
        var manager = FormOwner.EmployeeManagementTier == 4 && formStatus == FormStatus.Submitted
                    ? FormOwner.ExecutiveDirectors
                    : FormOwner.Managers;


        switch (formStatus)
        {
            case FormStatus.Unsubmitted:
                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, userId: FormOwner.ActiveDirectoryId, isOwner: true));
                dbForm.Response = JsonConvert.SerializeObject(requestFormModel);
                break;
            case FormStatus.Rejected:
                gbhModel.ReasonForDecision = requestFormModel.ReasonForDecision;
                dbForm.Response = JsonConvert.SerializeObject(gbhModel);
                goto case FormStatus.Recall;
            case FormStatus.Recall:
                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, userId: FormOwner.ActiveDirectoryId, isOwner: true));
                dbForm.NextApprover = null;
                dbForm.FormStatusId = (int)FormStatus.Unsubmitted;
                dbForm.FormSubStatus = formStatus == FormStatus.Recall ? FormStatus.Recalled.ToString()
                    : formStatus.ToString();
                emails.Add(new NotificationInfo(formStatus));
                break;
            case FormStatus.Submitted:
                permissions.Add(new FormPermission((byte)PermissionFlag.View, userId: FormOwner.ActiveDirectoryId, isOwner: true));
                requestFormModel.ReasonForDecision = string.Empty;
                dbForm.Response = JsonConvert.SerializeObject(requestFormModel);
                dbForm.SubmittedDate = DateTime.Now;
                if (!isRequestOnBehalf)
                {
                    if (FormOwnerReportToT1)
                    {
                        var edOdgUserList = await EmployeeService
                                                .GetEmployeeByPositionNumberAsync(positionNumber: ConflictOfInterest.ED_ODG_POSITION_ID);

                        permissions.Add(new FormPermission
                        {
                            PermissionFlag = (byte)PermissionFlag.UserActionable,
                            GroupId = isFormOwnerEdODG ? ConflictOfInterest.ODG_AUDIT_GROUP_ID : null,
                            PositionId = !isFormOwnerEdODG ? ConflictOfInterest.ED_ODG_POSITION_ID : null
                        });

                        dbForm.NextApprover = isFormOwnerEdODG ? ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL : edOdgUserList.GetDescription();
                        dbForm.NextApprovalLevel = isFormOwnerEdODG ? EmployeeTitle.GovAuditGroup.GetEmployeeTitle() : EmployeeTitle.ExecutiveDirectorODG.ToString();
                        dbForm.FormSubStatus = FormStatus.SubmittedAndEndorsed.GetFormStatusTitle();
                        emails.Add(new NotificationInfo(FormStatus.Requestor, toUser: edOdgUserList.FirstOrDefault()));
                    }
                    else
                    {
                        emails.Add(new NotificationInfo(FormStatus.Requestor));
                        if (FormOwner.EmployeeManagementTier is 1)
                        {
                            var executiveDirectorsOperationsAndGovernance = await EmployeeService.GetEmployeeByPositionNumberAsync(ConflictOfInterest.ED_ODG_POSITION_ID);
                            permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: ConflictOfInterest.ED_ODG_POSITION_ID));
                            emails.AddRange(GetNotificationsFromUsers(formStatus, executiveDirectorsOperationsAndGovernance));
                            dbForm.NextApprover = executiveDirectorsOperationsAndGovernance.GetDescription();
                            dbForm.FormSubStatus = FormStatus.SubmittedAndEndorsed.GetFormStatusTitle();
                            dbForm.NextApprovalLevel = EmployeeTitle.ExecutiveDirectorODG.ToString();
                        }
                        else if (manager.Any())
                        {
                            permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: manager.FirstOrDefault().EmployeePositionId));
                            emails.AddRange(GetNotificationsFromUsers(formStatus, manager));
                            dbForm.NextApprovalLevel = EmployeeTitle.RequestorManager.ToString();
                            dbForm.NextApprover = manager.FirstOrDefault().EmployeeEmail;

                        }
                        else
                        {
                            goto case FormStatus.AssignToBackup;
                        }
                    }
                }
                else
                {
                    dbForm.FormSubStatus = FormStatus.SubmittedAndEndorsed.GetFormStatusTitle();
                    dbForm.NextApprovalLevel = EmployeeTitle.ExecutiveDirectorODG.ToString();
                    permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable,
                        positionId: ConflictOfInterest.ED_ODG_POSITION_ID));
                    var eDOdgUserList = await EmployeeService.GetEmployeeByPositionNumberAsync(ConflictOfInterest.ED_ODG_POSITION_ID);
                    foreach (var EodgUser in eDOdgUserList)
                    {
                        emails.Add(new NotificationInfo(FormStatus.Submitted, null, null, EodgUser));
                    }
                    dbForm.NextApprover = eDOdgUserList.GetDescription();
                }
                break;
            case FormStatus.Approved:
                var nextLevel = formInfoInsertModel.FormDetails.NextApprovalLevel.GetParseEnum<EmployeeTitle>();

                if (nextLevel == EmployeeTitle.Tier3 ||
                    nextLevel == EmployeeTitle.Delegated ||
                    nextLevel == EmployeeTitle.IndependentReview ||
                    FormOwner.ManagerManagementTier is 3)
                {
                    gbhModel.IsTier3Actioned = true;
                    dbForm.FormSubStatus = nextLevel switch
                    {
                        EmployeeTitle.IndependentReview => FormStatus.IndependentReviewApproved.GetFormStatusTitle(),
                        EmployeeTitle.Delegate => FormStatus.DelegationApproved.GetFormStatusTitle(),
                        _ => subStatus
                    };
                    goto case FormStatus.AssignToBackup;
                }
                if (FormOwner.ExecutiveDirectors.Any()
                    && FormOwner.EmployeeManagementTier != 4)
                {
                    permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, positionId: FormOwner.ExecutiveDirectorPositionId));
                    emails.AddRange(GetNotificationsFromUsers(formStatus, FormOwner.ExecutiveDirectors));
                    dbForm.NextApprover = FormOwner.ExecutiveDirectorIdentifier;
                    dbForm.NextApprovalLevel = EmployeeTitle.Tier3.ToString();
                }
                else
                {
                    goto case FormStatus.AssignToBackup;
                }

                break;
            case FormStatus.Completed:
                isFormOwnerEdODG = FormOwner.EmployeePositionId == ConflictOfInterest.ED_ODG_POSITION_ID;
                emails.Add(new NotificationInfo(FormStatus.Completed));
                dbForm.NextApprover = null;
                dbForm.CompletedDate = DateTime.Now;
                break;
            case FormStatus.Delegated:
            case FormStatus.IndependentReview:
                var userId = Guid.Parse(formInfoInsertModel.FormDetails.NextApprover);
                var manualNextApprover =
                    await EmployeeService.GetEmployeeByAzureIdAsync(userId);
                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, userId: manualNextApprover.ActiveDirectoryId));
                emails.Add(new NotificationInfo(formStatus, toUser: manualNextApprover));
                var nextApprovalLevel = Enum.Parse<EmployeeTitle>(formStatus.ToString());
                dbForm.NextApprover = manualNextApprover.EmployeeEmail;
                dbForm.NextApprovalLevel = formStatus.ToString();
                dbForm.FormSubStatus = nextApprovalLevel.GetEmployeeTitle();
                break;
            case FormStatus.AssignToBackup:
                permissions.Add(new FormPermission((byte)PermissionFlag.UserActionable, groupId: ConflictOfInterest.ODG_AUDIT_GROUP_ID));
                emails.Add(new NotificationInfo(formStatus, toUser: new EmployeeDetailsDto
                {
                    EmployeePreferredName = ConflictOfInterest.ODG_AUDIT_GROUP_NAME,
                    EmployeeEmail = await GetODGGroupEmail(),
                    ActiveDirectoryId = ConflictOfInterest.ODG_AUDIT_GROUP_ID
                }));
                dbForm.NextApprovalLevel = EmployeeTitle.GovAuditGroup.GetEmployeeTitle();
                dbForm.NextApprover = await GetODGGroupEmail();
                break;
        }

        // When submitting and saving the response should just be taken as is
        if (dbForm.FormStatusId is not (int)FormStatus.Unsubmitted)
        {
            dbForm.FormApprovers += RequestingUser.EmployeeEmail + ";";
        }

        try
        {
            var formInfo = await FormInfoService.SaveFormInfoAsync(request, dbForm);
            await UpdateTasksAsync(formInfo);
            await _permissionManager.UpdateFormPermissionsAsync(formInfo.FormInfoId, permissions);
            await UpdateHistoryAsync(request, formInfo, ActioningGroup?.Id, formInfoInsertModel.AdditionalInfo, requestFormModel.ReasonForDecision);
            foreach (var email in emails)
            {
                Enum.TryParse(dbForm.NextApprovalLevel, out EmployeeTitle nextApproverLevel);
                await SendEmailAsync(dbForm.FormInfoId, email.EmailType, nextApproverLevel, dbForm.FormSubStatus,
                    email.ToUser, isFormOwnerEdODG, FormOwnerReportToT1);
            }
            return RequestResult.SuccessfulFormInfoRequest(formInfo);
        }
        catch (Exception e)
        {
            return RequestResult.FailedRequest(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    private static IEnumerable<NotificationInfo> GetNotificationsFromUsers(FormStatus formStatus, IEnumerable<IUserInfo> users)
    {
        return users.Select(user => new NotificationInfo(formStatus, toUser: user)).ToList();
    }

    private async Task SendEmailAsync(int formInfoId
        , FormStatus formStatus // this one could have been changed from the original request value
        , EmployeeTitle title
        , string subStatus, IUserInfo toUser
        , bool IsRequestorEdODG = false
        , bool FormOwnerReportToT1 = false)
    {
        var subject = "Gift, benefit, hospitality – Conflict of interest declaration ";
        var body = "";
        var ccEmail = "";
        var reqUrl = $"{BaseUrl}/conflict-of-interest/summary/{formInfoId}";
        var editUrl = $"{BaseUrl}/conflict-of-interest/{formInfoId}";
        var SummaryHref = $"<a href={reqUrl}>click here</a>";
        var editHref = $"<a href={editUrl}>click here</a>";
        var toEmail = toUser?.EmployeeEmail;

        try
        {
            switch (formStatus)
            {
                case FormStatus.Rejected:
                    toEmail = FormOwner.EmployeeEmail;
                    body += $"<div>Dear {FormOwner.EmployeePreferredFullName}</div>";

                    if (ActioningGroup?.Id == ConflictOfInterest.ODG_AUDIT_GROUP_ID)
                    {
                        subject += "not approved by Governance and Audit Group";
                        body += "<br/><div>The conflict of interest declaration you submitted in relation to the " +
                                "offer of a gift, benefit or hospitality has not been approved.</div>" +
                                $"<br/><div>Please <a href=\"{editUrl}\">click here</a> to view decision maker " +
                                "comments and amend your declaration. You may also want to contact your line manager " +
                                "to discuss further.</div>";
                    }
                    else if (RequestingUser.EmployeePositionId == FormOwner.ManagerPositionId)
                    {
                        subject += "not endorsed by line manager";
                        body += "<br/><div>The conflict of interest declaration you submitted in relation to the " +
                                "offer of a gift, benefit or hospitality has not been endorsed by your " +
                                "line manager.</div>" +
                                $"<br/><div>Please <a href=\"{editUrl}\">click here</a> to view their comments and " +
                                "amend and resubmit your declaration. You may also want to contact your line manager to discuss further.</div>";
                    }
                    else
                    {
                        subject += "not approved by senior management";
                        body += "<br/><div>The conflict of interest declaration you submitted in relation to the " +
                                "offer of a gift, benefit or hospitality has not been approved.</div>" +
                                $"<br/><div>Please <a href=\"{editUrl}\">click here</a> to view the decision maker’s " +
                                "comments and amend your declaration. You may also want to contact your line manager to discuss further.</div>";
                    }
                    break;
                case FormStatus.Submitted:
                    subject += "for your review";
                    body += $"<div>Dear {toUser.EmployeePreferredFullName}</div>" +
                            "<br/><div>A conflict of interest declaration regarding the offer of a gift, benefit or " +
                            $"hospitality has been submitted by your employee, {FormOwner.EmployeePreferredFullName}.</div>" +
                            $"<br/><div>Please <a href=\"{reqUrl}\">click here</a> to review the declaration.</div>";
                    break;
                case FormStatus.Recall:
                    subject += "recalled";
                    body += $"<div>Dear {FormOwner.EmployeePreferredFullName}</div><br/>" +
                            "<div>You have recalled your conflict of interest declaration. " +
                            $"<a href=\"{editUrl}\">Click here</a> to view, amend and resubmit your declaration as " +
                            "necessary.</div><br/>";
                    toEmail = FormOwner.EmployeeEmail;
                    ccEmail = !FormOwnerReportToT1 ? string.Join(';', FormOwner.Managers.Select(x => x.EmployeeEmail)) : string.Empty;
                    break;
                case FormStatus.Requestor:
                    if (!FormOwnerReportToT1)
                    {
                        toEmail = FormOwner.EmployeeEmail;
                        subject += "submitted";
                        body += $"<div>Dear {FormOwner.EmployeePreferredFullName}</div>" +
                                "<br/><div>Thank you for your conflict of interest declaration in relation to the offer of a " +
                                "gift, benefit or hospitality.</div>" +
                                "<br/><div>Your declaration has been sent to your manager for review.</div>" +
                                $"<br/><div>Please <a href=\"{reqUrl}\">click here</a> if you want to view, recall " +
                                "(cancel/amend) your declaration.</div>";
                    }
                    else
                    {
                        subject = "Conflict of interest declaration - for approval";
                        toEmail = IsRequestorEdODG ? ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL : toUser.EmployeeEmail;
                        ccEmail = FormOwner.EmployeeEmail;
                        body += string.Format(CoITemplates.SUBMITTED_DG_TEMPLATE, FormOwner.EmployeePreferredFullName, SummaryHref);
                    }
                    break;
                case FormStatus.Endorsed:
                    subject += "for review";
                    body += $"<div>Dear {toUser.EmployeePreferredFullName}</div>" +
                            "<br/><div>A conflict of interest declaration has been submitted in relation to the offer of a gift, benefit or hospitality and has been approved by the line manager.</div>" +
                            $"<br/><div>Please <a href=\"{reqUrl}\">click here</a> to review the declaration.</div>";
                    break;
                case FormStatus.Approved:
                    if (toUser!.ActiveDirectoryId == ConflictOfInterest.ODG_AUDIT_GROUP_ID)
                    {
                        var formSubStatus = subStatus.GetParseEnum<FormStatus>();
                        subject += formSubStatus == FormStatus.IndependentReviewApproved ? "for completion" : "for QA review";

                        body += $"<div>Dear {toUser.EmployeePreferredFullName}</div>" +
                                "<br/><div>A conflict of interest declaration has been received from " +
                                $"{FormOwner.EmployeePreferredFullName} regarding the offer of a gift, benefit or " +
                                $"hospitality.</div>" +
                                $"<br/><div>The declaration has been reviewed and was approved by {RequestingUser.EmployeePreferredFullName}.</div>" +
                                $"<br/><div>Please <a href=\"{reqUrl}\">click here</a> to review the form for quality assurance purposes.</div>";
                        toEmail = toUser.EmployeeEmail;
                    }
                    else
                    {
                        toEmail = toUser.EmployeeEmail;
                        ccEmail = ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL;
                        subject += "for your review";
                        body += $"<div>Dear {toUser.EmployeePreferredFullName}</div>" +
                                "<br/><div>A conflict of interest declaration has been submitted in relation to the offer of a gift, benefit or hospitality and has been approved by the line manager.</div>" +
                                $"<br/><div>Please <a href=\"{reqUrl}\">click here</a> to review the declaration.</div>";
                    }
                    break;
                case FormStatus.IndependentReview:
                    toEmail = toUser.EmployeeEmail;
                    subject += "for independent review";
                    body += $"<div>Dear {toUser.EmployeePreferredFullName}</div>" +
                            "<br/><div>A request for independent review of a conflict of interest decision has been " +
                            $"received from {FormOwner.EmployeePreferredFullName}. You have been selected to " +
                            $"undertake an independent review. Please <a href=\"{reqUrl}\">click here</a> to complete.</div>";
                    break;
                case FormStatus.IndependentReviewCompleted:
                    toEmail = FormOwner.EmployeeEmail;
                    if (FormOwner.Managers.Any())
                    {
                        var emails = new List<string> { ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL };
                        emails.AddRange(FormOwner.Managers.Select(x => x.EmployeeEmail));
                        ccEmail = string.Join(';', emails);
                    }
                    else
                    {
                        ccEmail = await GetODGGroupEmail() + ";";
                    }
                    subject += "outcome";
                    body += $"<div>Dear {FormOwner.EmployeePreferredFullName}</div>" +
                        "<div>Your conflict of interest declaration regarding the offer of a gift, benefit or hospitality has been independently reviewed.</div>" +
                            $"<br/><div>Please <a href=\"{reqUrl}\">click here</a> to view the form including any approved plan to manage the conflict.</div>";
                    break;
                case FormStatus.Delegated:
                    subject += "for your review";
                    body += $"<div>Dear {toUser.EmployeePreferredFullName}</div>" +
                            "<br/><div>A conflict of interest declaration regarding the offer of a gift, benefit or " +
                            $"hospitality submitted by {FormOwner.EmployeePreferredFullName} has been delegated to you by " +
                            "ODG Governance and Audit for review.</div>" +
                            $"<br/><div>Please <a href=\"{reqUrl}\">click here</a> to complete.</div>";
                    break;
                case FormStatus.Completed:
                    if (!FormOwnerReportToT1)
                    {
                        toEmail = FormOwner.EmployeeEmail;
                        if (FormOwner.Managers.Any())
                        {
                            var emails = new List<string> { ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL };
                            emails.AddRange(FormOwner.Managers.Select(x => x.EmployeeEmail));
                            ccEmail = string.Join(';', emails);
                        }
                        else
                        {
                            ccEmail = await GetODGGroupEmail() + ";";
                        }
                        if (FormOwner.ExecutiveDirectors.Any())
                        {
                            ccEmail += ";" + string.Join(';', FormOwner.ExecutiveDirectors.Select(x => x.EmployeeEmail));
                        }
                        subject += "outcome";
                        body += $"<div>Dear {FormOwner.EmployeePreferredFullName}</div>" +
                                "<br/><div>Your conflict of interest declaration regarding the offer of a gift, benefit " +
                                "or hospitality has been approved by senior management.</div>" +
                                $"<br/><div>Please <a href=\"{reqUrl}\">click here</a> to view management comments, including " +
                                "any requirements to manage conflicts of interest (as applicable).</div>";
                    }
                    else
                    {
                        subject = "Conflict of interest declaration outcome";
                        ccEmail = ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL;
                        if (!IsRequestorEdODG)
                        {
                            var eDOdgUserList = await EmployeeService.GetEmployeeByPositionNumberAsync(ConflictOfInterest.ED_ODG_POSITION_ID);
                            ccEmail = eDOdgUserList.GetDescription();
                        }
                        toEmail = FormOwner.EmployeeEmail;
                        body += string.Format(CoITemplates.APPROVED_DG_TEMPLATE, FormOwner.EmployeePreferredFullName, editUrl);
                    }
                    break;
            }

            if (title != EmployeeTitle.ExecutiveDirector)
            {
                body +=
                    "<br/><div>Further information about conflicts of interest relating to gifts, benefits and " +
                    "hospitality is available on <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, " +
                    "or from Governance and Audit via " +
                    "<a href=\"mailto:ODGGovernanceandAudit@transport.wa.gov.au\">ODGGovernanceandAudit@transport.wa.gov.au</a> " +
                    "or (08) 6551 7083.</div>";
            }

            if (formStatus == FormStatus.Completed && !IsRequestorEdODG)
            {
                var executiveDirectorOdg = await EmployeeService.GetEmployeeByPositionNumberAsync(ConflictOfInterest.ED_ODG_POSITION_ID);
                body += $"<br/><div>On behalf of {executiveDirectorOdg.GetDescription()}</div>" +
                        $"<br/><div>Executive Director</div>" +
                        $"<br/><div>Office of the Director General</div>";
            }

            if (formStatus == FormStatus.Recall)
            {
                body += "<br/><br/><div>Thank you</div>";
            }

            await _formEmailService.SendEmail(formInfoId, toEmail, subject, body, ccEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error thrown from {nameof(GBHService)}");
        }
    }


}