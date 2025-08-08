using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.Constants.COI;
using eforms_middleware.DataModel;
using eforms_middleware.Settings;
using eforms_middleware.Workflows;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using eforms_middleware.Interfaces;
using eforms_middleware.Specifications;
using Microsoft.Extensions.Configuration;
using static eforms_middleware.Settings.Helper;

namespace eforms_middleware.Services
{
    public class COINotificationService : BaseApprovalService
    {
        private readonly IRepository<TaskInfo> _taskInfoRepository;
        private readonly IRepository<AllForm> _allFormRepository;
        private readonly COIPermisionService _COIPermisionService;
        private readonly string _baseUrl;
        private  string SummaryPath => "coi-other/summary";

        public COINotificationService(IFormEmailService formEmailService
            , IRepository<FormInfo> formInfoRepository
            , IRepository<TaskInfo> taskInfoRepository
            , IRepository<FormHistory> formHistoryRepository
            , IRepository<AllForm> allFormRepository
            , IRepository<AdfGroup> adfGroup
            , IRepository<AdfPosition> adfPosition
            , IRepository<AdfUser> adfUser
            , IRepository<AdfGroupMember> adfGroupMember
            , COIPermisionService cOIPermisionService
            , IRepository<RefFormStatus> formRefStatus
            , IPermissionManager permissionManager
            , IFormInfoService formInfoService
            , IConfiguration configuration
            , ITaskManager taskManager
            , IFormHistoryService formHistoryService
            , IRequestingUserProvider requestingUserProvider
            , IEmployeeService employeeService
            , IRepository<WorkflowBtn> workflowBtnRepository
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
            _taskInfoRepository = taskInfoRepository;

            _allFormRepository = allFormRepository;
            _COIPermisionService = cOIPermisionService;
            _baseUrl = configuration.GetValue<string>("BaseEformsUrl");
        }

        public async Task RunReminder(ILogger log)
        {
            log.LogInformation("Requesting Tasks for {0}", DateTime.Today);
            var today = DateTime.Today;
            var specialReminderTaskInfo = await _taskInfoRepository.FindByAsync(x =>
                x.SpecialReminderDate.Value.Date == today
                && x.SpecialReminder == true && x.ActiveRecord == true);

            var escalationTaskInfo = await _taskInfoRepository.FindByAsync(x =>
                x.EscalationDate.Value.Date == today
                && x.Escalation == true && x.ActiveRecord == true);
            log.LogInformation("Total of {0} reminders to process", specialReminderTaskInfo.Count + escalationTaskInfo.Count);

            await SendNotificationMain(specialReminderTaskInfo, log);
            await SendNotificationMain(escalationTaskInfo, log, true);
            await SendYearlyNotification(log);
        }

        private async Task SendNotificationMain(List<TaskInfo> taskInfo
            , ILogger log
            , bool isEscalation = false)
        {
            foreach (var task in taskInfo)
            {
                try
                {
                    var formInfoData = await _formInfoRepository.FindByAsync(x => x.FormInfoId == task.FormInfoId
                        && !string.IsNullOrEmpty(x.NextApprover)
                        && x.NextApprover == task.AssignedTo
                        && x.FormOwnerEmail != task.AssignedTo);
                    if (formInfoData.Any())
                    {
                        var formInfo = formInfoData.FirstOrDefault();
                        var dt = JsonConvert.DeserializeObject<COIMain>(formInfo.Response);

                        var reqUrl = $"<a href=\"{BaseEformsURL}/conflict-of-interest/summary/{formInfo.FormInfoId}\">click here</a>";

                        switch (formInfo.AllFormsId)
                        {
                            case (int)FormType.CoI_GBH:
                                if (isEscalation)
                                {
                                    await SendGBH5DayNotification(formInfo, reqUrl, log);
                                }
                                else
                                {
                                    await SendGBHThirdDayNotification(formInfo, reqUrl, log);
                                }
                                break;
                            case (int)FormType.CoI_SE:
                                if (isEscalation)
                                {
                                    await SendSE5DayNotification(formInfo, reqUrl, log);
                                }
                                else
                                {
                                    await SendSEThirdDayNotification(formInfo, reqUrl, log);
                                }
                                break;
                            case (int)FormType.CoI_REC:
                                if (isEscalation)
                                {
                                    await Send5DayRecruitment(formInfo, reqUrl, log);
                                }
                                else
                                {
                                    await SendRecruitmentNotification(formInfo, reqUrl, log);
                                    await UpdateTaskInfo(task.TaskInfoId, formInfo.AllFormsId);
                                }
                                break;
                        }
                    }
                }
                catch (Exception e)
                {
                    log.LogError(e, "FormId: {TaskFormInfoId} - Error in {SendNotificationMainName}", task.FormInfoId, nameof(SendNotificationMain));
                }
            }
        }

