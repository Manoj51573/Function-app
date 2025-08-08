using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using eforms_middleware.Settings;
using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure;

namespace eforms_middleware.MasterData
{
    [DomainAuthorisation]
    public class ChartOfAccountsFunctions
    {
        private readonly IRepository<RefGLFunds> _fundsRepo;
        private readonly IRepository<RefGLCostCentres> _costCentreRepo;
        private readonly IRepository<RefGLLocations> _locationRepo;
        private readonly IRepository<RefGLProjects> _projectRepo;
        private readonly IRepository<RefGLActivities> _activitiesRepo;
        private readonly IRepository<RefGLAccountsSubAccounts> _accountsSubAccountsRepo;

        public ChartOfAccountsFunctions(IRepository<RefGLFunds> fundsRepo,
        IRepository<RefGLCostCentres> costCentreRepo,
        IRepository<RefGLLocations> locationRepo,
        IRepository<RefGLProjects> projectRepo,
        IRepository<RefGLActivities> activitiesRepo,
        IRepository<RefGLAccountsSubAccounts> accountsSubAccountsRepo)
        {
            _fundsRepo = fundsRepo;
            _costCentreRepo = costCentreRepo;
            _locationRepo = locationRepo;   
            _projectRepo = projectRepo;
            _activitiesRepo = activitiesRepo;
            _accountsSubAccountsRepo = accountsSubAccountsRepo;
        }

        [FunctionName("get-all-coa-funds")]
        public async Task<IActionResult> GetAllCOAFunds(
         [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
         ILogger log)
        {            
            var dt = await _fundsRepo.FindByAsync(x => x.ActiveRecord);
            return new OkObjectResult(dt);
        }

        [FunctionName("get-all-coa-cost-centres")]
        public async Task<IActionResult> GetAllCOACostCentres(
         [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
         ILogger log)
        {            
            var dt = await _costCentreRepo.FindByAsync(x => x.ActiveRecord);
            return new OkObjectResult(dt);
        }

        [FunctionName("get-all-coa-locations")]
        public async Task<IActionResult> GetAllCOALocations(
         [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
         ILogger log)
        {
            var dt = await _locationRepo.FindByAsync(x => x.ActiveRecord);
            return new OkObjectResult(dt);
        }

        [FunctionName("get-all-coa-activities")]
        public async Task<IActionResult> GetAllCOAActivities(
         [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
         ILogger log)
        {
            var dt = await _activitiesRepo.FindByAsync(x => x.ActiveRecord);
            return new OkObjectResult(dt);
        }

        [FunctionName("get-all-coa-projects")]
        public async Task<IActionResult> GetAllCOAProjects(
         [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
         ILogger log)
        {
            var dt = await _projectRepo.FindByAsync(x => x.ActiveRecord);
            return new OkObjectResult(dt);
        }

        [FunctionName("get-all-coa-account")]
        public async Task<IActionResult> GetAccountsNumber(
         [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
         ILogger log)
        {
            var dt = await _accountsSubAccountsRepo.FindByAsync(x => x.ActiveRecord);
            return new OkObjectResult(dt);
        }
    }
}
