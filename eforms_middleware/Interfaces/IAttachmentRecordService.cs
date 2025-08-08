using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using eforms_middleware.DataModel;
using Microsoft.AspNetCore.Http;

namespace eforms_middleware.Interfaces;

public interface IAttachmentRecordService
{
    Task<List<AttachmentRecordFile>> CreateOrUpdateAttachmentRecordAsync(int formId, IFormFileCollection files);
    Task RemoveAttachmentAsync(int formId, Guid id);
    Task<bool> AttachmentExistsAsync(Guid id);
    Task ActivateAttachmentRecordsAsync(int formId, IList<AttachmentResult> attachments);
    Task DeactivateAllAttachmentsAsync(int formInfoId);
}