        #region "GBH"

        private async Task SendGBHThirdDayNotification(FormInfo formInfo
            , string reqUrl
            , ILogger log)
        {
            try
            {
                var messages = new List<MailMessage>();

                await SetupBaseApprovalService(formInfo.FormInfoId);

                if (CurrentApprovers.Any(x => x.PositionId.HasValue && x.PositionId == FormOwner.ManagerPositionId))
                {
                    foreach (var manager in FormOwner.Managers)
                    {
                        const string subject = "Reminder: Conflict of Interest declaration for your review";
                        var body = string.Format(GBHEmailTemplate.RequestorManagerNotify, manager.EmployeePreferredFullName, reqUrl);
                        messages.Add(new MailMessage(FromEmail, manager.EmployeeEmail, subject, body));
                    }
                }
                else if (CurrentApprovers.Any(x => x.PositionId.HasValue && x.PositionId == FormOwner.ExecutiveDirectorPositionId))
                {
                    foreach (var executiveDirector in FormOwner.ExecutiveDirectors)
                    {
                        const string subject = "Reminder: Conflict of interest declaration for your review";
                        var body = string.Format(GBHEmailTemplate.Tier3, executiveDirector.EmployeePreferredFullName, reqUrl);
                        messages.Add(new MailMessage(FromEmail, executiveDirector.EmployeeEmail, subject, body));
                    }
                }
                else if (ActioningGroup.Id == ConflictOfInterest.ODG_AUDIT_GROUP_ID)
                {
                    const string subject = "Reminder: Conflict of interest declaration for QA review";
                    var body = string.Format(GBHEmailTemplate.GovAudit, CurrentApprovers.Single().PermissionActionerDescription, reqUrl);
                    messages.Add(new MailMessage(FromEmail, ActioningGroup.GroupEmail, subject, body));
                }

                _formEmailService.SendEmail(messages);
            }
            catch (Exception e)
            {
                log.LogError(e, "FormId: {FormInfoFormInfoId} - Error in {SendGbhThirdDayNotificationName}", formInfo.FormInfoId, nameof(SendGBHThirdDayNotification));
            }
        }

        private async Task SendGBH5DayNotification(FormInfo formInfo
            , string reqUrl
            , ILogger log)
        {
            try
            {
                await SetupBaseApprovalService(formInfo.FormInfoId);
                var messages = new List<MailMessage>();

                var willEscalateAgain = true;
                if (!FormOwner.ExecutiveDirectors.Any() || CurrentApprovers.Any(x => x.PositionId == FormOwner.ExecutiveDirectorPositionId))
                {
                    formInfo.NextApprover = await GetODGGroupEmail();
                    formInfo.NextApprovalLevel = EmployeeTitle.GovAuditGroup.GetEmployeeTitle();
                    var subject = "Reminder: Gift, benefit, hospitality –  Conflict of interest declaration for your review";
                    var body = string.Format(GBHEmailTemplate.GovAuditEscalated, FormOwner.ExecutiveDirectorName, reqUrl);
                    var message = new MailMessage(FromEmail, formInfo.NextApprover, subject, body);
                    foreach (var executiveDirector in FormOwner.ExecutiveDirectors)
                    {
                        message.CC.Add(executiveDirector.EmployeeEmail);
                    }
                    messages.Add(message);
                    willEscalateAgain = false;
                }
                else if (CurrentApprovers.Any(x => x.PositionId == FormOwner.ManagerPositionId))
                {
                    formInfo.NextApprover = FormOwner.ExecutiveDirectorIdentifier;
                    formInfo.NextApprovalLevel = EmployeeTitle.Tier3.GetEmployeeTitle();
                    // CC in this scenario and others like it most important is it addressed to the correct recipient
                    // and duplicates to the managers will have to be tolerated
                    var subject = "Reminder: Gift, benefit, hospitality –  Conflict of Interest declaration for your review";
                    foreach (var executiveDirector in FormOwner.ExecutiveDirectors)
                    {
                        var body = string.Format(GBHEmailTemplate.TIER_3_ESCALATED, executiveDirector.EmployeePreferredFullName, FormOwner.EmployeePreferredFullName, FormOwner.ManagerName, reqUrl);
                        var message = new MailMessage(FromEmail, executiveDirector.EmployeeEmail, subject, body);
                        foreach (var manager in FormOwner.Managers)
                        {
                            message.CC.Add(manager.EmployeeEmail);
                        }
                        messages.Add(message);
                    }
                }

                if (messages.Any())
                {
                    formInfo.FormSubStatus = FormStatus.Escalated.ToString();
                    await CreateNewTaskInfo(formInfo, CurrentApprovers.Single().PermissionActionerDescription, log, COIFormType.GiftBenefitHospitality, willEscalateAgain);
                    _formEmailService.SendEmail(messages);
                }
            }
            catch (Exception e)
            {
                log.LogError(e, "FormId: {FormInfoFormInfoId} - Error in {SendGbh5DayNotificationName}", formInfo.FormInfoId, nameof(SendGBH5DayNotification));
            }
        }

