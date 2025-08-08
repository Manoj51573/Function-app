using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
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
    public class RecruitmentEformFunction
    {
        private readonly IRequestingUserProvider _requestingUserProvider;
        private readonly IRecruitmentEformService _recruitmentEformService;
        public RecruitmentEformFunction(IRequestingUserProvider requestingUserProvider,
                IRecruitmentEformService recruitmentEformService)
        {
            _requestingUserProvider = requestingUserProvider;
            _recruitmentEformService = recruitmentEformService;
        }
        [FunctionName("func-Create-Update-BusinessCaseNonAdv")]
        public async Task<IActionResult> CreateOrUpdateBusinessCaseNonAdv([HttpTrigger(AuthorizationLevel.Function,
                    "post", Route = null)] HttpRequest request,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation($"START - C# HTTP Trigger Function for Function App: {nameof(RecruitmentEformFunction.CreateOrUpdateBusinessCaseNonAdv)}");
            var result = new JsonResult(null);
            var requestBody = await new StreamReader(request.Body).ReadToEndAsync();
            var formInfoRequestModel = JsonConvert.DeserializeObject<FormInfoUpdate>(requestBody);
            var requestingUser = !IsImpersonationAllowed ? request.Headers["upn"] : request.Headers["Requesting-User"];
            var isValidRequestForm = formInfoRequestModel.FormDetails.AllFormsId == (int)FormType.Bcna;
            if (!isValidRequestForm)
            {
                return new JsonResult(null);
            }

            _requestingUserProvider.SetRequestingUser(requestingUser);
            log.LogInformation($"Execute Function for user {requestingUser}");
            try
            {
                var businessCaseNonAdvHttpRequest = JsonConvert.DeserializeObject<BusinessCaseNonAdvHttpRequest>(requestBody);
                var requestResult = await _recruitmentEformService.BusinessCaseNonAdvCreateOrUpdate(businessCaseNonAdvHttpRequest);
                result.StatusCode = requestResult.StatusCode;
                result.Value = requestResult.Value;
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
                result.Value = new { error = ex.Message };
                result.StatusCode = StatusCodes.Status500InternalServerError;
            }
            return result;

        }

    }
}
