using System.Collections.Generic;

namespace eforms_middleware.DataModel;

public class ResponseAttachments
{
    public IList<AttachmentResult> Attachments { get; set; }
}