        #endregion

        #region "SE"

        private async Task SendSEThirdDayNotification(FormInfo formInfo
            , string reqUrl
            , ILogger log)
        {
            try
            {
                var messages = new List<MailMessage>();
                await SetupBaseApprovalService(formInfo.FormInfoId);

                if (CurrentApprovers.Any(x => x.PositionId.HasValue && x.PositionId == FormOwner.ManagerPositionId))
                {
                    const string subject = "Reminder: Secondary Employment request for your review";
                    foreach (var manager in FormOwner.Managers)
                    {
                        var body = string.Format(SecondaryEmployementTemplate.RequestorManager, manager.EmployeePreferredFullName, FormOwner.EmployeePreferredFullName, reqUrl);
                        messages.Add(new MailMessage(FromEmail, manager.EmployeeEmail, subject, body));
                    }
                }
                else if (CurrentApprovers.Any(x => x.PositionId.HasValue && x.PositionId == FormOwner.ExecutiveDirectorPositionId))
                {
                    const string subject = "Reminder: Secondary Employment request for your review";
                    foreach (var executiveDirector in FormOwner.ExecutiveDirectors)
                    {
                        var body = string.Format(SecondaryEmployementTemplate.Tier3, executiveDirector.EmployeePreferredFullName, FormOwner.EmployeePreferredFullName, reqUrl);
                        messages.Add(new MailMessage(FromEmail, executiveDirector.EmployeeEmail, subject, body));
                    }
                }

                _formEmailService.SendEmail(messages);
            }
            catch (Exception e)
            {
                log.LogError(e, "FormId: {FormInfoFormInfoId} - Error in {SendSeThirdDayNotificationName}", formInfo.FormInfoId, nameof(SendSEThirdDayNotification));
            }
        }

