using System;
using System.Linq;
using DoT.Infrastructure.Interfaces;

namespace DoT.Infrastructure.DbModels.Entities;

public partial class FormPermission
{
    public FormPermission(byte permission, Guid? userId = null, int? positionId = null, Guid? groupId = null,
        bool isOwner = false)
    {
        PermissionFlag = permission;
        UserId = userId;
        PositionId = positionId;
        GroupId = groupId;
        IsOwner = isOwner;
    }
    
    public string Email => User?.EmployeeEmail ?? Position?.AdfUserPositions?.FirstOrDefault()?.EmployeeEmail ?? Group?.GroupEmail;
    public string Name => User?.EmployeeFullName ?? PositionOwnerString ?? Group?.GroupName;

    public string PositionOwnerString => Position?.AdfUserPositions.Count > 1
        ? Position.PositionTitle
        : Position?.AdfUserPositions?.FirstOrDefault()?.EmployeeFullName;

    public string PositionDescription => Position?.AdfUserPositions.Count == 1
        ? Position?.AdfUserPositions.Single().EmployeeEmail
        : Position?.PositionTitle;

    public string PermissionActionerDescription => User?.EmployeeEmail ?? PositionDescription ?? Group?.GroupName;

    public bool UserHasPermission(IUserInfo requestingUser)
    {
        return (UserId.HasValue && UserId == requestingUser.ActiveDirectoryId) ||
               (PositionId.HasValue && PositionId == requestingUser.EmployeePositionId) ||
               (GroupId.HasValue &&
                Group.AdfGroupMembers.Any(x => x.MemberId == requestingUser.ActiveDirectoryId));
    }
}