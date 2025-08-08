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
    public class WHSIREscalationService : EscalationManagerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly IPermissionManager _permissionManager;

        public WHSIREscalationService(IEmployeeService employeeService, IPermissionManager permissionManager, WHSIncidentReportApprovalService wHSIncidentReportApprovalService)
        {
            _employeeService = employeeService;
            _permissionManager = permissionManager;

        }

        public async override Task<EscalationResult> EscalateFormAsync(FormInfo originalForm)
        {
            try
            {
                var statusBtnData = new StatusBtnData();
                bool doesEscalate = false;
                FormPermission permission = null;
                var specification = new FormPermissionSpecification(formId: originalForm.FormInfoId
                    , addPositionInfo: true
                    , addUserInfo: true);
                var permissions = await _permissionManager.GetPermissionsBySpecificationAsync(specification);
                var approvalPermission = permissions.Single(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);
                var approver = await _employeeService.GetEmployeeByEmailAsync(approvalPermission.Email);

                var nextApprover = approver.Managers.Any()
                                    ? approver?.Managers.FirstOrDefault()
                                    : approver?.ExecutiveDirectors.FirstOrDefault();

                if (nextApprover.EmployeeManagementTier > 3)
                {
                    

                    //should not escalate beyond tier 4
                    doesEscalate = nextApprover.EmployeeManagementTier > 3;

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
                else
                {
                    permission = new FormPermission((byte)PermissionFlag.UserActionable, groupId: ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_ID);
                    originalForm.NextApprover = ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_EMAIL;
                    originalForm.NextApprovalLevel = ConflictOfInterest.POD_EFFORMS_BUSINESS_ADMIN_GROUP_NAME;
                    new List<StatusBtnModel>
                                            {
                                                new StatusBtnModel(){
                                                    StatusId = (int)FormStatus.Delegated,
                                                    BtnText = FormStatus.Delegate.ToString(),
                                                    FormSubStatus = FormStatus.Delegated.ToString(),
                                                    HasDeclaration = false
                                                },
                                            };
                    return new EscalationResult()
                    {
                        UpdatedForm = originalForm,
                        DoesEscalate = doesEscalate,
                        NotifyDays = 0,
                        EscalationDays = 0,
                        PermissionUpdate = permission,
                        StatusBtnData = statusBtnData
                    };
                }
                
            }
            catch
            {
                return null;
            }
        }
    }
}