        private async Task SendSE5DayNotification(FormInfo formInfo
            , string reqUrl
            , ILogger log)
        {
            try
            {
                const string subject = "Reminder: Secondary Employment request for your review";
                string body = string.Empty;
                string ccEmail = string.Empty;
                await SetupBaseApprovalService(formInfo.FormInfoId);
                var messages = new List<MailMessage>();
                formInfo.FormSubStatus = FormStatus.Escalated.ToString();
                bool allowEscalation = false;
                List<string> emailRec = new List<string>();
                // Tier 3 is empty/null or is the current approver
                if (!FormOwner.ExecutiveDirectors.Any() || CurrentApprovers.Any(x => x.PositionId.HasValue && x.PositionId == FormOwner.ExecutiveDirectorPositionId))
                {
                    formInfo.FormStatusId = (int)FormStatus.Approved;
                    var adminGroupEmail = await GetPodEformsBusinessAdminGroup();
                    formInfo.NextApprover = adminGroupEmail;
                    formInfo.NextApprovalLevel = ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_NAME;
                    body = string.Format(SecondaryEmployementTemplate.Tier3Escalation, ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_NAME, FormOwner.EmployeePreferredFullName, FormOwner.ExecutiveDirectors?.FirstOrDefault()?.EmployeePositionTitle, reqUrl);
                    ccEmail = string.Join(";", FormOwner.ExecutiveDirectors.Select(x => x.EmployeeEmail));
                    emailRec.Add(adminGroupEmail);
                }
                // Manager is the current approver
                else if (CurrentApprovers.Any(x => x.PositionId.HasValue && x.PositionId == FormOwner.ManagerPositionId))
                {
                    var approvers = await GetAdfUserByPositionId(CurrentApprovers?.FirstOrDefault()?.PositionId ?? 0);
                    ccEmail = string.Join(";", approvers.Select(x => x.EmployeeEmail));
                    formInfo.NextApprovalLevel = FormOwner?.ExecutiveDirectors?.FirstOrDefault()?.EmployeePositionTitle;
                    formInfo.FormStatusId = (int)FormStatus.Endorsed;
                    formInfo.NextApprover = FormOwner?.ExecutiveDirectors?.FirstOrDefault()?.EmployeeEmail;
                    body = string.Format(SecondaryEmployementTemplate.Tier3Escalation, formInfo.NextApprovalLevel, FormOwner.EmployeePreferredFullName, FormOwner.ManagerName, reqUrl);
                    foreach (var executiveDirector in FormOwner.ExecutiveDirectors)
                    {                        
                        emailRec.Add(executiveDirector.EmployeeEmail);                        
                    }
                    allowEscalation = true;
                }
                
                await CreateNewTaskInfo(formInfo, CurrentApprovers?.FirstOrDefault()?.Email, log, COIFormType.SecondaryEmployment, allowEscalation);
                foreach (var email in emailRec) 
                {
                    await _formEmailService.SendEmail(formInfo.FormInfoId, email, subject, body, ccEmail);
                }

            }
            catch (Exception e)
            {
                log.LogError(e, "FormId: {FormInfoFormInfoId} - Error in {SendSe5DayNotificationName}", formInfo.FormInfoId, nameof(SendSE5DayNotification));
            }
        }
        #endregion

        #region "Recruitment"

        private async Task SendRecruitmentNotification(FormInfo formInfo
            , string reqUrl
            , ILogger log)
        {
            try
            {
                var messages = new List<MailMessage>();

                var recruitmentModel = JsonConvert.DeserializeObject<RecruitmentModel>(formInfo.Response);
                var subject = $"Reminder: Recruitment Process eF number: {formInfo.FormInfoId} - {recruitmentModel.PositionTitle}";
                var specification = new FormPermissionSpecification(formInfo.FormInfoId,
                    permissionFlag: (byte)PermissionFlag.UserActionable, addUserInfo: true,
                    addPositionInfo: true, addGroupInfo: true);
                var currentPermissions = await _permissionManager.GetPermissionsBySpecificationAsync(specification);
                var currentPermission = currentPermissions.Single();

                if (currentPermission.UserId.HasValue && currentPermission.UserId == recruitmentModel.PanelChair.ActiveDirectoryId)
                {
                    var body = string.Format(RecruitmentEmailTemplate.PanelChair, currentPermission.User.EmployeeFullName, reqUrl);
                    messages.Add(new MailMessage(FromEmail, to: currentPermission.User.EmployeeEmail, subject, body));
                }
                else if (currentPermission.UserId.HasValue && recruitmentModel.PanelMembers.Any(x => x.ActiveDirectoryId == currentPermission.UserId))
                {
                    var body = string.Format(RecruitmentEmailTemplate.PanelMember, currentPermission.User.EmployeeFullName, reqUrl);
                    messages.Add(new MailMessage(FromEmail, to: currentPermission.User.EmployeeEmail, subject, body));
                }
                else if (currentPermission.GroupId.HasValue && currentPermission.GroupId == ConflictOfInterest.TALENT_TEAM_GROUP_ID)
                {
                    var body = string.Format(RecruitmentEmailTemplate.RecruitmentAdvisor, reqUrl);
                    messages.Add(new MailMessage(FromEmail, to: currentPermission.Group.GroupEmail, subject, body));
                }
                else
                {
                    foreach (var executiveDirectorPeopleAndCulture in currentPermission.Position.AdfUserPositions)
                    {
                        var body = string.Format(RecruitmentEmailTemplate.EDPeopleCulture, executiveDirectorPeopleAndCulture.EmployeeFullName, reqUrl);
                        messages.Add(new MailMessage(FromEmail, to: executiveDirectorPeopleAndCulture.EmployeeEmail, subject, body));
                    }
                }

                _formEmailService.SendEmail(messages);
            }
            catch (Exception e)
            {
                log.LogError(e, "FormId: {FormInfoFormInfoId} - Error in {SendRecruitmentNotificationName}", formInfo.FormInfoId, nameof(SendRecruitmentNotification));
            }
        }

