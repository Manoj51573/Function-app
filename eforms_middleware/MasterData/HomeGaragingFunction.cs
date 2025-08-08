using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Services;
using eforms_middleware.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using static eforms_middleware.Settings.Helper;

namespace eforms_middleware.MasterData
{
    [DomainAuthorisation]
    public class HomeGaragingFunction
    {
        private readonly HomeGaragingApprovalService _homeGaragingApprovalService;
        private readonly IRequestingUserProvider _requestingUserProvider;

        public HomeGaragingFunction(
            HomeGaragingApprovalService homeGaragingApprovalService,
            IRequestingUserProvider requestingUserProvider)
        {
            _homeGaragingApprovalService = homeGaragingApprovalService;
            _requestingUserProvider = requestingUserProvider;
        }

        [FunctionName("func-create-update-home-garaging-request")]
        public async Task<IActionResult> CreateUpdateRosterChangeRequest(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest request,
        ILogger log, ExecutionContext context)
        {
            log.LogInformation("START - C# HTTP trigger function processed a request for Function App: func-create-update-home-garaging-request");

            string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
            var requestModel = JsonConvert.DeserializeObject<FormInfoUpdate>(requestBody);
            var requestingUser = request.Headers["Requesting-User"];
            if (!IsImpersonationAllowed)
            {
                requestingUser = request.Headers["upn"];
            }
            _requestingUserProvider.SetRequestingUser(requestingUser);

            log.LogInformation("Executing function for {0}", requestingUser);

            var result = new JsonResult(null);

            if (requestModel.FormDetails.AllFormsId is (int)FormType.Hma)
            {
                try
                {
                    var requestResult = await _homeGaragingApprovalService.HomeGaragingApprovalSystem(requestBody);

                    result.StatusCode = requestResult.StatusCode;
                    result.Value = requestResult.Value;
                }
                catch (Exception e)
                {
                    log.LogError(e, e.Message);
                    result.Value = new
                    {
                        error = e.Message
                    };
                    result.StatusCode = StatusCodes.Status500InternalServerError;
                }

            }
            return result;

        }
    }
}
