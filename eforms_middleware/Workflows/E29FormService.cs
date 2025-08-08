using System;
using System.Linq;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.Constants.E29;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Specifications;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace eforms_middleware.GetMasterData
{
    public class E29FormService : FormServiceBase
    {
        private readonly IValidator<E29Form> _validator;
        private readonly IRepository<FormPermission> _formPermissionRepo;

        public E29FormService(ILogger<E29FormService> log, ITaskManager taskManager,
            IRepository<RefFormStatus> formStatusRepository, IFormHistoryService formHistoryService,
            IValidator<E29Form> validator, IFormInfoService formInfoService,
            IMessageFactoryService messageFactoryService, IEmployeeService employeeService,
            IRequestingUserProvider requestingUserProvider, IPermissionManager permissionManager,
            IRepository<FormPermission> formPermissionRepo, IPositionService positionService) : base(log, taskManager,
            formStatusRepository, formHistoryService, formInfoService,
            messageFactoryService, employeeService, requestingUserProvider, permissionManager, positionService)
        {
            _validator = validator;
            _formPermissionRepo = formPermissionRepo;
        }

        protected override string GetValidSerializedModel()
        {
            var requestForm = JsonConvert.DeserializeObject<E29Form>(Request.FormDetails.Response);
            return JsonConvert.SerializeObject(requestForm);
        }

        protected override async Task<bool> NextStateAvailable()
        {
            var originalStatus = (FormStatus)DbRecord.FormStatusId;
            var requestedStatus = Enum.Parse<FormStatus>(Request.FormAction);
            var spec = new GetHistoryByActionAndFormIdDescending(DbRecord.FormInfoId,
                Enum.GetName(typeof(FormStatus), FormStatus.Delegate));
            var canDelegate = await FormHistoryService.GetFirstOrDefaultHistoryBySpecification(spec);
            switch (originalStatus)
            {
                case FormStatus.Unsubmitted:
                    return requestedStatus == originalStatus || requestedStatus == FormStatus.Submitted ||
                           (requestedStatus == FormStatus.Delegate && canDelegate == null);
                case FormStatus.Submitted:
                    return requestedStatus == FormStatus.Rejected || requestedStatus == FormStatus.Completed;
                default:
                    return false;
            }
        }

        protected override Task<bool> CanCreate()
        {
            // Users cannot create E29 forms
            return Task.FromResult(false);
        }

        protected override async Task<ValidationResult> ValidateInternal()
        {
            var state = Enum.Parse<FormStatus>(Request.FormAction);
            var result = await ValidateFormData(state == FormStatus.Submitted);

            return result;
        }

        protected override Task SubmitInternalAsync()
        {
            base.SubmitInternalAsync();
            SetNextApprover();
            return Task.CompletedTask;
        }

        private async Task<ValidationResult> ValidateFormData(bool clearRejectionReason = false)
        {
            var originalForm = JsonConvert.DeserializeObject<E29Form>(DbRecord.Response);
            var predecessorForm = new E29Form();
            if (originalForm != null && originalForm.PreviousMonthFormId.HasValue)
            {
                var predecessor =
                    await FormInfoService.GetFormInfoByIdAsync(originalForm.PreviousMonthFormId.Value);
                predecessorForm = JsonConvert.DeserializeObject<E29Form>(predecessor.Response);
            }
            var context = new ValidationContext<E29Form>(originalForm);
            context.RootContextData["FormHistory"] = predecessorForm;

            var validatedForm = _validator.Validate(context);
            if (validatedForm.IsValid)
            {
                var users = originalForm!.Users;
                var requestStatus = Enum.Parse<FormStatus>(Request.FormAction);
                if (requestStatus == FormStatus.Submitted)
                {
                    users = users.Where(u => u.RemovedReason != (int)TrelisRemovalReason.AddedInError).ToList();
                }

                originalForm.Users = users;
                originalForm.RejectionReason = clearRejectionReason ? null : originalForm.RejectionReason;
            }

            return validatedForm;
        }

        protected override async Task<string> Delegate(string email)
        {
            if (DbRecord.FormOwnerEmail == null)
            {
                var delegatedEmployee = await EmployeeService.GetEmployeeDetailsAsync(email);
                DbRecord.FormOwnerEmail = delegatedEmployee.EmployeeEmail;
                DbRecord.FormOwnerName = string.Join(' ', delegatedEmployee.EmployeeFirstName,
                    delegatedEmployee.EmployeeSurname);
                DbRecord.FormOwnerEmployeeNo = delegatedEmployee.EmployeeNumber;
                DbRecord.FormOwnerPositionNo = delegatedEmployee.EmployeeNumber;
                DbRecord.FormOwnerPositionTitle = delegatedEmployee.EmployeePositionTitle;
                return delegatedEmployee.EmployeeEmail;
            }
            return await base.Delegate(email);
        }

        protected override async Task Reject()
        {
            var spec = new GetHistoryByActionAndFormIdDescending(DbRecord.FormInfoId,
                Enum.GetName(typeof(FormStatus), FormStatus.Delegate));
            var wasDelegated = await FormHistoryService.GetFirstOrDefaultHistoryBySpecification(spec);
            if (wasDelegated == null)
            {
                await base.Reject();
            }
            else
            {
                DbRecord.FormSubStatus = nameof(FormStatus.Unsubmitted);
                DbRecord.FormStatusId = (int)FormStatus.Unsubmitted;
                var ownerSpecification = new FormPermissionSpecification(formId: DbRecord.FormInfoId, filterOwner: true, addPositionInfo: true);
                var permission = await _formPermissionRepo.SingleOrDefaultAsync(ownerSpecification);
                DbRecord.NextApprover = permission.Position.AdfUserPositions.Count == 1
                    ? permission.Position.AdfUserPositions.Single().EmployeeEmail
                    : permission.Position.PositionTitle;
                PermissionsToAdd.Add(new FormPermission
                    { PositionId = permission.PositionId, PermissionFlag = (byte)PermissionFlag.UserActionable });
            }
            var originalForm = JsonConvert.DeserializeObject<E29Form>(DbRecord.Response);
            var requestForm = JsonConvert.DeserializeObject<E29Form>(Request.FormDetails.Response);
            originalForm!.RejectionReason = requestForm!.RejectionReason;
            DbRecord.Response = JsonConvert.SerializeObject(originalForm);
        }

        protected override string RejectionReason =>
            JsonConvert.DeserializeObject<E29Form>(Request.FormDetails.Response)!.RejectionReason;

        protected override async Task CompletedAsync()
        {
            await base.CompletedAsync();
        }

        private void SetNextApprover()
        {
            DbRecord.NextApprover = E29Constants.TrelisAccessManagementGroupMail;
            PermissionsToAdd.Add(new FormPermission
            {
                GroupId = E29Constants.TRELIS_ACCESS_MANAGEMENT_ID, PermissionFlag = (byte)PermissionFlag.UserActionable
            });
            PermissionsToAdd.Add(new FormPermission
                { UserId = RequestingUser.ActiveDirectoryId, PermissionFlag = (byte)PermissionFlag.View });
        }

        protected override async Task SendEmailAsync()
        {
            if (Request.FormAction == nameof(FormStatus.Unsubmitted)) return;
            await base.SendEmailAsync();
        }
    }
}