using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;

namespace eforms_middleware.Specifications;

public class PermissionOtherThanViewSpecification : BaseQuerySpecification<FormPermission>
{
    public PermissionOtherThanViewSpecification(int formId)
    : base(x => x.FormId == formId && x.PermissionFlag != (byte)PermissionFlag.View, x => x.Id, orderDescending: true)
    {
    }
}