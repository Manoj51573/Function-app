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
    public class OnlinePublishingRequestFunctions
    {
        private readonly OnlinePublishingRequestApprovalService _onlinePublishingRequestApprovalService;

        public OnlinePublishingRequestFunctions(
        OnlinePublishingRequestApprovalService onlinePublishingRequestApprovalService)
        {

            _onlinePublishingRequestApprovalService = onlinePublishingRequestApprovalService;
        }


        [FunctionName("create-update-opr")]
        public async Task<IActionResult> CreateUpdateOpr(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string oprDataJson = await new StreamReader(req.Body).ReadToEndAsync();
            var oprData = JsonConvert.DeserializeObject<FormInfoRequest>(oprDataJson);
            oprData.ActionBy = req.Headers["Requesting-User"];
            oprData.BaseUrl = req.Headers["Origin"].FirstOrDefault();
            oprData.FormOwnerEmail = oprData.ActionBy;
            return await _onlinePublishingRequestApprovalService.OPRApproval(oprData);

        }
    }
}
