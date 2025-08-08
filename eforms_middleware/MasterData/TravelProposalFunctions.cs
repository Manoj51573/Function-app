using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using eforms_middleware.Settings;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Workflows;
using System.IO;
using eforms_middleware.DataModel;
using Newtonsoft.Json;
using System.Linq;

namespace eforms_middleware.MasterData
{
    [DomainAuthorisation]
    public class TravelProposalFunctions
    {
        private readonly IRepository<CampRate> _campRateRepository;
        private readonly IRepository<RefRegion> _refRegionRepository;
        private readonly IRepository<SalaryRate> _salaryRateRepository;
        private readonly IRepository<TravelRate> _travelRateRepository;
        private readonly IRepository<TravelLocation> _travelLocationRepository;
        private readonly IRepository<TravelTime> _travelTimeRepository;
        private readonly TravelProposalApprovalService _travelProposalApprovalService;

        public TravelProposalFunctions(IRepository<CampRate> campRateRepository,
        IRepository<RefRegion> refRegionRepository,
        IRepository<SalaryRate> salaryRateRepository,
        IRepository<TravelRate> travelRateRepository,
        IRepository<TravelLocation> travelLocationRepository,
        IRepository<TravelTime> travelTimeRepository,
        TravelProposalApprovalService travelProposalApprovalService)
        {

            _campRateRepository = campRateRepository;
            _refRegionRepository = refRegionRepository;
            _salaryRateRepository = salaryRateRepository;
            _travelRateRepository = travelRateRepository;
            _travelLocationRepository = travelLocationRepository;
            _travelTimeRepository = travelTimeRepository;
            _travelProposalApprovalService = travelProposalApprovalService;
        }

        [FunctionName("get-all-location")]
        public async Task<IActionResult> GetAllLocation(
           [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
           ILogger log)
        {
            var dt = await _travelLocationRepository.ListAsync();
            return new OkObjectResult(dt);
        }

        [FunctionName("get-location-rate")]
        public async Task<IActionResult> GetLocationRate(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            int travelLocationId = Convert.ToInt32(req.Query["travelLocationId"]);
            string rateType = req.Query["rateType"];
            var email = req.Headers["Requesting-User"];
            int salaryRateId = await _travelProposalApprovalService.GetSalaryRateIdByEmail(email);

            var dt = await _travelRateRepository.FirstOrDefaultAsync(x => x.TravelLocationId == travelLocationId
                            && x.RateType == rateType
                            && x.SalaryRateId == salaryRateId
                            && x.IsActive.Value);
            return new OkObjectResult(dt);
        }

        [FunctionName("get-all-salary-rate")]
        public async Task<IActionResult> GetAllSalaryRate(
          [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
          ILogger log)
        {
            var dt = await _salaryRateRepository.ListAsync();
            return new OkObjectResult(dt);
        }

        [FunctionName("get-all-region")]
        public async Task<IActionResult> GetAllRegion(
          [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
          ILogger log)
        {
            var dt = await _refRegionRepository.ListAsync();
            return new OkObjectResult(dt);
        }

        [FunctionName("get-camp-rate")]
        public async Task<IActionResult> GetAllCampRate(
          [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
          ILogger log)
        {
            string campType = req.Query["campType"];
            bool isCookProvided = Convert.ToBoolean(req.Query["isCookProvided"]);
            string parallelType = req.Query["parallelType"];

            var dt = await _campRateRepository.FirstOrDefaultAsync(x => x.Type == campType
                                && x.IsCookProvided == isCookProvided
                                && x.ParallelType == parallelType);
            return new OkObjectResult(dt);
        }

        [FunctionName("get-all-time-travel")]
        public async Task<IActionResult> GetAllTimeTravel(
          [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
          ILogger log)
        {
            var dt = await _travelTimeRepository.ListAsync();
            return new OkObjectResult(dt);
        }

        [FunctionName("create-update-travel-proposal")]

        public async Task<IActionResult> CreateUpdateTravelProposal(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string travelDataJson = await new StreamReader(req.Body).ReadToEndAsync();
            var travelData = JsonConvert.DeserializeObject<FormInfoRequest>(travelDataJson);
            travelData.ActionBy = req.Headers["Requesting-User"];
            travelData.BaseUrl = req.Headers["Origin"].FirstOrDefault();
            return await _travelProposalApprovalService.TravelApproval(travelData); ;
        }

        [FunctionName("travel-proposal-approval")]
        public async Task<IActionResult> TravelProposalApproval(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
           ILogger log)
        {            
            string travelDataJson = await new StreamReader(req.Body).ReadToEndAsync();
            var travelData = JsonConvert.DeserializeObject<FormInfoRequest>(travelDataJson);
            travelData.ActionBy = req.Headers["Requesting-User"];
            travelData.BaseUrl = req.Headers["Origin"].FirstOrDefault();
            return await _travelProposalApprovalService.TravelApprovalWorkFlow(travelData);
        }

        [FunctionName("show-travel-cost")]
        public async Task<IActionResult> IsATORateViewAllowed(
             [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
           ILogger log)
        {
            var actionBy = req.Headers["Requesting-User"];            
            var isAllowed = await _travelProposalApprovalService.IsRateViewAllowed(actionBy);
            return new OkObjectResult(isAllowed);
        }
    }
}
