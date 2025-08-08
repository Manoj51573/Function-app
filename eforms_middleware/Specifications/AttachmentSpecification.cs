using System;
using DoT.Infrastructure.DbModels.Entities;

namespace eforms_middleware.Specifications;

public class AttachmentSpecification : BaseQuerySpecification<FormAttachment>
{
    public AttachmentSpecification(Guid id)
    : base(attachment => attachment.Id == id, attachment => attachment.Id)
    {
        
    }
}