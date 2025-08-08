using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Interfaces;
using eforms_middleware.Settings;
using eforms_middleware.Workflows;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace eforms_middleware.MasterData;

[DomainAuthorisation]
public class WASuburbsFunctions
{
    private readonly IRepository<RefWASuburb> _refWASuburbRepository;
    private readonly WorkflowBtnManager _workflowBtnService;
    protected readonly IEmployeeService EmployeeService;

    public WASuburbsFunctions(IRepository<RefWASuburb> waSuburbRepository
        , WorkflowBtnManager workflowBtnService, IEmployeeService employeeService)
    {
        _refWASuburbRepository = waSuburbRepository;
        _workflowBtnService = workflowBtnService;
        EmployeeService = employeeService;
    }

    [FunctionName("get-all-wa-suburbs")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
        HttpRequest req,
        ILogger log)
    {
        log.LogInformation("get-all-wa-suburbs => HTTP trigger function processed a request.");

        var responseMessage = await _refWASuburbRepository.ListAsync();

        return new OkObjectResult(responseMessage);
    }
}