        private async Task Send5DayRecruitment(FormInfo formInfo
            , string reqUrl
            , ILogger log)
        {
            try
            {
                var nextApprover = await GetAdfUserByEmail(formInfo.NextApprover);
                var name = nextApprover.EmployeePreferredFullName;
                var recruitmentModel = JsonConvert.DeserializeObject<RecruitmentModel>(formInfo.Response);

                string subject = $"Reminder: Recruitment Process eF number: {formInfo.FormInfoId} - {recruitmentModel.PositionTitle}";
                string body = string.Format(RecruitmentEmailTemplate.RecruitmentAdvisorNotify, name);

                await _formEmailService.SendEmail(formInfo.FormInfoId
                    , await GetTalentTeamGroup()
                    , subject
                    , body);
            }
            catch (Exception e)
            {
                log.LogError(e, "FormId: {FormInfoFormInfoId} - Error in {Send5DayRecruitmentName}", formInfo.FormInfoId, nameof(Send5DayRecruitment));
            }
        }

        #endregion

        #region "12 Month Notification"

        private async Task SendYearlyNotification(ILogger log)
        {
            var lastYear = DateTime.Today.AddYears(-1);
            log.LogInformation("Processing forms completed on {LastYear}", lastYear);

            var completedYear = await _formInfoRepository.FindByAsync(x =>
                x.FormStatusId == (int)FormStatus.Completed && x.CompletedDate.HasValue &&
                x.CompletedDate.Value.Date == lastYear);
            log.LogDebug("Processing {CompletedCount} forms that require completed at supplied date", completedYear.Count);

            foreach (var item in completedYear)
            {
                await SetupBaseApprovalService(item.FormInfoId);
                if (item.AllFormsId is (int)FormType.CoI_GBC or (int)FormType.CoI_CPR or (int)FormType.CoI_Other)
                {
                    var formType = "";
                    var subject = "";
                    switch ((FormType)item.AllFormsId)
                    {
                        case FormType.CoI_GBC:
                            formType = "Government board/committee";
                            subject = "Government board/committee declaration reminder";
                            break;
                        case FormType.CoI_CPR:
                            formType = "Close Personal Relationship in Workplace";
                            subject = "Close Personal Relationship in Workplace declaration reminder";
                            break;
                        case FormType.CoI_Other:
                            formType = "(Other)";
                            subject = "Other - Conflict of Interest declaration reminder";
                            break;
                    }
                    var suffixText = "Employee Services " +
                                     "on (08) 6551 6888 / " +
                                     "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a>";

                   var odgGovernancelink= "ODG Governance and Audit via " +
                                            "<a href=\"mailto:odggovernanceandaudit@transport.wa.gov.au\">odggovernanceandaudit@transport.wa.gov.au</a>";

        var SummaryHref = $"<a href={_baseUrl}/{SummaryPath}/{item.FormInfoId}>click here</a>";
                    var body = "";
                    if (item.AllFormsId is (int)FormType.CoI_Other){
                         body = string.Format(CoIOtherTemplates.TWELVE_MONTH_REMINDER, FormOwner.EmployeePreferredFullName,
                       item.CompletedDate, SummaryHref, odgGovernancelink);
                        _formEmailService.SendEmail(item.FormInfoId, FormOwner.EmployeeEmail, subject, body, FormOwner.ManagerIdentifier);
                    }
                    else{
                         body = string.Format(CoITemplates.TWELVE_MONTH_REMINDER, FormOwner.EmployeePreferredFullName,
                         item.CompletedDate, formType, suffixText);

                        _formEmailService.SendEmail(item.FormInfoId, FormOwner.EmployeeEmail, subject, body,
                            string.Join(';', FormOwner.Managers.Select(x => x.EmployeeEmail)));
                    }
                    break;
                }
                try
                {
                    var formType = item.ChildFormType.GetParseEnum<COIFormType>();
                    switch (formType)
                    {
                        case COIFormType.SecondaryEmployment:
                            await SetupBaseApprovalService(item.FormInfoId);
                            var cc = string.Join(';', FormOwner.Managers.Select(x => x.EmployeeEmail));
                            var body = string.Format(SecondaryEmployementTemplate.Reminder, FormOwner.EmployeePreferredFullName, item.CompletedDate);
                            _formEmailService.SendEmail(item.FormInfoId
                                , ConflictOfInterest.TALENT_TEAM_GROUP_EMAIL
                                , "Secondary Employment declaration reminder"
                                , body
                                , cc);
                            break;
                    }
                }
                catch (Exception e)
                {
                    log.LogError(e, "FormId: {ItemFormInfoId} - Error in {SendYearlyNotificationName}", item.FormInfoId, nameof(SendYearlyNotification));
                }
            }
        }

