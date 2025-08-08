using System.Collections.Generic;
using System.Linq;
using DoT.Infrastructure.DbModels.Entities;
using Microsoft.EntityFrameworkCore;

namespace eforms_middleware.Specifications;

public class AttachmentsWithoutRecordsSpecification : BaseQuerySpecification<FormInfo>
{
    public AttachmentsWithoutRecordsSpecification(IEnumerable<int> formIds)
    : base(info => formIds.Any(id => info.FormInfoId == id) && !info.FormAttachments.Any(), info => info)
    {
        AddInclude(info => info.FormAttachments);
        AddInclude(info => info.FormPermissions);
    }
}