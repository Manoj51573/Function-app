using System;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs.Models;
using eforms_middleware.DataModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace eforms_middleware.Interfaces;

public interface IAttachmentService
{
    Task<RequestResult> AddAttachmentsAsync(int formId, IFormFileCollection files);
    Task<RequestResult> RemoveAttachmentAsync(int formId, Guid id);
    Task<BlobRequestResult> ViewAttachmentAsync(int formId, Guid id);
}