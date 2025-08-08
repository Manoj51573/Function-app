using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using eforms_middleware.Settings;
using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure;
using eforms_middleware.Workflows;
using eforms_middleware.Interfaces;

namespace eforms_middleware.Forms
{
    [DomainAuthorisation]
    public class AllFormsFunctions
    {
        private readonly IRepository<AllForm> _allFormRepository;
        private readonly WorkflowBtnManager _workflowBtnService;
        protected readonly IEmployeeService EmployeeService;

        public AllFormsFunctions(IRepository<AllForm> allFormRepository
            , WorkflowBtnManager workflowBtnService, IEmployeeService employeeService)
        {
            _allFormRepository = allFormRepository;
            _workflowBtnService = workflowBtnService;
            EmployeeService = employeeService;
        }

        [FunctionName("get-all-forms-by-id")]
        public async Task<IActionResult> GetAllFormsById(
           [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
           ILogger log)
        {
            int allFormsId = Convert.ToInt32(req.Query["allFormsId"]);
            var dt = await _allFormRepository.FirstOrDefaultAsync(x => x.AllFormsId == allFormsId && x.IsAvailable);
            return new OkObjectResult(dt);
        }


        [FunctionName("get-all-forms-by-parent")]
        public async Task<IActionResult> GetAllFormsByParentId(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            int parentFormId = Convert.ToInt32(req.Query["parentFormId"]);

            var requestingUser = req.Headers["Requesting-User"];

            var user = await EmployeeService.GetEmployeeByEmailAsync(requestingUser);
            var isContractor = string.IsNullOrEmpty(user.EmployeeNumber);

            var parent = await _allFormRepository.FirstOrDefaultAsync(x => x.AllFormsId == parentFormId && x.IsAvailable && ( isContractor? x.IsEmployeeOnly==false : true));
           
            if (parent == null)
            {
                return new OkObjectResult(null);
            }
            var dt = await _allFormRepository.FindByAsync(x => x.ParentAllFormsId == parentFormId && x.IsAvailable);
            return new OkObjectResult(dt);
        }

        [FunctionName("get-form-btn")]
        public async Task<IActionResult> GetFormBtn(
           [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
           ILogger log)
        {
            int formId = Convert.ToInt32(req.Query["formId"]);
            var requestingUser = req.Headers["Requesting-User"];

            var dt = await _workflowBtnService.GetBtnForUser(formId, requestingUser);

            return new OkObjectResult(dt);
        }
    }
}
