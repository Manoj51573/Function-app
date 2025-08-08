using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace eforms_middleware.MasterData;

public class TempPermissionFunction
{
    private readonly IPermissionManager _permissionManager;
    private readonly IRequestingUserProvider _requestingUserProvider;

    public TempPermissionFunction(IPermissionManager permissionManager, IRequestingUserProvider requestingUserProvider)
    {
        _permissionManager = permissionManager;
        _requestingUserProvider = requestingUserProvider;
    }
    
    [FunctionName("AddPermission")]
    public async Task<IActionResult> Add(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
    {
        var requestingUser = req.Headers["Requesting-User"];
        _requestingUserProvider.SetRequestingUser(requestingUser);
        var formId = int.Parse(req.Headers["FormId"]);
        string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
        var body = JsonConvert.DeserializeObject<FormPermission>(requestBody);
        var result = await _permissionManager.UpdateFormPermissionsAsync(formId, new List<FormPermission>{body});
        //var result = await _permissionManager.GetRequestersFormPermissionsAsync(formId);
        // return new OkObjectResult(result);
        return new OkObjectResult(result);
    }
    
    [FunctionName("GetPermission")]
    public async Task<IActionResult> Get(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
    {
        var requestingUser = req.Headers["Requesting-User"];
        _requestingUserProvider.SetRequestingUser(requestingUser);
        var formId = int.Parse(req.Headers["FormId"]);
        // var result = await _permissionManager.AddPermissionAsync(formId, PermissionFlag.UserActionable, true, userId: new Guid("21AEE799-20A6-4DBD-A55D-B465C5FC534C"));
        var result = await _permissionManager.GetRequestersFormPermissionsAsync(formId);
        // return new OkObjectResult(result);
        return new OkObjectResult(result);
    }
}