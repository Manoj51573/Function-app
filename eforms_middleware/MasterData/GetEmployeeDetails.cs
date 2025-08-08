using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using eforms_middleware.Interfaces;
using eforms_middleware.Settings;

namespace eforms_middleware.GetMasterData;

[DomainAuthorisation]
public class GetEmployeeDetails
{
    private readonly IEmployeeService _employeeService;

    public GetEmployeeDetails(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }
    
    [FunctionName("func-get-employee-details")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
        ILogger log, ExecutionContext context)
    {
        log.LogInformation("START - C# HTTP trigger function processed a request for Function App: func-get-employee-details");

        var result = new JsonResult(null);

        string userId = req.Query["user"];
        if (string.IsNullOrEmpty(userId))
        {
            result.Value = new
            {
                error = "No user supplied"
            };
            // Wrong error code it's a bad request if they didn't supply an email
            result.StatusCode = StatusCodes.Status403Forbidden;
            return result;
        }

        try
        {
            var user = await _employeeService.GetEmployeeDetailsAsync(userId);
            result.Value = new
            {
                user,
                count = user == null ? 0 : 1
            };
            result.StatusCode = user == null ? StatusCodes.Status404NotFound : StatusCodes.Status200OK;
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
        log.LogInformation("END - C# HTTP trigger function processed a request for Function App: func-get-employee-details");

        return result;
    }
}

