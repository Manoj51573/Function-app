using System;

namespace eforms_middleware.DataModel;

public class RemoveAttachmentRequest
{
    public int FormId { get; set; }
    public Guid AttachmentId { get; set; }
}