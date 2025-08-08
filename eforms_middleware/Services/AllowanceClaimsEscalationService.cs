using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Specifications;
using eforms_middleware.Workflows;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace eforms_middleware.Services
{
    public class AllowanceClaimsEscalationService : EscalationManagerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly IPermissionManager _permissionManager;

        public AllowanceClaimsEscalationService(IEmployeeService employeeService, IPermissionManager permissionManager)
        {
            _employeeService = employeeService;
            _permissionManager = permissionManager;
        }

        public async override Task<EscalationResult> EscalateFormAsync(FormInfo originalForm)
        {
            try
            {
                var statusBtnData = new StatusBtnData();
                bool doesEscalate = true;
                FormPermission permission = null;
                var specification = new FormPermissionSpecification(formId: originalForm.FormInfoId
                    , addPositionInfo: true
                    , addUserInfo: true);
                var permissions = await _permissionManager.GetPermissionsBySpecificationAsync(specification);
                var approvalPermission = permissions.Single(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);
                var approver = await _employeeService.GetEmployeeByEmailAsync(approvalPermission.Email);
                if (approver.EmployeeManagementTier > 3)
                {
                    var nextApprover = approver.Managers.Any() 
                                    ? approver?.Managers.FirstOrDefault() 
                                    : approver?.ExecutiveDirectors.FirstOrDefault();

                    //should not escalate beyond tier 4
                    doesEscalate = nextApprover.EmployeeManagementTier > 4;

                    permission = new()
                    {
                        PositionId = nextApprover.EmployeePositionId,
                        PermissionFlag = (byte)PermissionFlag.UserActionable
                    };

                    statusBtnData.StatusBtnModel =
                        new List<StatusBtnModel>
                                            {
                                                new StatusBtnModel(){
                                                    StatusId = (int)FormStatus.Approved,
                                                    BtnText = FormStatus.Approve.ToString(),
                                                    FormSubStatus = FormStatus.Approved.ToString(),
                                                    HasDeclaration = true
                                                },
                                                new StatusBtnModel(){
                                                    StatusId = (int)FormStatus.Unsubmitted,
                                                    BtnText = FormStatus.Reject.ToString(),
                                                    FormSubStatus = FormStatus.Rejected.ToString(),
                                                    HasDeclaration = false
                                                }
                                            };

                    originalForm.FormSubStatus = FormStatus.Escalated.ToString();
                    originalForm.NextApprover = nextApprover.EmployeeEmail;
                    originalForm.NextApprovalLevel = nextApprover.EmployeePositionTitle;
                }
                else
                {
                    permission = new FormPermission((byte)PermissionFlag.UserActionable, groupId: AllowanceAndClaims.POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID);
                    originalForm.NextApprover = AllowanceAndClaims.POD_EFFORMS_BUSINESS_ADMIN_GROUP_EMAIL;
                    originalForm.NextApprovalLevel = AllowanceAndClaims.POD_EFFORMS_BUSINESS_ADMIN_GROUP_NAME;
                    new List<StatusBtnModel>
                                            {
                                                new StatusBtnModel(){
                                                    StatusId = (int)FormStatus.Approved,
                                                    BtnText = FormStatus.Approve.ToString(),
                                                    FormSubStatus = FormStatus.Approved.ToString(),
                                                    HasDeclaration = true
                                                },
                                                new StatusBtnModel(){
                                                    StatusId = (int)FormStatus.Unsubmitted,
                                                    BtnText = FormStatus.Reject.ToString(),
                                                    FormSubStatus = FormStatus.Rejected.ToString(),
                                                    HasDeclaration = false
                                                }
                                            };
                }
                return new EscalationResult()
                {
                    UpdatedForm = originalForm,
                    DoesEscalate = doesEscalate,
                    NotifyDays = 2,
                    EscalationDays = 3,
                    PermissionUpdate = permission,
                    StatusBtnData = statusBtnData
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
