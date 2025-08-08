using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure.Interfaces;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Specifications;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eforms_middleware.Workflows;

public class BaseApprovalService
{
    public readonly IFormEmailService _formEmailService;
    public readonly IRepository<AdfGroup> _adfGroup;
    public readonly IRepository<AdfPosition> _adfPosition;
    public readonly IRepository<AdfUser> _adfUser;
    public readonly IRepository<AdfGroupMember> _adfGroupMember;
    public readonly IRepository<FormInfo> _formInfoRepository;
    public readonly IRepository<RefFormStatus> _formRefStatus;
    public readonly IRepository<FormHistory> _formHistoryRepository;
    public readonly IPermissionManager _permissionManager;
    protected readonly IFormInfoService FormInfoService;
    private readonly ITaskManager _taskManager;
    private readonly IFormHistoryService _formHistoryService;
    private readonly IRequestingUserProvider _requestingUserProvider;
    protected readonly IEmployeeService EmployeeService;
    protected readonly string BaseUrl;
    protected IUserInfo RequestingUser;
    protected IEnumerable<FormPermission> CurrentApprovers { get; set; }
    protected IUserInfo FormOwner { get; set; }
    protected IList<IUserInfo> OwingPositionUsers { get; set; }
    //protected IUserInfo FormOwner { get; set; }

    protected AdfGroup ActioningGroup { get; set; }
    protected IRepository<WorkflowBtn> WorkflowBtnRepository;
    public IList<FormPermission> Permissions { get; set; }
    protected bool FormOwnerReportToT1
    {
        get
        {
            return FormOwner.Managers.Any(x => x.EmployeeManagementTier is 1);
        }
    }



    public BaseApprovalService(
        IFormEmailService formEmailService
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
        , IRepository<WorkflowBtn> workflowBtnRepository)
    {
        _formEmailService = formEmailService;
        _adfGroup = adfGroup;
        _adfPosition = adfPosition;
        _adfUser = adfUser;
        _adfGroupMember = adfGroupMember;
        _formInfoRepository = formInfoRepository;
        _formRefStatus = formRefStatus;
        _formHistoryRepository = formHistoryRepository;
        _permissionManager = permissionManager;
        FormInfoService = formInfoService;
        _taskManager = taskManager;
        _formHistoryService = formHistoryService;
        _requestingUserProvider = requestingUserProvider;
        EmployeeService = employeeService;
        BaseUrl = configuration.GetValue<string>("BaseEformsUrl");
        WorkflowBtnRepository = workflowBtnRepository;
    }

    protected async Task SetupBaseApprovalService(int? formInfoId)
    {
        RequestingUser = await _requestingUserProvider.GetRequestingUser();
        Permissions = new List<FormPermission>();
        if (formInfoId.HasValue)
        {
            var specification = new FormPermissionSpecification(formInfoId.Value, addGroupMemberInfo: true, addPositionInfo: true, addUserInfo: true);
            Permissions = await _permissionManager.GetPermissionsBySpecificationAsync(specification);
            var owner = Permissions.Single(x => x.IsOwner);
            if (owner.UserId.HasValue)
            {
                FormOwner = await EmployeeService.GetEmployeeByAzureIdAsync(Permissions.Single(x => x.IsOwner).UserId!.Value);
            }
            if (owner.PositionId.HasValue)
            {
                OwingPositionUsers = await EmployeeService.GetEmployeeByPositionNumberAsync(owner.PositionId.Value);
            }

            CurrentApprovers = Permissions.Where(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable).ToList();
            ActioningGroup = CurrentApprovers.Any() ? CurrentApprovers.Single().Group : null;
        }
        else
        {
            FormOwner = await _requestingUserProvider.GetRequestingUser();
        }
    }

    public async Task<List<AdfGroup>> GetAdfGroupByMemberEmail(string email)
    {
        var result = new List<AdfGroup>();
        try
        {
            var user = await GetAdfUserByEmail(email);
            var groupMember = _adfGroupMember.FindBy(x => x.MemberId == user.ActiveDirectoryId);

            foreach (var item in groupMember)
            {
                var dt = await GetAdfGroupById(item.GroupId);
                result.Add(dt);
            }
        }
        catch
        {
            return null;
        }
        return result;
    }

    public async Task<IUserInfo> GetAdfUserByEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return null;

