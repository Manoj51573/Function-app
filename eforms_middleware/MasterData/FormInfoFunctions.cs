using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.GetMasterData;
using eforms_middleware.Interfaces;
using eforms_middleware.Services;
using eforms_middleware.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;
using static eforms_middleware.Settings.Helper;

namespace eforms_middleware.Forms
{
    [DomainAuthorisation]
    public class FormInfoFunctions
    {
        private readonly Func<FormType, FormServiceBase> _formService;
        private readonly IFormInfoService _formInfoService;
        private readonly COIApprovalService _coiApprovalService;
        private readonly IRequestingUserProvider _requestingUserProvider;
        private readonly EmployeeDetailsService _employeeDetailsService;

        public FormInfoFunctions(Func<FormType, FormServiceBase> formService
            , IFormInfoService formInfoService
            , COIApprovalService coiApprovalService
            , EmployeeDetailsService employeeDetailsService
            , IRequestingUserProvider requestingUserProvider)
        {
            _formService = formService;
            _formInfoService = formInfoService;
            _coiApprovalService = coiApprovalService;
            _employeeDetailsService = employeeDetailsService;
            _requestingUserProvider = requestingUserProvider;
        }

        [FunctionName("func-create-update-form-details")]
        public async Task<IActionResult> CreateUpdateFormInfo(
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

            if (requestModel.FormDetails.AllFormsId is (int)FormType.CoI_SE or (int)FormType.CoI_REC
                or (int)FormType.CoI_GBH or (int)FormType.CoI)
            {
                try
                {
                    var requestResult = await _coiApprovalService.COIApprovalSystem(requestBody);

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
                return result;
            }

            try
            {
                var formType = (FormType)requestModel.FormDetails.AllFormsId;
                try
                {
                    var requestResult = await _formService(formType).ProcessRequest(requestModel);

                    return requestResult.ToActionResult();
                }
                catch (Exception e)
                {
                    log.LogError(e, e.Message);
                    result.Value = new
                    {
                        e.Message
                    };
                    result.StatusCode = StatusCodes.Status500InternalServerError;
                }
                return result;
            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
                var status = e is ArgumentException
                    ? StatusCodes.Status400BadRequest
                    : StatusCodes.Status500InternalServerError;
                var value = new
                {
                    e.Message
                };
                return RequestResult.ErrorActionResult(errorCode: status, value: value);
            }
        }

        [FunctionName("func-update-form-details")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = null)] HttpRequest req,
            ILogger log, ExecutionContext context)
        {
            log.LogInformation("START - C# HTTP trigger function processed a request for Function App: func-create-update-form-details");

            var requestingUser = req.Headers["Requesting-User"];
            if (!IsImpersonationAllowed)
            {
                requestingUser = req.Headers["upn"];
            }

            if (requestingUser != StringValues.Empty)
            {
                log.LogInformation("Executing function for {0}", requestingUser);
            }

            _requestingUserProvider.SetRequestingUser(requestingUser);

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Get original item if ID provided
            var request = JsonConvert.DeserializeObject<FormInfoUpdate>(requestBody);
            if (!request.FormDetails.FormInfoId.HasValue)
            {
                log.LogInformation("Cannot update a form without an existing form ID");
                return new StatusCodeResult(422);
            }
            var form = await _formInfoService.GetFormByIdAsync(request.FormDetails.FormInfoId.Value);
            if (form is null || (!form.CanAction && request.FormAction != nameof(FormStatus.Recall)) || (request.FormAction == nameof(FormStatus.Recall) && !form.CanRecall))
            {
                log.LogInformation("User {RequestingUser} tried to action form with ID {FormID} to which they do not have access", requestingUser, request.FormDetails.FormInfoId);
                return new StatusCodeResult(403);
            }
            var formType = (FormType)form.AllFormsID;

            try
            {
                var requestResult = await _formService(formType).ProcessRequest(request);

                return requestResult.ToActionResult();
            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
                var value = new
                {
                    e.Message
                };
                return RequestResult.ErrorActionResult(value: value);
            }
        }
        //
        [FunctionName("get-my-forms")]
        public async Task<IActionResult> GetMyForms(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                var requestingUser = req.Query["user"];
                if (!IsImpersonationAllowed)
                {
                    requestingUser = req.Headers["upn"];
                }

                if (requestingUser != StringValues.Empty)
                {
                    log.LogInformation("Executing function for {0}", requestingUser);
                }
                _requestingUserProvider.SetRequestingUser(requestingUser);
                var myForms = await _formInfoService.GetUsersFormsAsync(requestingUser);
                return new OkObjectResult(myForms);
            }
            catch (Exception e)
            {
                log.LogError(e.Message, e);
                return new InternalServerErrorResult();
            }
        }


        [FunctionName("get-all-directorate")]
        public IActionResult GetAllDirectorate(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                var directorateList = _formInfoService.GetAllDirectorate();
                return new OkObjectResult(directorateList);
            }
            catch (Exception e)
            {
                log.LogError(e.Message, e);
                return new InternalServerErrorResult();
            }
        }

        [FunctionName("get-all-employee")]
        public async Task<IActionResult> GetAllEmployee(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                var employeeList = await _employeeDetailsService.GetEmployee();
                return new OkObjectResult(employeeList);
            }
            catch (Exception e)
            {
                log.LogError(e.Message, e);
                return new InternalServerErrorResult();
            }
        }

        [FunctionName("get-all-form-types")]
        public IActionResult GetAllFormTypes(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                var formTypesList = _formInfoService.GetAllFormTypes();
                return new OkObjectResult(formTypesList);
            }
            catch (Exception e)
            {
                log.LogError(e.Message, e);
                return new InternalServerErrorResult();
            }
        }

        [FunctionName("get-all-form-statuses")]
        public IActionResult GetAllFormStatuses(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                var formStatuses = _formInfoService.GetAllFormStatuses();
                return new OkObjectResult(formStatuses);
            }
            catch (Exception e)
            {
                log.LogError(e.Message, e);
                return new InternalServerErrorResult();
            }
        }

        /// <summary>
        /// Get Function
        /// </summary>
        /// <param name="req"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("func-get-form-info-by-id")]
        public async Task<IActionResult> GetFormInfoByID(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            if (!int.TryParse(req.Query["ID"], out int formInfoId))
            {
                return new BadRequestObjectResult(new[] { new { errorMessage = "An ID is required." } });
            }
            var requestingUser = req.Headers["Requesting-User"];
            if (!IsImpersonationAllowed)
            {
                requestingUser = req.Headers["upn"];
            }
            _requestingUserProvider.SetRequestingUser(requestingUser);
            if (formInfoId == 0)
            {
                return new OkObjectResult(null);
            }

            try
            {
                var form = await _formInfoService.GetFormByIdAsync(formInfoId);
                if (form == null)
                {
                    return new NotFoundObjectResult(new[] { new { errorMessage = $"Requested form {formInfoId} not found." } });
                }

                return new OkObjectResult(new { form = new[] { form }, count = 1 });
            }
            catch (Exception e)
            {
                log.LogError(e, e.Message);
                var errorObjectResult = new ObjectResult(e.Message);
                errorObjectResult.StatusCode = StatusCodes.Status500InternalServerError;
                return errorObjectResult;
            }
        }
    }
}
