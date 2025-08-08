using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Workflows;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace eforms_middleware.Services
{
    public class BusinessCaseNonAdvEscalationManager : EscalationManagerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly IPermissionManager _permissionManager;
        private readonly ILogger<BusinessCaseNonAdvEscalationManager> _logger;
        private readonly BaseApprovalService _baseApprovalService;
        public BusinessCaseNonAdvEscalationManager(IEmployeeService employeeService,
        IPermissionManager permissionManager,
        ILogger<BusinessCaseNonAdvEscalationManager> logger,
        BaseApprovalService baseApprovalService) => (_employeeService, _permissionManager, _logger, _baseApprovalService)
                                                    = (employeeService, permissionManager, logger, baseApprovalService);


        public override Task<EscalationResult> EscalateFormAsync(FormInfo originalForm)
        {
            throw new System.NotImplementedException();
        }
    }
}