        try
        {
            return await EmployeeService.GetEmployeeByEmailAsync(email);
        }
        catch
        {
            return null;
        }
    }

    public async Task<AdfUser> GetUserByEmail(string email)
    {
        return await _adfUser.FirstOrDefaultAsync(x => x.EmployeeEmail == email);
    }

    public async Task<IUserInfo> GetAdfUserById(string id)
    {
        return await EmployeeService.GetEmployeeByAzureIdAsync(new Guid(id));
    }

    public async Task<IList<IUserInfo>> GetAdfUserByPositionId(int id)
    {
        return await EmployeeService.GetEmployeeByPositionNumberAsync(id);

    }


    public async Task<AdfGroup> GetAdfGroupByEmail(string email)
    {
        try
        {
            return await _adfGroup.FirstOrDefaultAsync(x => x.GroupEmail == email);

        }
        catch
        {
            return null;
        }
    }

    public async Task<AdfGroup> GetAdfGroupById(Guid id)
    {
        try
        {
            return await _adfGroup.FirstOrDefaultAsync(x => x.Id == id);
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> GetODGGroupEmail()
    {
        var result = await GetAdfGroupById(ConflictOfInterest.ODG_AUDIT_GROUP_ID);
        return result.GroupEmail;
    }

    public async Task<string> GetTalentTeamGroup()
    {
        var result = await GetAdfGroupById(ConflictOfInterest.TALENT_TEAM_GROUP_ID);
        return result.GroupEmail;
    }

    public async Task<string> GetPodEformsBusinessAdminGroup()
    {
        var result = await GetAdfGroupById(ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID);
        return result.GroupEmail;
    }

    public async Task<string> GetPodESOGroup()
    {
        var result = await GetAdfGroupById(ConflictOfInterest.POD_ESO_GROUP_ID);
        return result.GroupEmail;
    }

    public async Task<AdfPosition> GetAdfPositionByEmail(string email)
    {
        try
        {
            var member = await GetAdfUserByEmail(email);
            return await GetAdfPositionByAdId(member?.ActiveDirectoryId);
        }
        catch
        {
            return null;
        }
    }

    public async Task<AdfPosition> GetAdfPositionByAdId(Guid? id)
    {
        try
        {
            // TODO This isn't necessary as we can get the position from the user now
            throw new NotImplementedException();
            // return await _adfPosition.FirstOrDefaultAsync(x => x.CurrentOccupantAdId == id);
        }
        catch (Exception ex)
        {
            return null;
        }

    }


    public async Task<IList<IUserInfo>> GetEDPeopleAndCulture(bool isLeaveFlag = true)
    {
        try
        {
            var result =
                await EmployeeService.GetEmployeeByPositionNumberAsync(ConflictOfInterest
                    .ED_PEOPLE_AND_CULTURE_POSITION_ID);
            return result;
        }
        catch
        {
            return null;
        }
    }

    public async Task<AdfUser> GetTierThree(string requestorEmail
        , int? formId)
    {
        try
        {
            // TODO this is probably not needed anymore as the Employee Service will already returning this
            throw new NotImplementedException();
            // var requestor = await GetAdfPositionByEmail(requestorEmail);
            // string directorate = requestor?.Directorate;
            // if (requestor == null)
            // {
            // var dt = await _formInfoRepository.FirstOrDefaultAsync(x => x.FormInfoId == formId);
            // directorate = dt?.FormOwnerDirectorate;
            // }

            // var tier3 = await _adfPosition.FirstOrDefaultAsync(x => x.Directorate == requestor.Directorate
            //  && x.ManagementTier == 3);

            // return await _adfUser.FirstOrDefaultAsync(x => tier3.CurrentOccupantAdId == x.ActiveDirectoryId);
        }
        catch
        {
            return null;
        }
    }

    public async Task<AdfUser> GetEmployeeByPositionNumber(string positionNumber)
    {
        try
        {
            // TODO this is probably not needed anymore as the Employee Service will already returning this
            throw new NotImplementedException();
            // if (string.IsNullOrEmpty(positionNumber))
            // {
            //     return null;
            // }
            //
            // var positionMember = await GetAdfPositionByPositionNumber(positionNumber);
            //
            // return await GetAdfUserAdId(positionMember.CurrentOccupantAdId);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get Executive Director Office of the Director General
    /// </summary>
    /// <param name="requestorPositionNumber"></param>
    /// <returns></returns>
    public async Task<IUserInfo> GetEDODG()
    {
        try
        {
            var dt = await EmployeeService.GetEmployeeByPositionNumberAsync(ConflictOfInterest.ED_ODG_POSITION_ID);
            return dt.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    public async Task<string> GetFullName(string toEmail, bool firstNameOnly = false)
    {
        var result = string.Empty;
        try
        {
            try
            {
                IUserInfo memberUser = await GetAdfUserByEmail(toEmail);
                result = firstNameOnly
                   ? memberUser.EmployeePreferredName
                   : memberUser.EmployeePreferredFullName;
            }
            catch
            {
                var groupMember = await GetAdfGroupByEmail(toEmail);
                result = groupMember?.GroupName;
            }
        }
        catch
        {

        }
        return result;
    }

    public async Task<IUserInfo> GetManagerByEmail(string formOwnerEmail)
    {
        var dt = await EmployeeService.GetEmployeeDetailsAsync(formOwnerEmail);
        return dt.Managers.FirstOrDefault();
    }

    public async Task<AdfUser> GetRequestorManager(string formOwnerEmail)
    {
        try
        {
            var requestor = await GetAdfPositionByEmail(formOwnerEmail);
            var requestorManager = await GetEmployeeByPositionNumber(requestor.ReportsPositionNumber);
            return requestorManager;
        }
        catch
        {
            return null;
        }
    }

    public async Task<RefFormStatus> GetFormStatusByName(string formStatus)
    {
        return await _formRefStatus.FirstOrDefaultAsync(x => x.Status == formStatus);
    }

    public async Task<FormInfo> CreateFormInfo(FormInfoRequest form)
    {
        var formOwner = await GetAdfUserByEmail(form.FormOwnerEmail);
        var formInfo = _formInfoRepository.Create(new FormInfo()
        {
            FormItemId = 1,
            AllFormsId = form.AllFormsId,
            Response = form.Response,
            FormOwnerName = $"{formOwner.EmployeeFirstName} {formOwner.EmployeeSurname}",
            FormOwnerEmail = formOwner.EmployeeEmail,
            FormOwnerDirectorate = formOwner.Directorate,
            FormOwnerPositionNo = formOwner.EmployeePositionNumber,
            FormOwnerEmployeeNo = formOwner.EmployeeNumber,
            FormOwnerPositionTitle = formOwner.EmployeePositionTitle,
            FormStatusId = form.StatusId,
            FormSubStatus = form.FormSubStatus,
            Created = DateTime.Now,
            CreatedBy = form.ActionBy,
            SubmittedDate = form.StatusId == (int)FormStatus.Submitted ? DateTime.Now : null,
            CompletedDate = form.StatusId == (int)FormStatus.Completed ? DateTime.Now : null,
            ActiveRecord = true,
            NextApprover = form.NextApproverEmail,
            NextApprovalLevel = form.NextApproverTitle
        });

        await AddToHistory(formInfo, form);
        return formInfo;
    }

    public async Task<FormInfo> UpdateFormInfo(FormInfoRequest form)
    {
        var formInfo = await _formInfoRepository.FirstOrDefaultAsync(x => x.FormInfoId == form.FormId);
        var currentApprover = formInfo.NextApprover;
        if (form.IsUpdateAllowed)
        {
            formInfo.Response = form.Response;
        }
        formInfo.FormStatusId = form.StatusId;
        formInfo.FormSubStatus = form.FormSubStatus;
        formInfo.SubmittedDate = form.StatusId == (int)FormStatus.Submitted
            ? DateTime.Now : formInfo.SubmittedDate;
        formInfo.CompletedDate = form.StatusId == (int)FormStatus.Completed
            ? DateTime.Now : formInfo.CompletedDate;
        formInfo.NextApprover = form.NextApproverEmail;
        formInfo.NextApprovalLevel = form.NextApproverTitle;

        _formInfoRepository.Update(formInfo);
        await AddToHistory(formInfo, form);
        return formInfo;
    }

    public async Task UpdateOnBehalfUser(int? formId
        , string formOwnerEmail)
    {
        try
        {
            var formInfo = await _formInfoRepository.FirstOrDefaultAsync(x => x.FormInfoId == formId);
            if (formInfo != null)
            {
                var formOwner = await GetAdfUserByEmail(formOwnerEmail);
                formInfo.FormOwnerName = $"{formOwner.EmployeeFirstName} {formOwner.EmployeeSurname}";
                formInfo.FormOwnerEmail = formOwner.EmployeeEmail;
                formInfo.FormOwnerDirectorate = formOwner.Directorate;
                formInfo.FormOwnerPositionNo = formOwner.EmployeePositionNumber;
                formInfo.FormOwnerEmployeeNo = formOwner.EmployeeNumber;
                formInfo.FormOwnerPositionTitle = formOwner.EmployeePositionTitle;
                _formInfoRepository.Update(formInfo);
                await _permissionManager.RemoveAllPremissonOnReject(formId ?? 0, true);
            }
        }
        catch
        {

        }
    }

    private async Task AddToHistory(FormInfo form
        , FormInfoRequest formReq)
    {
        var actionUser = await GetAdfUserByEmail(formReq.ActionBy);
        _formHistoryRepository.Create(new FormHistory
        {
            ActionType = formReq.FormSubStatus,
            ActionBy = formReq.ActionBy,
            ActiveRecord = form.ActiveRecord,
            AditionalComments = formReq.AdditionalInfo,
            AllFormsId = formReq.AllFormsId,
            Created = DateTime.Now,
            FormStatusId = formReq.StatusId,
            FormInfoId = form.FormInfoId,
            RejectedReason = formReq.ReasonForRejection,
            ActionByPosition = actionUser.EmployeePositionTitle
        });
        await SetBasePermission(form);
    }

    public async Task SetBasePermission(FormInfo formInfo)
    {
        var formPermissions = new List<FormPermission>();

        if (formInfo.FormStatusId == (int)FormStatus.Unsubmitted
            || formInfo.FormStatusId == (int)FormStatus.Submitted)
        {
            var formOwner = await GetAdfUserByEmail(formInfo.FormOwnerEmail);
            formPermissions.Add(new FormPermission
            {
                FormId = formInfo.FormInfoId,
                PermissionFlag = formInfo.FormStatusId == (int)FormStatus.Submitted
                    ? (byte)PermissionFlag.View
                    : (byte)PermissionFlag.UserActionable,
                IsOwner = true,
                PositionId = null,
                GroupId = null,
                UserId = formOwner.ActiveDirectoryId
            });
        }

        //Give permission to creator if form is submitted on behalf
        if (formInfo.FormOwnerEmail != formInfo.CreatedBy)
        {
            var creator = await GetAdfUserByEmail(formInfo.CreatedBy);
            formPermissions.Add(new FormPermission
            {
                FormId = formInfo.FormInfoId,
                PermissionFlag = (byte)PermissionFlag.View,
                IsOwner = false,
                PositionId = creator?.EmployeePositionId,
                GroupId = null,
                UserId = null
            });
        }

        if (!string.IsNullOrEmpty(formInfo.NextApprover))
        {
            var group = await GetAdfGroupByEmail(formInfo.NextApprover);
            var approverPosition = group?.Id == null ?
                await GetAdfUserByEmail(formInfo.NextApprover)
                : null;
            formPermissions.Add(new FormPermission
            {
                FormId = formInfo.FormInfoId,
                PermissionFlag = formInfo.FormStatusId == (int)FormStatus.Completed
                    ? (byte)PermissionFlag.View
                    : (byte)PermissionFlag.UserActionable,
                IsOwner = false,
                PositionId = approverPosition?.EmployeePositionId,
                GroupId = group?.Id,
                UserId = null
            });
        }

        await _permissionManager.UpdateFormPermissionsAsync(formInfo.FormInfoId, formPermissions);
    }

    #region "Set Workflow Button"

    public async Task SetNextApproverBtn(int formId
        , StatusBtnData btnStatus)
    {
        try
        {
            if (btnStatus == null) return;

            var existingWorkFlow =
                await WorkflowBtnRepository.FindByAsync(x => x.FormId == formId
                 && x.FormSubStatus != FormStatus.Delegated.ToString());
            if (btnStatus.StatusBtnModel.Where(x => x.BtnText == FormStatus.Delegate.ToString()).Any())
            {
                existingWorkFlow = await WorkflowBtnRepository.FindByAsync(x => x.FormId == formId);
            }

            existingWorkFlow.ForEach(x =>
            {
                x.IsActive = false;
                WorkflowBtnRepository.Update(x);
            });

            var dt = await _permissionManager.GetActionPermission(formId);
            foreach (var permission in dt)
            {
                var isAllowed = CheckGroupPosition(permission, btnStatus);
                if (btnStatus.IsDelegate && isAllowed)
                {
                    GetDelegateBtn(formId, permission.Id);
                }
                if (btnStatus.IsIndependentReview && isAllowed)
                {
                    GetIndependentRvwBtn(formId, permission.Id);
                }

                btnStatus.StatusBtnModel?.ForEach(x =>
                {
                    WorkflowBtnRepository.Create(new WorkflowBtn()
                    {
                        FormId = formId,
                        IsActive = true,
                        PermisionId = permission.Id,
                        StatusId = x.StatusId,
                        FormSubStatus = x.FormSubStatus,
                        BtnText = x.BtnText
                    });
                });
            }
        }
        catch
        {

        }
    }

    private bool CheckGroupPosition(FormPermission permission, StatusBtnData btnData)
    {
        if (btnData.PositionId == null && btnData.GroupId == null)
        {
            return false;
        }

        if (btnData.GroupId != null && permission.GroupId == btnData.GroupId)
        {
            return true;
        }

        if (btnData.PositionId != null
                && permission.PositionId == btnData.PositionId)
        {
            return true;
        }
        return false;
    }

    public async Task UpdateNextApproverBtn(int formId)
    {
        var existingData = await WorkflowBtnRepository.FindByAsync(x => x.FormId == formId);
        if (existingData.Any())
        {
            existingData.ForEach(x =>
            {
                x.IsActive = false;
                WorkflowBtnRepository.Update(x);
            });
        }
    }

    public void GetDelegateBtn(int formId
        , int? permissionId
        , string btnText = "Delegate")
    {
        WorkflowBtnRepository.Create(new WorkflowBtn()
        {
            FormId = formId,
            IsActive = true,
            PermisionId = permissionId ?? 0,
            StatusId = (int)FormStatus.Delegated,
            FormSubStatus = FormStatus.Delegated.ToString(),
            BtnText = btnText
        });
    }

    private void GetIndependentRvwBtn(int formId
        , int permissionId)
    {
        WorkflowBtnRepository.Create(new WorkflowBtn()
        {
            FormId = formId,
            IsActive = true,
            PermisionId = permissionId,
            StatusId = (int)FormStatus.IndependentReview,
            FormSubStatus = "Independent Reviewed",
            BtnText = "Independent Review"
        });
    }

    public StatusBtnModel SetStatusBtnData(FormStatus formStatus
        , string btnText
        , bool hasDeclaration = false
        , string formSubStatus = "")
    {
        return new StatusBtnModel()
        {
            StatusId = (int)formStatus,
            BtnText = btnText,
            FormSubStatus = string.IsNullOrEmpty(formSubStatus) ? formStatus.ToString() : formSubStatus,
            HasDeclaration = hasDeclaration
        };

    }
    #endregion

    #region "SET Approver To Group"

    public async Task<WorkflowModel> SetFormDefault(FormInfoRequest formInfoRequest)
    {

        if (formInfoRequest.FormId != 0)
        {
            var form = await _formInfoRepository.FirstOrDefaultAsync(x => x.FormInfoId == formInfoRequest.FormId);
            formInfoRequest.PreviousApprover = form.NextApprover;
        }

        formInfoRequest.NextApproverEmail = string.Empty;
        formInfoRequest.NextApproverTitle = string.Empty;
        formInfoRequest.FormSubStatus = formInfoRequest.FormSubStatus;
        formInfoRequest.StatusId = formInfoRequest.StatusId;
        formInfoRequest.IsUpdateAllowed = formInfoRequest.StatusId is (int)FormStatus.Unsubmitted or (int)FormStatus.Submitted;

        EmailSendType type = (FormStatus)formInfoRequest.StatusId == FormStatus.Completed
                            ? EmailSendType.Completed
                            : formInfoRequest.FormSubStatus == FormStatus.Recalled.ToString()
                                ? EmailSendType.Recalled
                                : formInfoRequest.FormSubStatus == FormStatus.Rejected.ToString()
                                ? EmailSendType.Rejected
                                : EmailSendType.None;

        var emailNotificationModel = new EmailNotificationModel();
        emailNotificationModel.EmailSendType = new List<EmailSendType>()
        {
            type
        };
        return new WorkflowModel()
        {
            FormInfoRequest = formInfoRequest,
            StatusBtnData = null,
            EmailNotificationModel = emailNotificationModel
        };
    }

    #endregion
    protected async Task UpdateTasksAsync(FormInfo dbRecord
        , bool isEscalationAllowed = true
        , int remiderDayInterval = 3
        , int escalationDayInterval = 5)
    {
        var task = new TaskInfo
        {
            AllFormsId = dbRecord.AllFormsId,
            FormInfoId = dbRecord.FormInfoId,
            FormOwnerEmail = dbRecord.FormOwnerEmail,
            AssignedTo = dbRecord.NextApprover,
            TaskStatus = ((FormStatus)dbRecord.FormStatusId).ToString(),
            TaskCreatedBy = "System",
            TaskCreatedDate = DateTime.Now,
            ActiveRecord = dbRecord.ActiveRecord,
            SpecialReminderDate = DateTime.Today.AddDays(remiderDayInterval),
            SpecialReminderTo = dbRecord.NextApprover,
            EscalationDate = isEscalationAllowed ? DateTime.Today.AddDays(escalationDayInterval) : null,
            Escalation = isEscalationAllowed,
            ReminderTo = dbRecord.NextApprover,
            SpecialReminder = true
        };
        await _taskManager.AddFormTaskAsync(dbRecord.FormInfoId, task);
    }

    protected async Task UpdateHistoryAsync(FormInfoUpdate request, FormInfo dbRecord, Guid? actioningGroup, string additionalInfo, string ReasonForDecision = "")
    {
        await _formHistoryService.AddFormHistoryAsync(request, dbRecord, actioningGroup, additionalInfo, ReasonForDecision);
    }

    #region "SET Approver To Group"

    public async Task<FormInfoRequest> SetApproverToEdODG(FormInfoRequest travelFormInfo)
    {
        var edODG = await GetEDODG();
        travelFormInfo.NextApproverEmail = edODG?.EmployeeEmail;
        travelFormInfo.NextApproverTitle = edODG.EmployeePositionTitle;
        return travelFormInfo;
    }

    public async Task<FormInfoRequest> SetToApproverToGroup(FormInfoRequest travelFormInfo, Guid groupId)
    {
        var group = await GetAdfGroupById(groupId);
        travelFormInfo.NextApproverEmail = group?.GroupEmail;
        travelFormInfo.NextApproverTitle = group.GroupName;

        return travelFormInfo;
    }

    public async Task<FormInfoRequest> SetFormUnsubmitted(FormInfoRequest travelFormInfo
        , FormStatus formStatus)
    {
        travelFormInfo.NextApproverEmail = string.Empty;
        travelFormInfo.NextApproverTitle = string.Empty;
        travelFormInfo.FormSubStatus = formStatus.ToString();
        travelFormInfo.FormStatus = FormStatus.Unsubmitted.ToString();

        return travelFormInfo;
    }

    public async Task<int?> SetAdHocPermission(int formId
           , Guid? groupId = null
           , int? positionId = null
           , PermissionFlag permissionFlag = PermissionFlag.View)
    {
        var formPermissions = new FormPermission
        {
            FormId = formId,
            PermissionFlag = (byte)permissionFlag,
            IsOwner = false,
            PositionId = positionId,
            GroupId = groupId,
            UserId = null
        };
        return await _permissionManager.SetAdHocPermission(formPermissions);
    }
    public async Task<int?> SetAdHocPermissionWithADId(int formId,
            Guid userId
           , PermissionFlag permissionFlag = PermissionFlag.View)
    {
        var formPermissions = new FormPermission
        {
            FormId = formId,
            PermissionFlag = (byte)permissionFlag,
            IsOwner = false,
            PositionId = null,
            GroupId = null,
            UserId = userId
        };
        return await _permissionManager.SetAdHocPermission(formPermissions);
    }

    #endregion



    protected async Task UpdateTasksAsyncLeaveCashOut(FormInfo dbRecord, FormType formType, string formStatus)
    {
        var status = (FormStatus)dbRecord.FormStatusId;
        TaskInfo task = null;
        if (status is not FormStatus.Unsubmitted and not FormStatus.Completed)
        {
            task = new TaskInfo
            {
                AllFormsId = dbRecord.AllFormsId,
                FormInfoId = dbRecord.FormInfoId,
                FormOwnerEmail = dbRecord.FormOwnerEmail,
                AssignedTo = dbRecord.NextApprover,
                TaskStatus = formStatus,
                TaskCreatedBy = "System",
                TaskCreatedDate = DateTime.Now,
                ActiveRecord = dbRecord.ActiveRecord,
                SpecialReminderDate = dbRecord.NextApprover is "!DOT Employee Services Officer Group" && formType is FormType.LcR_PLA && formType is FormType.LcR_DSA ? null : DateTime.Today.AddDays(3),
                SpecialReminderTo = dbRecord.NextApprover,
                EscalationDate = DateTime.Today.AddDays(5),
                Escalation = true,
                ReminderTo = dbRecord.NextApprover,
                SpecialReminder = true
            };
        }

        await _taskManager.AddFormTaskAsync(dbRecord.FormItemId, task);
    }
}