        #endregion

        #region "Task Update"   

        private async Task UpdateTaskInfo(int taskInfoId, int allFormId)
        {
            var taskInfoDt = await _taskInfoRepository.FirstOrDefaultAsync(x => x.TaskInfoId == taskInfoId);

            taskInfoDt.SpecialReminder = true;
            taskInfoDt.SpecialReminderDate = allFormId == (int)FormType.CoI_REC ?
                        DateTime.Today.AddDays(2) :
                        DateTime.Today.AddDays(3);

            _taskInfoRepository.Update(taskInfoDt);
        }

        private async Task CreateNewTaskInfo(FormInfo formInfoDt
            , string actualApprover
            , ILogger log
            , COIFormType formType
            , bool escalation)
        {
            try
            {
                _formInfoRepository.Update(formInfoDt);
                var status = (FormStatus)formInfoDt.FormStatusId;

                var taskInfoDt = _taskInfoRepository.FindBy(x => x.FormInfoId == formInfoDt.FormInfoId);

                foreach (var item in taskInfoDt)
                {
                    item.SpecialReminder = false;
                    item.Escalation = false;
                    item.SpecialReminderDate = null;
                    item.EscalationDate = null;
                    item.ActiveRecord = false;
                    _taskInfoRepository.Update(item);
                }

                _taskInfoRepository.Create(new TaskInfo()
                {
                    AllFormsId = formInfoDt.AllFormsId,
                    FormInfoId = formInfoDt.FormInfoId,
                    FormOwnerEmail = formInfoDt.FormOwnerEmail,
                    AssignedTo = formInfoDt.NextApprover,
                    TaskStatus = status.ToString(),
                    TaskCreatedBy = "System",
                    TaskCreatedDate = DateTime.Now,
                    ActiveRecord = formInfoDt.ActiveRecord,
                    SpecialReminderDate =
                        formType == COIFormType.Recruitment ?
                        DateTime.Today.AddDays(2) :
                        DateTime.Today.AddDays(3),
                    SpecialReminderTo = formInfoDt.NextApprover,
                    // It doesn't need escalating everytime
                    EscalationDate = escalation ? DateTime.Today.AddDays(5) : null,
                    Escalation = escalation,
                    SpecialReminder = true,
                    ReminderTo = formInfoDt.NextApprover
                });

                await AddToHistory(formInfoDt, actualApprover, log);
                await _COIPermisionService.SetPermission(formInfoDt.FormInfoId);
            }
            catch (Exception e)
            {
                log.LogError(e, $"FormId: {formInfoDt.FormInfoId} - Error in {nameof(CreateNewTaskInfo)}");
            }
        }

        private async Task AddToHistory(FormInfo formInfoDt, string actualApprover, ILogger log)
        {
            try
            {
                var escalatedTo = await GetFullName(formInfoDt.NextApprover);
                var actionType = $"Escalated to {escalatedTo}";

                _formHistoryRepository.Create(new FormHistory
                {
                    ActionType = actionType,
                    ActionBy = "System",
                    ActiveRecord = true,
                    AditionalComments = $"Escalated to {escalatedTo}, no Action from {actualApprover}",
                    AllFormsId = formInfoDt.AllFormsId,
                    Created = DateTime.Now,
                    FormStatusId = formInfoDt.FormStatusId,
                    FormInfoId = formInfoDt.FormInfoId,
                    RejectedReason = string.Empty,
                    ActionByPosition = "Automated System"
                });
            }
            catch (Exception e)
            {
                log.LogError(e, $"FormId: {formInfoDt.FormInfoId} - Error in {nameof(AddToHistory)}");
            }
        }
        #endregion
    }
}