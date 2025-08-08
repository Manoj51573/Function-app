using System;
using Microsoft.AspNetCore.Http;

namespace eforms_middleware.DataModel;

public class AttachmentRecordFile
{
    public Guid Id { get; set; }
    public int FormInfoId { get; set; }
    public int FormPermissionId { get; set; }
    public IFormFile File { get; set; }
}