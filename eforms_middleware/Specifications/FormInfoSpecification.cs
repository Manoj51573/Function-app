using System.Linq;
using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure.Interfaces;

namespace eforms_middleware.Specifications;

public class FormInfoSpecification : BaseQuerySpecification<FormInfo>
{
    public FormInfoSpecification(int? id, IUserInfo requestingUser = null, bool includePermissions = false, bool includeAttachments = false, bool includeButtons=false)
    : base(x => (!id.HasValue || x.FormInfoId == id) && 
                (requestingUser == null || 
                 x.FormPermissions.Any(p => (p.UserId.HasValue && p.UserId == requestingUser.ActiveDirectoryId) ||
                                                      (p.PositionId.HasValue && p.PositionId == requestingUser.EmployeePositionId) ||
                                                      (p.GroupId.HasValue &&
                                                       p.Group.AdfGroupMembers.Any(g =>
                                                           g.MemberId == requestingUser.ActiveDirectoryId)))), 
        x => x.FormInfoId)
    {
        AddInclude(x => x.AllForms.ParentAllForms);
        if (includePermissions)
        {
            AddInclude(x => x.FormPermissions);
            AddInclude($"{nameof(FormInfo.FormPermissions)}.{nameof(FormPermission.Group)}.{nameof(AdfGroup.AdfGroupMembers)}");
            AddInclude($"{nameof(FormInfo.FormPermissions)}.{nameof(FormPermission.Position)}.{nameof(AdfPosition.AdfUserPositions)}");
            AddInclude($"{nameof(FormInfo.FormPermissions)}.{nameof(FormPermission.User)}");
            if (includeButtons)AddInclude($"{nameof(FormInfo.FormPermissions)}.{nameof(FormPermission.WorkflowBtns)}");
        }

        if (includeAttachments)
        {
            AddInclude(x => x.FormAttachments);
        }
    }
}