using eforms_middleware.Interfaces;
using eforms_middleware.Services;
using eforms_middleware.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace eforms_middleware.MasterData;

[DomainAuthorisation]
public class EmployeeFunctions
{
    private readonly IEmployeeService _employeeService;
    private readonly RosterChangeApprovalService _rosterChangeApprovalService;
    private readonly IPositionService _positionService;
    public EmployeeFunctions(IEmployeeService employeeService,
        RosterChangeApprovalService rosterChangeApprovalService,
        IPositionService positionService)
    {
        _employeeService = employeeService;
        _positionService = positionService;
        _rosterChangeApprovalService = rosterChangeApprovalService;
    }

    [FunctionName("query-employees")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
    {
        log.LogInformation("C# HTTP trigger function query-employees processed");

        var searchResults = await _employeeService.FindEmployeesAsync(req.Query);

        if (searchResults.Any()) log.LogInformation("No employees found with query", req.Query);
        return new OkObjectResult(searchResults);
    }

    [FunctionName("query-user")]
    public async Task<IActionResult> QueryUser([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
    {
        log.LogInformation("HTTP Trigger Query User");
        var AdId = req.Query["AdId"];
        var result = await _employeeService.GetEmployeeByAzureIdAsync(new Guid(AdId.ToString()));
        return new OkObjectResult(result);

    }
    [FunctionName("query-position")]
    public async Task<IActionResult> QueryPosition([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
    {
        log.LogInformation("HTTP Trigger Query Position Detail");
        var positionId = int.Parse(req.Query["positionId"].ToString());
        var result = await _positionService.GetBusinessCasePositionDetail(positionId, false);

        return new OkObjectResult(result);
    }

    [FunctionName("validate-pod-user")]
    public async Task<IActionResult> ValidatePodUser([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
    {
        log.LogInformation($"HTTP Trigger for {nameof(ValidatePodUser)}");
        var azureId = new Guid(req.Query["azureId"].ToString());
        var result = await _employeeService.IsPodGroupUser(azureId);
        return new OkObjectResult(result);
    }

    [FunctionName("validate-odg-user")]
    public async Task<IActionResult> ValidateOdgUser([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
    {
        log.LogInformation($"HTTP Trigger for {nameof(ValidateOdgUser)}");
        var azureId = new Guid(req.Query["azureId"].ToString());
        var result = await _employeeService.IsOdgGroupUser(azureId);
        return new OkObjectResult(result);
    }
}