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
    public class WHSIncidentReportFunctions
    {
        private readonly WHSIncidentReportApprovalService _whsIncidentreportApprovalService;

        public WHSIncidentReportFunctions(
        WHSIncidentReportApprovalService whsIncidentreportApprovalService)
        {

            _whsIncidentreportApprovalService = whsIncidentreportApprovalService;
        }


        [FunctionName("create-update-whsir")]
        public async Task<IActionResult> CreateUpdateWHSIR(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string whsirDataJson = await new StreamReader(req.Body).ReadToEndAsync();
            var whsirData = JsonConvert.DeserializeObject<FormInfoRequest>(whsirDataJson);
            whsirData.ActionBy = req.Headers["Requesting-User"];
            whsirData.BaseUrl = req.Headers["Origin"].FirstOrDefault();
            whsirData.FormOwnerEmail = whsirData.ActionBy;
            return await _whsIncidentreportApprovalService.WHSIRApproval(whsirData);

        }
    }
}
