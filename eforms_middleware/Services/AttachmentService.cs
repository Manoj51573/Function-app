using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs.Models;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace eforms_middleware.Services;

public class AttachmentService : IAttachmentService
{
    private readonly IFormInfoService _formInfoService;
    private readonly IBlobService _blobService;
    private readonly IValidator<IFormFile> _fileValidator;
    private readonly ILogger<AttachmentService> _logger;
    private readonly IAttachmentRecordService _attachmentRecordService;

    public AttachmentService(IAttachmentRecordService attachmentRecordService, IFormInfoService formInfoService,
        IBlobService blobService, IValidator<IFormFile> fileValidator, ILogger<AttachmentService> logger)
    {
        _attachmentRecordService = attachmentRecordService;
        _formInfoService = formInfoService;
        _blobService = blobService;
        _fileValidator = fileValidator;
        _logger = logger;
    }
    
    public async Task<RequestResult> AddAttachmentsAsync(int formId, IFormFileCollection files)
    {
        var form = await _formInfoService.GetFormByIdAsync(formId);

        if (form is not { CanAction: true })
        {
            return RequestResult.FailedRequest((int)HttpStatusCode.Forbidden,
                new[]
                {
                    new FailureItem { ErrorMessage = "Forbidden - User does not have access to the requested attachment." }
                });
        }

        if (files == null)
        {
            return RequestResult.FailedRequest((int)HttpStatusCode.UnprocessableEntity, new[]
            {
                new FailureItem { ErrorMessage = "Attachment Error - No attachments are supplied." }
            });
        }
        
        var validationFailures = new Dictionary<string, ValidationResult>();
        foreach (var attachment in files)
        {
            var validationResult = _fileValidator.Validate(attachment);
            if (!validationResult.IsValid)
            {
                validationFailures.Add(attachment.FileName, validationResult);
            }
        }

        if (validationFailures.Any())
        {
            return RequestResult.FailedRequest((int)HttpStatusCode.UnprocessableEntity,
                validationFailures.Select(v => new FailureItem
                    { ErrorMessage = $"{v.Key} Invalid: {string.Join(' ', v.Value.Errors.Select(x => x.ErrorMessage))}" }).ToArray());
        }

        try
        {
            var formAttachments = await _attachmentRecordService.CreateOrUpdateAttachmentRecordAsync(formId, files);

            await _blobService.UploadAttachmentsAsync(formAttachments);

            return RequestResult.SuccessfulAttachmentRequest(formAttachments);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error thrown in {ClassName}", nameof(AttachmentService));
            return RequestResult.FailedRequest((int)HttpStatusCode.InternalServerError,
                new[] { new FailureItem { ErrorMessage = "An unexpected error occurred. Please try again later." } });
        }
    }

    public async Task<RequestResult> RemoveAttachmentAsync(int formId, Guid id)
    {
        var form = await _formInfoService.GetFormByIdAsync(formId);

        if (form is not { CanAction: true })
        {
            return RequestResult.FailedRequest((int)HttpStatusCode.Forbidden,
                new[]
                {
                    new FailureItem { ErrorMessage = "Forbidden - User does not have access to the requested form to alter attachments." }
                });
        }
        
        await _attachmentRecordService.RemoveAttachmentAsync(formId, id);
        return RequestResult.SuccessfulRequest();
    }

    public async Task<BlobRequestResult> ViewAttachmentAsync(int formId, Guid id)
    {
        var form = await _formInfoService.GetFormByIdAsync(formId);

        if (form is null)
        {
            return BlobRequestResult.Failed((int)HttpStatusCode.Forbidden, "Forbidden - User does not have access to the requested Form.");
        }

        var exists = await _attachmentRecordService.AttachmentExistsAsync(id);
        if (!exists)
        {
            return BlobRequestResult.Failed((int)HttpStatusCode.NotFound, null);
        }

        var blobResult = await _blobService.DownloadDocumentAsync(formId, id);

        return BlobRequestResult.Succeeded(blobResult.Value);
    }
}