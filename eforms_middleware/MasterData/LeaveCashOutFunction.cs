using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Services.AppAuthentication;
using eforms_middleware.DataModel;
using System.IO;
using System.Linq;
using System.Web.Http;
using eforms_middleware.Constants;
using eforms_middleware.GetMasterData;
using eforms_middleware.Services;
using Microsoft.Extensions.Primitives;
using eforms_middleware.Settings;
using eforms_middleware.Interfaces;
using static eforms_middleware.Settings.Helper;
using eforms_middleware.Workflows;

namespace eforms_middleware.MasterData
{
    [DomainAuthorisation]
    public class LeaveCashOutFunction
    {
        private readonly Func<FormType, FormServiceBase> _formService;
        private readonly IFormInfoService _formInfoService;
        private readonly LeaveCashOutApprovalService _leaveCashOutApprovalService;
        private readonly IRequestingUserProvider _requestingUserProvider;
        private readonly EmployeeDetailsService _employeeDetailsService;


        public LeaveCashOutFunction(Func<FormType, FormServiceBase> formService
            , IFormInfoService formInfoService
            , LeaveCashOutApprovalService leaveCashOutApprovalService
            , EmployeeDetailsService employeeDetailsService
            , IRequestingUserProvider requestingUserProvider)
        {
            _formService = formService;
            _formInfoService = formInfoService;
            _leaveCashOutApprovalService = leaveCashOutApprovalService;
            _employeeDetailsService = employeeDetailsService;
            _requestingUserProvider = requestingUserProvider;
        }

        [FunctionName("func-create-update-leave-request")]
        public async Task<IActionResult> CreateUpdateLeaveRequest(
           [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("START - C# HTTP trigger function processed a request for Function App: func-create-update-form-details");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var requestModel = JsonConvert.DeserializeObject<FormInfoUpdate>(requestBody);
            var requestingUser = req.Headers["Requesting-User"];
            if (!IsImpersonationAllowed)
            {
                requestingUser = req.Headers["upn"];
            }
            _requestingUserProvider.SetRequestingUser(requestingUser);

            log.LogInformation("Executing function for {0}", requestingUser);

            var result = new JsonResult(null);

            if (requestModel.FormDetails.AllFormsId is (int)FormType.LcR_LAC or (int)FormType.LcR_MA or (int) FormType.LcR_PLA or(int) FormType.LcR_LCO or(int) FormType.LcR_LWP or(int) FormType.LcR_PLS or(int) FormType.LcR_DSA)
            {
                try
                {
                    var requestResult = await _leaveCashOutApprovalService.LeaveCashOutApprovalSystem(requestBody);

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
