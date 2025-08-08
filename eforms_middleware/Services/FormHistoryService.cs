using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Specifications;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eforms_middleware.Services
{
    public class FormHistoryService : IFormHistoryService
    {
        private readonly ILogger<FormHistoryService> _log;
        private readonly IRepository<FormHistory> _formHistoryRepository;
        private readonly IRequestingUserProvider _requestingUserProvider;
        private readonly IPermissionManager _permissionManager;
        private readonly IFormInfoService _formInfoService;

        public FormHistoryService(ILogger<FormHistoryService> log,
            IRepository<FormHistory> formHistoryRepository,
            IRequestingUserProvider requestingUserProvider,
            IPermissionManager permissionManager,
            IFormInfoService formInfoService)
        {
            _log = log;
            _formHistoryRepository = formHistoryRepository;
            _requestingUserProvider = requestingUserProvider;
            _permissionManager = permissionManager;
            _formInfoService = formInfoService;
        }

        public async Task<List<FormHistoryDto>> GetFormHistoryDetailsByID(int formInfoId)
        {
            var specification = new FormHistorySpecification(formId: formInfoId);
            var formsHistory = await _formHistoryRepository.ListAsync(specification);
            if (!formsHistory.Any()) return new List<FormHistoryDto>();
            var formInfo = await _formInfoService
                                .GetFormInfoByIdAsync(formInfoId).ConfigureAwait(false);
            var result = formsHistory.Select(x =>
            {
                var actionType = x.GroupName.IsNullOrEmpty()
                    ? $"{x.ActionType} by {x.ActionBy}"
                    : $"{x.ActionType} by {x.ActionBy} on behalf of {x.GroupName}";
                return new FormHistoryDto
                {
                    AllFormsID = x.AllFormsId,
                    FormStatus = Enum.GetName(typeof(FormStatus), x.FormStatusId),
                    FormHistoryID = x.FormHistoryId,
                    FormInfoID = x.FormInfoId,
                    Created = x.Created,
                    ActionType = actionType,
                    ActionBy = x.ActionBy,
                    AdditionalComments = x.AditionalComments,
                    RejectedReason = x.RejectedReason,
                    ActionByPosition = x.ActionByPosition,
                    FormOwner = formInfo?.FormOwnerName,
                    FormOwnerEmail = formInfo?.FormOwnerEmail

                };
            }).ToList();
            return result;
        }

        public async Task<FormHistory> GetFirstOrDefaultHistoryBySpecification(
            ISpecification<FormHistory> specification)
        {
            return await _formHistoryRepository.FirstOrDefaultAsync(specification);
        }

        public async Task AddFormHistoryAsync(FormInfoUpdate request, FormInfo form, Guid? actioningGroup, string additionalInformation, string ReasonForDecision = "")
        {
            if (form.FormStatusId == (int)FormStatus.Attachment) return;
            // Form permission coming in already updated
            var requestingUser = await _requestingUserProvider.GetRequestingUser();
            var actionedBy = requestingUser?.EmployeeEmail ?? "System";
            var specification = new FormPermissionSpecification(formId: form.FormInfoId,
                addGroupMemberInfo: true, addPositionInfo: true,
                addUserInfo: true);
            var permissions = (await _permissionManager.GetPermissionsBySpecificationAsync(specification)).ToList();
            var nextApprover =
                permissions.SingleOrDefault(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);
            var groupName = permissions.SingleOrDefault(x => actioningGroup.HasValue && x.GroupId == actioningGroup)?.Group?.GroupName;
            var newHistory = new FormHistory
            {
                Created = form.Modified,
                ActionBy = actionedBy,
                ActionType = GetHistoryAction(request, nextApprover),
                ActiveRecord = true,
                AditionalComments = additionalInformation,
                AllFormsId = form.AllFormsId,
                FormInfoId = form.FormInfoId,
                FormStatusId = form.FormStatusId,
                GroupName = groupName,
                ActionByPosition = requestingUser?.EmployeePositionTitle,
                RejectedReason = ReasonForDecision
            };
            await _formHistoryRepository.AddAsync(newHistory);
        }

        private static string GetHistoryAction(FormInfoUpdate request, FormPermission nextApprover)
        {
            var action = Enum.Parse<FormStatus>(request.FormAction);
            return action switch
            {
                FormStatus.Unsubmitted when !nextApprover.IsOwner => "Cancelled",
                FormStatus.Unsubmitted => "Saved",
                FormStatus.Delegate => $"Delegated to {nextApprover.User.EmployeeEmail}",
                FormStatus.Recall => "Recalled",
                FormStatus.Submitted => "Submitted",
                FormStatus.Escalated => $"Escalated to {nextApprover.PermissionActionerDescription}",
                FormStatus.Cancelled => $"Cancelled",
                _ => request.FormAction
            };
        }
    }
}