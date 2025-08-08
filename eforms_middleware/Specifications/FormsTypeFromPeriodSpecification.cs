using System;
using DoT.Infrastructure.DbModels.Entities;

namespace eforms_middleware.Specifications
{
    public class FormsTypeFromPeriodSpecification : BaseQuerySpecification<FormInfo>
    {
        public FormsTypeFromPeriodSpecification(int formTypeId, DateTime? createdAfter = null, DateTime? createdBefore = null)
            : base(
                x => x.AllFormsId == formTypeId && (createdAfter == null || createdAfter <= x.Created) &&
                     (createdBefore == null || x.Created <= createdBefore), x => x)
        {
            
        }
    }
}