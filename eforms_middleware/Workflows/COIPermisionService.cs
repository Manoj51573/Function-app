using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure;
using eforms_middleware.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using eforms_middleware.Constants;
using static eforms_middleware.Settings.Helper;
using eforms_middleware.DataModel;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace eforms_middleware.Workflows
{
    public class COIPermisionService : BaseApprovalService
    {
        private readonly IRepository<FormInfo> _formInfo;


        public COIPermisionService(IFormEmailService formEmailService
            , IPermissionManager permissionManager
            , IRepository<AdfGroup> adfGroup
            , IRepository<AdfPosition> adfPosition
            , IRepository<AdfUser> adfUser
            , IRepository<AdfGroupMember> adfGroupMember
            , IRepository<FormInfo> formInfo
            , IRepository<FormInfo> formInfoRepository
            , IRepository<RefFormStatus> formRefStatus
            , IRepository<FormHistory> formHistoryRepository
            , IFormInfoService formInfoService
            , IConfiguration configuration
            , ITaskManager taskManager
            , IFormHistoryService formHistoryService
            , IRequestingUserProvider requestingUserProvider
            , IEmployeeService employeeService
            , IRepository<WorkflowBtn> workflowBtnRepository) : base(formEmailService
            , adfGroup
            , adfPosition
            , adfUser
            , adfGroupMember
            , formInfoRepository
            , formRefStatus
            , formHistoryRepository
            , permissionManager, formInfoService, configuration, taskManager, formHistoryService, requestingUserProvider, employeeService, workflowBtnRepository)
        {

            _formInfo = formInfo;
        }

        public async Task SetPermission(int formInfoId)
        {
            var formInfo = await _formInfo.FirstOrDefaultAsync(x => x.FormInfoId == formInfoId);
            var group = await GetAdfGroupByEmail(formInfo.NextApprover);
            var dt = new List<FormPermission>();            
            var formOwnerData = await GetAdfUserByEmail(formInfo.FormOwnerEmail);

            //Provide view right to talent team
            //if the form is completed and has no conflict
            if (formInfo.AllFormsId == (int)FormType.CoI_REC
                && formInfo.FormStatusId == (int)FormStatus.Completed)
            {
                var recruitmentModel = JsonConvert.DeserializeObject<RecruitmentModel>(formInfo.Response);
                if (recruitmentModel.HasConflictOfInterest == "No")
                {
                    dt.Add(new FormPermission
                    {
                        FormId = formInfoId,
                        PermissionFlag = (byte)PermissionFlag.View,
                        IsOwner = false,
                        PositionId = null,
                        GroupId = ConflictOfInterest.TALENT_TEAM_GROUP_ID,
                        UserId = null
                    });

                    dt.Add(new FormPermission
                    {
                        FormId = formInfoId,
                        PermissionFlag = (byte)PermissionFlag.View,
                        IsOwner = true,
                        PositionId = null,
                        GroupId = null,
                        UserId = formOwnerData?.ActiveDirectoryId
                    });
                }
            }

            //Provide view right form Admin as soon as the forms get submitted
            if ((formInfo.AllFormsId == (int)COIFormType.Recruitment
                    || formInfo.AllFormsId == (int)COIFormType.SecondaryEmployment)
                    && formInfo.FormStatusId == (int)FormStatus.Submitted)
            {
                dt.Add(new FormPermission
                {
                    FormId = formInfoId,
                    PermissionFlag = (byte)PermissionFlag.View,
                    IsOwner = false,
                    PositionId = null,
                    GroupId = ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID,
                    UserId = null
                });
            }

            if (formInfo.FormStatusId == (int)FormStatus.Unsubmitted || formInfo.FormStatusId == (int)FormStatus.Submitted)
            {
                dt.Add(new FormPermission
                {
                    FormId = formInfoId,
                    PermissionFlag = formInfo.FormStatusId == (int)FormStatus.Submitted
                                    ? (byte)PermissionFlag.View
                                    : (byte)PermissionFlag.UserActionable,
                    IsOwner = true,
                    PositionId = null,
                    GroupId = null,
                    UserId = formOwnerData?.ActiveDirectoryId
                });
            }

            if (!string.IsNullOrEmpty(formInfo.NextApprover))
            {
                var memberPosition = group?.Id == null ?
                    await GetAdfUserByEmail(formInfo.NextApprover)
                    : null;
                dt.Add(new FormPermission
                {
                    FormId = formInfoId,
                    PermissionFlag = formInfo.AllFormsId == (int)FormStatus.Completed
                                   ? (byte)PermissionFlag.View
                                   : (byte)PermissionFlag.UserActionable,
                    IsOwner = false,
                    PositionId = memberPosition?.EmployeePositionId,
                    GroupId = group?.Id,
                    UserId = null
                });
            }

            await _permissionManager.UpdateFormPermissionsAsync(formInfoId, dt);
        }
    }
}
