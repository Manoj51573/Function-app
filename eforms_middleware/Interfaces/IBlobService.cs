using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using eforms_middleware.DataModel;

namespace eforms_middleware.Interfaces;

public interface IBlobService
{
    Task UploadAttachmentsAsync(IEnumerable<AttachmentRecordFile> attachments);
    Task<BlobRequestResult> DownloadDocumentAsync(int formId, Guid filename);
    Dictionary<int, List<string>> GetBlobIds();
}