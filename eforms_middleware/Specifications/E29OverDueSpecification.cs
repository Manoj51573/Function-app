using System;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;

namespace eforms_middleware.Specifications;

public class E29OverDueSpecification : BaseQuerySpecification<FormInfo>
{
    public E29OverDueSpecification(DateTime firstDate, DateTime secondDate)
    : base(
        x => x.AllFormsId == (int)FormType.e29 && x.FormStatusId == (int)FormStatus.Unsubmitted &&
             (x.Created.Value.Date == firstDate.Date || x.Created.Value.Date == secondDate.Date), x => x.Created)
    {
        
    }
}