using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure.Interfaces;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Specifications;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eforms_middleware.GetMasterData
{
    public abstract class FormServiceBase
    {
        protected readonly ILogger<FormServiceBase> Log;
        private readonly ITaskManager _taskManager;
        protected FormInfo DbRecord;
        protected FormInfoUpdate Request;
        protected readonly IFormHistoryService FormHistoryService;
        protected readonly IFormInfoService FormInfoService;
        protected readonly IEmployeeService EmployeeService;
        private readonly IRequestingUserProvider _requestingUserProvider;
        protected readonly IPermissionManager PermissionManager;
        protected readonly IPositionService PositionService;
        private readonly IMessageFactoryService _messageFactoryService;
        private List<RefFormStatus> _statuses;
        private FormInfo _copy;
        private DateTime _now;
        private readonly IRepository<RefFormStatus> _formStatusRepository;
        private TaskInfo _taskInfo;
        protected IUserInfo RequestingUser;
        protected List<FormPermission> PermissionsToAdd = new();
        protected bool IsCoiHrForm
        {
            get
            {
                return Request.FormDetails.AllFormsId is (int)FormType.CoI_CPR
                           or (int)FormType.CoI_GBC or (int)FormType.CoI_SE or (int)FormType.CoI_REC;
            }
        }

        protected bool IsCoiOther
        {
            get
            {
                return Request.FormDetails.AllFormsId is (int)FormType.CoI_Other;
            }
        }

        protected bool IsCoiDogForm
        {
            get
            {
                return Request.FormDetails.AllFormsId is (int)FormType.CoI_GBH
                            || IsCoiOther;

            }
        }
        protected bool IsFormOwnerEdODG { get; set; }
        protected bool IsFormOwnerReportToT1 { get; set; }


        protected FormServiceBase(ILogger<FormServiceBase> log, ITaskManager taskManager,
            IRepository<RefFormStatus> formStatusRepository, IFormHistoryService formHistoryService,
            IFormInfoService formInfoService, IMessageFactoryService messageFactoryService, IEmployeeService employeeService,
            IRequestingUserProvider requestingUserProvider, IPermissionManager permissionManager, IPositionService positionService)
        {
            Log = log;
            _taskManager = taskManager;
            _formStatusRepository = formStatusRepository;
            FormHistoryService = formHistoryService;
            FormInfoService = formInfoService;
            _messageFactoryService = messageFactoryService;
            EmployeeService = employeeService;
            _requestingUserProvider = requestingUserProvider;
            PermissionManager = permissionManager;
            PositionService = positionService;
        }

        public async Task<RequestResult> ProcessRequest(FormInfoUpdate request)
        {
            Request = request;

            DbRecord = await FormInfoService.GetExistingOrNewFormInfoAsync(request, addAttachments: true);
            _copy = DbRecord?.ShallowCopy();
            _statuses = await _formStatusRepository.ListAsync();
            _now = DateTime.Now;
            RequestingUser = await _requestingUserProvider.GetRequestingUser();
            var actioningGroup = DbRecord.FormPermissions.SingleOrDefault(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable)?.GroupId;
            string additionalComments = null;
            var isRequestOnBehalf = false;
            int AssignToPositionId = 0;
            IsFormOwnerEdODG = RequestingUser != null && RequestingUser.EmployeePositionId == ConflictOfInterest.ED_ODG_POSITION_ID;
            if (DbRecord.FormOwnerEmail != null)
            {
                var formOwmer = await EmployeeService.GetEmployeeDetailsAsync(DbRecord.FormOwnerEmail);
                IsFormOwnerEdODG = formOwmer != null && formOwmer.EmployeePositionId == ConflictOfInterest.ED_ODG_POSITION_ID;
                IsFormOwnerReportToT1 = formOwmer != null && formOwmer.Managers.Any(x => x.EmployeeManagementTier is 1);
            }

            if ((request.FormDetails.AllFormsId is (int)FormType.CoI_Other or (int)FormType.CoI_CPR
                or (int)FormType.CoI_GBC) && request.FormDetails.Response != null)
            {
                var formInfoInsertModel = JsonConvert.DeserializeObject<FormInfoInsertModel>(request.FormDetails.Response);
                if (formInfoInsertModel != null && formInfoInsertModel.AdditionalComments != null)
                {
                    additionalComments = formInfoInsertModel.AdditionalComments;
                }
            }

            var CoiTypeValidation = new
            {
                IsCoiOther = request.FormDetails.AllFormsId is (int)FormType.CoI_Other,
                IsCoiBoardCommittee = request.FormDetails.AllFormsId is (int)FormType.CoI_GBC,
                IsCoiClosePeronalRelationShip = request.FormDetails.AllFormsId is (int)FormType.CoI_CPR,
                IsFormDetailResponseNotNull = request.FormDetails.Response != null
            };

            switch (CoiTypeValidation)
            {
                case { IsCoiBoardCommittee: true, IsFormDetailResponseNotNull: true }:
                    {
                        var model = JsonConvert.DeserializeObject<GbcEmployeeForm>(request.FormDetails.Response);
                        isRequestOnBehalf = model != null && model.IsRequestOnBehalf;
                        break;
                    }
                case { IsCoiClosePeronalRelationShip: true, IsFormDetailResponseNotNull: true }:
                    {
                        var model = request.FormDetails.Response != null ?
                                     JsonConvert.DeserializeObject<CprEmployeeForm>(request.FormDetails.Response) : null;
                        isRequestOnBehalf = model != null && model.IsRequestOnBehalf;
                        break;
                    }
                case { IsCoiOther: true, IsFormDetailResponseNotNull: true }:
                    {
                        var model = JsonConvert.DeserializeObject<CoIOther>(request.FormDetails.Response);
                        isRequestOnBehalf = model != null && model.IsRequestOnBehalf;
                        break;
                    }
            }

            var requestorValidation = new
            {
                IsHRForms = IsCoiHrForm,
                IsODGForms = IsCoiDogForm
            };
            if (isRequestOnBehalf)
            {
                switch (requestorValidation)
                {
                    case { IsHRForms: true }:
                        {
                            var isUserPodGroupMember = await EmployeeService.IsPodGroupUser(RequestingUser.ActiveDirectoryId);
                            AssignToPositionId = ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID;
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
                            break;

                        }
                    case { IsODGForms: true }:
                        {
                            var IsOdgGroupMember = await EmployeeService.IsOdgGroupUser(RequestingUser.ActiveDirectoryId);
                            AssignToPositionId = ConflictOfInterest.ED_ODG_POSITION_ID;
                            if (!IsOdgGroupMember)
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
                            break;
                        }

                }

            }

            var createValidationResult = Request.FormDetails.FormInfoId != null || await CanCreate();
            if (!createValidationResult)
            {
                var failedValidation =
                    new ValidationFailure("Authorization", "You do not have access to create this form.");
                return RequestResult.FailedRequest(StatusCodes.Status403Forbidden,
                    new[] { failedValidation });
            }

            var nextStateValid = await NextStateAvailable();
            if (!nextStateValid)
            {
                Log.LogInformation("User attempted to perform action {FormAction} when in state {FormStatus} which is not available",
                    request.FormAction, DbRecord!.FormStatusId);
                return RequestResult.FailedRequest(StatusCodes.Status422UnprocessableEntity,
                    new[] { new { Message = $"{Request.FormAction} not available" } });
            }
            var formValidationResult = await ValidateAsync();
            if (!formValidationResult.Success) return formValidationResult;

            HandleAttachments();
            var state = Enum.Parse<FormStatus>(request.FormAction);
            try
            {
                switch (state)
                {
                    case FormStatus.Unsubmitted:
                        await SaveInternalAsync();
                        break;
                    case FormStatus.Submitted:
                        {
                            if (!isRequestOnBehalf)
                            {

                                await SubmitInternalAsync();
                            }
                            else
                            {
                                await OnBehalfRequestAutoApprove(AssignToPositionId);
                            }
                            break;
                        }
                    case FormStatus.Delegate:
                        DbRecord!.NextApprover = await Delegate(request.FormDetails.NextApprover);
                        break;
                    case FormStatus.Rejected:
                        await Reject();
                        break;
                    case FormStatus.Approved:
                        await Approve();
                        break;
                    case FormStatus.IndependentReviewCompleted:
                        await EndorseAsync();
                        break;
                    case FormStatus.Endorsed:
                        await EndorseAsync();
                        break;
                    case FormStatus.Completed:
                        await CompletedAsync();
                        break;
                    case FormStatus.Recall:
                        var recallResult = await RecallAsync();
                        if (!recallResult.Success) return recallResult;
                        break;
                    case FormStatus.Delegated:
                        var employee = await EmployeeService.GetEmployeeByAzureIdAsync(Guid.Parse(Request.FormDetails.NextApprover));
                        if (employee == null || employee.EmployeePositionId == null)
                        {
                            return RequestResult.FailedRequest(404, "User not found");
                        }
                        PermissionsToAdd.Add(new FormPermission
                        {
                            UserId = employee.ActiveDirectoryId,
                            PermissionFlag = (byte)PermissionFlag.UserActionable
                        });
                        DbRecord.NextApprover = employee.EmployeeEmail;
                        DbRecord.FormStatusId = (int)FormStatus.Delegated;
                        DbRecord.FormSubStatus = Enum.GetName(FormStatus.Delegated);
                        break;
                    case FormStatus.IndependentReview:
                        var employeeInfo = await EmployeeService.GetEmployeeByAzureIdAsync(Guid.Parse(Request.FormDetails.NextApprover));
                        if (employeeInfo == null || employeeInfo.EmployeePositionId == null)
                        {
                            return RequestResult.FailedRequest(404, "User not found");
                        }
                        PermissionsToAdd.Add(new FormPermission
                        {
                            UserId = employeeInfo.ActiveDirectoryId,
                            PermissionFlag = (byte)PermissionFlag.UserActionable
                        });
                        DbRecord.NextApprover = employeeInfo.EmployeeEmail;
                        DbRecord.FormStatusId = (int)FormStatus.IndependentReview;
                        DbRecord.FormSubStatus = Enum.GetName(FormStatus.IndependentReview);
                        break;
                    case FormStatus.Attachment:
                        await SaveInternalAsync();
                        break;
                    default:
                        Log.LogInformation("User attempted to perform action {FormAction} which is not available",
                            request.FormAction);
                        return RequestResult.FailedRequest(StatusCodes.Status400BadRequest,
                            new { Message = "Invalid action requested." });
                }
                DbRecord = await FormInfoService.SaveFormInfoAsync(request, DbRecord);
                await WriteTask(DbRecord.FormInfoId);
                await PermissionManager.UpdateFormPermissionsAsync(DbRecord.FormInfoId, PermissionsToAdd);
                if (state != FormStatus.Attachment) // Do not add history for attachment action
                {
                    await FormHistoryService.AddFormHistoryAsync(request, DbRecord, actioningGroup, additionalComments,
                        state == FormStatus.Rejected ? RejectionReason : string.Empty);
                }
                await SendEmailAsync();

                return RequestResult.SuccessfulFormInfoRequest(DbRecord);
            }
            catch (Exception e)
            {
                Log.LogError(e, e.Message);
                // TODO this needs to be better but temporarily as this is the only expected exception this should work
                if (e.Message.Contains("Tier 3"))
                {
                    throw;
                }

                throw new Exception("An unexpected error occured. Please try again later.");
            }


        }

        protected virtual async Task SaveInternalAsync()
        {
            var requestingUser = await _requestingUserProvider.GetRequestingUser();
            PermissionsToAdd.Add(new FormPermission
            {
                UserId = requestingUser.ActiveDirectoryId,
                PermissionFlag = (byte)PermissionFlag.UserActionable,
                IsOwner = DbRecord.FormInfoId == 0
            });
            DbRecord.FormStatusId = (int)FormStatus.Unsubmitted;
            DbRecord.FormSubStatus = Enum.GetName(FormStatus.Unsubmitted);
            DbRecord.Response = GetValidSerializedModel();
        }

        protected abstract string GetValidSerializedModel();

        protected virtual Task SubmitInternalAsync()
        {
            if (DbRecord.FormInfoId == 0)
            {
                PermissionsToAdd.Add(new FormPermission
                {
                    UserId = RequestingUser.ActiveDirectoryId,
                    PermissionFlag = (byte)PermissionFlag.View,
                    IsOwner = true
                });
            }
            DbRecord.SubmittedDate = _now;
            DbRecord.FormStatusId = (int)FormStatus.Submitted;
            DbRecord.FormSubStatus = Enum.GetName(FormStatus.Submitted);
            DbRecord.Response = GetValidSerializedModel();
            return Task.CompletedTask;
        }

        protected abstract Task<ValidationResult> ValidateInternal();

        protected abstract Task<bool> NextStateAvailable();

        protected virtual async Task<string> Delegate(string email)
        {
            var delegateTo = await EmployeeService.GetEmployeeDetailsAsync(email);
            PermissionsToAdd.Add(new FormPermission
            { UserId = delegateTo.ActiveDirectoryId, PermissionFlag = (byte)PermissionFlag.UserActionable });
            return delegateTo.EmployeeEmail;
        }


        private async Task<RequestResult> RecallAsync()
        {
            var requestingUser = await _requestingUserProvider.GetRequestingUser();
            PermissionsToAdd.Add(new FormPermission
            { UserId = requestingUser.ActiveDirectoryId, PermissionFlag = (byte)PermissionFlag.UserActionable });
            DbRecord.NextApprover = null;
            DbRecord.FormStatusId = (int)FormStatus.Unsubmitted;
            return RequestResult.SuccessfulFormInfoRequest(DbRecord);
        }

        protected virtual Task Approve()
        {
            DbRecord.FormStatusId = (int)FormStatus.Approved;
            DbRecord.FormSubStatus = Enum.GetName(FormStatus.Approved);
            return Task.CompletedTask;
        }

        protected virtual Task EndorseAsync()
        {
            DbRecord.FormStatusId = (int)FormStatus.Endorsed;
            DbRecord.FormSubStatus = Enum.GetName(FormStatus.Endorsed);
            return Task.CompletedTask;
        }

        protected virtual async Task OnBehalfRequestAutoApprove(int AssignToPositionId)
        {
            DbRecord.SubmittedDate = _now;
            DbRecord.FormStatusId = (int)FormStatus.Endorsed;
            DbRecord.FormSubStatus = Enum.GetName(FormStatus.Endorsed);
            PermissionsToAdd.Add(new FormPermission((byte)PermissionFlag.UserActionable,
                positionId: AssignToPositionId, isOwner: false));
            PermissionsToAdd.Add(new FormPermission((byte)PermissionFlag.View,
                                userId: RequestingUser.ActiveDirectoryId, isOwner: true));
            var EdPcUserList = await EmployeeService.GetEmployeeByPositionNumberAsync(AssignToPositionId);
            DbRecord.Response = GetValidSerializedModel();
            DbRecord.NextApprover = EdPcUserList.GetDescription();
        }

        protected virtual Task CompletedAsync()
        {
            PermissionsToAdd.Add(new FormPermission { FormId = DbRecord.FormInfoId, PermissionFlag = (byte)PermissionFlag.View });
            DbRecord.CompletedDate = _now;
            DbRecord.FormSubStatus = Enum.GetName(FormStatus.Completed);
            DbRecord.FormStatusId = (int)FormStatus.Completed;
            DbRecord.NextApprover = null;
            return Task.CompletedTask;
        }

        protected virtual async Task Reject()
        {
            DbRecord.FormSubStatus = nameof(FormStatus.Unsubmitted);
            DbRecord.FormStatusId = (int)FormStatus.Unsubmitted;
            var spec = new GetHistoryByActionAndFormIdDescending(DbRecord.FormInfoId,
                Enum.GetName(typeof(FormStatus), FormStatus.Submitted));
            var oldHistory = await FormHistoryService.GetFirstOrDefaultHistoryBySpecification(spec);
            var user = await EmployeeService.GetEmployeeDetailsAsync(oldHistory.ActionBy);
            PermissionsToAdd.Add(new FormPermission
            { UserId = user.ActiveDirectoryId, PermissionFlag = (byte)PermissionFlag.UserActionable });
            DbRecord.NextApprover = oldHistory.ActionBy == DbRecord.FormOwnerEmail ? null : oldHistory.ActionBy;
        }

        protected virtual string RejectionReason => string.Empty;

        protected virtual async Task SendEmailAsync()
        {
            await _messageFactoryService.SendEmailAsync(DbRecord, Request);
        }

        protected virtual Task<bool> CanCreate()
        {
            return Task.FromResult(true);
        }

        protected void SetTaskInfo(string assignedTo, int reminderFrequency, int remindersCount,
            string reminderTo,
            string specialReminderTo, DateTime? specialReminderDate, DateTime? escalationDate)
        {
            _taskInfo = new TaskInfo
            {
                AssignedTo = assignedTo,
                TaskStatus = Request.FormAction,
                ReminderFrequency = reminderFrequency,
                RemindersCount = remindersCount,
                TaskCreatedBy = Request.UserId,
                TaskCreatedDate = _now,
                ReminderTo = reminderTo,
                SpecialReminderTo = specialReminderTo,
                SpecialReminderDate = specialReminderDate,
                SpecialReminder = specialReminderDate.HasValue,
                Escalation = escalationDate.HasValue,
                EscalationDate = escalationDate
            };
        }

        protected virtual IList<AttachmentResult> ReturnAttachments()
        {
            return null;
        }

        private void HandleAttachments()
        {
            var attachments = ReturnAttachments();
            if ((attachments?.Count ?? 0) == 0) return;
            var updatedAttachments = new List<FormAttachment>();
            var actioningPermission =
                DbRecord.FormPermissions.Single(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);
            var attachmentsToDeactivate =
                DbRecord.FormAttachments.Where(a => !attachments.Any() || attachments.All(x => a.Id != x.Id)).Select(
                    a =>
                    {
                        if (a.ActiveRecord && a.PermissionId == actioningPermission.Id) a.ActiveRecord = false;
                        return a;
                    }).ToList();
            var attachmentsToActivate =
                DbRecord.FormAttachments.Where(a => attachments.Any(x => a.Id == x.Id)).Select(
                    a =>
                    {
                        if (!a.ActiveRecord && a.PermissionId == actioningPermission.Id) a.ActiveRecord = true;
                        return a;
                    }).ToList();
            updatedAttachments.AddRange(attachmentsToActivate);
            updatedAttachments.AddRange(attachmentsToDeactivate);
            DbRecord.FormAttachments = updatedAttachments;
        }

        private async Task WriteTask(int formInfoId)
        {
            if (_taskInfo != null)
            {
                _taskInfo.AllFormsId = DbRecord.AllFormsId;
                _taskInfo.FormInfoId = DbRecord.FormInfoId;
                _taskInfo.FormOwnerEmail = DbRecord.FormOwnerEmail;
                _taskInfo.ActiveRecord = DbRecord.ActiveRecord;
            }

            await _taskManager.AddFormTaskAsync(formInfoId, _taskInfo);
        }

        private async Task<RequestResult> ValidateAsync()
        {
            var validationResult = await ValidateInternal();
            if (validationResult.IsValid) return RequestResult.SuccessfulFormInfoRequest(new FormInfo());
            Log.LogInformation("{RequestingUser} attempted to submit an invalid form {ValidationResult}",
                RequestingUser, validationResult);
            return RequestResult.FailedRequest(StatusCodes.Status422UnprocessableEntity,
                validationResult.Errors);

        }
    }
}