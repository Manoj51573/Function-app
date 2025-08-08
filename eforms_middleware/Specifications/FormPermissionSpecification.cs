using System;
using System.Linq;
using DoT.Infrastructure.DbModels.Entities;

namespace eforms_middleware.Specifications;

public class FormPermissionSpecification : BaseQuerySpecification<FormPermission>
{
    public FormPermissionSpecification(int formId, byte? permissionFlag = null, Guid? adId = null,
        bool? filterOwner = null, bool addGroupMemberInfo = false, bool addPositionInfo = false, bool addUserInfo = false,
        bool addAttachments = false, bool addFormInfo = false, bool addTaskInfo = false, bool addGroupInfo = false)
    : base(x => x.FormId == formId && (!permissionFlag.HasValue || permissionFlag == x.PermissionFlag) &&
                (!adId.HasValue || x.UserId == adId || (x.GroupId.HasValue && x.Group.AdfGroupMembers.Any(y => y.MemberId == adId))
                || (x.PositionId.HasValue && x.Position.AdfUserPositions.Any(u => u.ActiveDirectoryId == adId))) &&
                (!filterOwner.HasValue || filterOwner == x.IsOwner), x => x.Id)
    {
        if (addGroupInfo)
        {
            AddInclude(x => x.Group);
        }
        
        if (addGroupMemberInfo)
        {
            AddInclude($"{nameof(FormPermission.Group)}.{nameof(AdfGroup.AdfGroupMembers)}.{nameof(AdfGroupMember.Member)}");
        }

        if (addPositionInfo)
        {
            AddInclude(x => x.Position.AdfUserPositions);
            AddInclude($"{nameof(FormPermission.Position)}.{nameof(AdfPosition.AdfUserPositions)}");
        }

        if (addUserInfo)
        {
            AddInclude(x => x.User.Position);
        }

        if (addAttachments)
        {
            AddInclude(x => x.FormAttachments);
        }

        if (addFormInfo)
        {
            AddInclude(x => x.Form);
        }

        if (addFormInfo && addTaskInfo)
        {
            AddInclude(x => x.Form.TaskInfos);
        }
    }
}