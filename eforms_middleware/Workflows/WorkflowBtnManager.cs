using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure;
using eforms_middleware.Interfaces;
using System.Threading.Tasks;
using eforms_middleware.DataModel;
using System.Collections.Generic;
using eforms_middleware.Constants;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace eforms_middleware.Workflows
{
    public class WorkflowBtnManager : BaseApprovalService
    {
        public WorkflowBtnManager(IFormEmailService formEmailService
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

        }


        private async Task<StatusBtnModel> GetOwnerBtn(int formId
            , string userEmail)
        {
            var permissionData = await _permissionManager?.GetAllPermission(formId);
            var formOwner = await GetAdfUserByEmail(userEmail);
            var formInfo = await _formInfoRepository.FirstOrDefaultAsync(x => x.FormInfoId == formId);

            var dt = permissionData.Find(x => x.IsOwner
               && x.FormId == formId
               && x.UserId == formOwner.ActiveDirectoryId);

            if (formInfo.FormStatusId == (int)FormStatus.Unsubmitted
                || formInfo.FormStatusId == (int)FormStatus.Completed
                 || dt == null)
            {
                return null;
            }

            var btnData = new List<WorkflowBtn>();
            return new StatusBtnModel()
            {
                BtnText = FormStatus.Recall.ToString(),
                FormSubStatus = FormStatus.Recalled.ToString(),
                StatusId = (int)FormStatus.Unsubmitted
            };
        }

        private async Task<List<StatusBtnModel>> SetBtn(List<WorkflowBtn> btnData
            , int formId
           , string userEmail)
        {
            var statusBtnList = new List<StatusBtnModel>();

            foreach (var btn in btnData)
            {
                statusBtnList.Add(new StatusBtnModel()
                {
                    BtnText = btn.BtnText,
                    FormSubStatus = btn.FormSubStatus,
                    StatusId = btn.StatusId,
                    IsReject = FormStatus.Reject.ToString() == btn.BtnText,
                    IsUserDialog = (int)FormStatus.Delegated == btn.StatusId ||
                                    (int)FormStatus.IndependentReview == btn.StatusId
                });
            }

            var userBtn = await GetOwnerBtn(formId, userEmail);
            if (userBtn != null)
            {
                statusBtnList.Add(userBtn);
            }
            else
            {
                var delegateBtn = await GetDelegateForGroup(formId, userEmail);
                if (delegateBtn != null && !statusBtnList.Where(x=> x.BtnText == FormStatus.Delegate.ToString()).Any())
                {
                    statusBtnList.Add(delegateBtn);
                }
            }

            statusBtnList.Add(new StatusBtnModel()
            {
                BtnText = "Cancel",
                FormSubStatus = FormStatus.Cancelled.ToString(),
                StatusId = (int)FormStatus.Cancelled
            });

            return statusBtnList;
        }

        public async Task<StatusBtnModel> GetDelegateForGroup(int formId, string userEmail)
        {
            var permission = await _permissionManager.GetAllPermission(formId);
            var group = await GetAdfGroupByMemberEmail(userEmail);
            var formInfo = await _formInfoRepository.FirstOrDefaultAsync(x => x.FormInfoId == formId);

            if (formInfo.FormStatusId == (int)FormStatus.Completed || formInfo.FormStatusId == (int)FormStatus.Unsubmitted) return null;

            foreach (var perm in permission)
            {
                if (group.Where(x => x.Id == perm.GroupId).Any())
                {
                    var wrkBtn = await WorkflowBtnRepository.FirstOrDefaultAsync(x => x.PermisionId == perm.Id
                    && (x.IsActive ?? false)
                    && x.StatusId == (int)FormStatus.Delegated);
                    if (wrkBtn != null)
                    {
                        return new StatusBtnModel()
                        {
                            BtnText = wrkBtn.BtnText,
                            FormSubStatus = wrkBtn.FormSubStatus,
                            HasDeclaration = wrkBtn.HasDeclaration ?? false,
                            IsReject = false,
                            IsUserDialog = true,
                            StatusId = wrkBtn.StatusId
                        };
                    }
                }
            }

            return null;
        }

        public async Task<List<StatusBtnModel>> GetBtnForUser(int formId
            , string userEmail)
        {
            var permissionData = await _permissionManager?.GetActionPermission(formId);

            var group = await GetAdfGroupByMemberEmail(userEmail);
            var position = await GetAdfUserByEmail(userEmail);
            var permissions = new List<FormPermission>();
            var btnData = new List<WorkflowBtn>();

            foreach (var permission in permissionData)
            {
                if ((permission.GroupId != null && group.Where(x => x.Id == permission.GroupId).Any())
                    || (permission.PositionId != null && permission.PositionId == position?.EmployeePositionId))
                {
                    btnData.AddRange(await WorkflowBtnRepository.FindByAsync(x => x.PermisionId == permission.Id && (x.IsActive ?? false)));
                }
            }
            return await SetBtn(btnData, formId, userEmail);
        }
    }
}
