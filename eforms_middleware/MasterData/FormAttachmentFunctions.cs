using System;
using System.Threading.Tasks;
using System.Web.Http;
using eforms_middleware.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using static eforms_middleware.Settings.Helper;

namespace eforms_middleware.MasterData;

public class FormAttachmentFunctions
{
    private readonly IAttachmentService _attachmentService;
    private readonly IRequestingUserProvider _requestingUserProvider;

    public FormAttachmentFunctions(IAttachmentService attachmentService, IRequestingUserProvider requestingUserProvider)
    {
        _attachmentService = attachmentService;
        _requestingUserProvider = requestingUserProvider;
    }
    
    [FunctionName("add-attachments")]
    public async Task<IActionResult> AddAttachment(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "form-info/{formId:int}/add-attachments")] HttpRequest req, int formId, ILogger log)
    {
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
        log.LogInformation($"C# HTTP trigger function processed a add-attachments. For Form: {formId}");
        var result = new JsonResult(null);
        try
        {
            var data = await req.ReadFormAsync();
            var files = data.Files;

            var attachmentResult = await _attachmentService.AddAttachmentsAsync(formId, files);

            log.LogInformation("add-attachments finished.");
            if (attachmentResult.Success)
            {
                return new OkObjectResult(attachmentResult.Value);
            }
            result.StatusCode = attachmentResult.StatusCode;
            result.Value = attachmentResult.Value;
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
    
    [FunctionName("remove-attachment")]
    public async Task<IActionResult> RemoveAttachment(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "form-info/{formId:int}/remove-attachment/{id:guid}")]
        HttpRequest req, int formId, Guid id, ILogger log)
    {
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
        log.LogInformation("C# HTTP trigger function processed a request. For Form {FormId} to remove attachment {Id}", formId, id);

        var result = new JsonResult(null);
        try
        {
            var requestResult = await _attachmentService.RemoveAttachmentAsync(formId, id);
            result.StatusCode = requestResult.StatusCode;
            result.Value = requestResult.Value;
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
        
        log.LogInformation("Remove attachments finished.");
        return result;
    }

    [FunctionName("view-attachment")]
    public async Task<IActionResult> ViewAttachment(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "form-info/{formId:int}/view-attachment/{id:guid}")]
        HttpRequest req, int formId, Guid id, ILogger log)
    {
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
        log.LogInformation("C# HTTP trigger function processed a request. For Form {FormId} to view attachment {Id}", formId, id);
        
        try
        {
            var result =  await _attachmentService.ViewAttachmentAsync(formId, id);
            log.LogInformation("C# HTTP trigger function completed a request. For Form {FormId} to view attachment {Id}", formId, id);
            return new FileStreamResult(result.Value.Content.ToStream(), result.Value.Details.ContentType);
        }
        catch (Exception e)
        {
            log.LogError(e, e.Message);
            return new InternalServerErrorResult();
        }
    }
}