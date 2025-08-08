using System;
using System.Collections.Generic;
using System.Linq;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.Specifications;
using Xunit;

namespace DoT.Eforms.Test.Specifications;

public class FormPermissionSpecificationTest
{
    [Theory]
    [MemberData(nameof(TestParams))]
    public void MatchesExpectedNumberOfItems(FormPermissionSpecification otherThanViewSpecification)
    {
        var result = GetTestCollection()
            .AsQueryable()
            .Where(otherThanViewSpecification.Criteria);

        var formInfo = Assert.Single(result);
    }

    public static IEnumerable<object[]> TestParams()
    {
        var userGuid = new Guid("1F4BC577-78A3-4778-9EFA-2C6993CCD0E0");
        var positionOccupant = new Guid("E8D7AF83-D818-4FCD-B6B7-3D0DFEB3BDAF");
        var groupMember = new Guid("7D22B670-AC6D-40AD-B1D6-305914F3936A");
        return new []
        {
            new object[] { new FormPermissionSpecification(1, null, adId: userGuid) },
            new object[] { new FormPermissionSpecification(1, (byte)PermissionFlag.UserActionable) },
            new object[] { new FormPermissionSpecification(1, null, adId: positionOccupant) },
            new object[] { new FormPermissionSpecification(1, null, adId: groupMember)}
        };
    }

    private static IEnumerable<FormPermission> GetTestCollection()
    {
        return new List<FormPermission>
        {
            new ()
            {
                Id = 1, FormId = 1, PermissionFlag = (byte)PermissionFlag.View, PositionId = 1, IsOwner = true,
                Group = null,
                Position = new AdfPosition
                {
                    AdfUserPositions = new List<AdfUser>
                        { new() { ActiveDirectoryId = Guid.Parse("E8D7AF83-D818-4FCD-B6B7-3D0DFEB3BDAF") } }
                },
                UserId = Guid.NewGuid(), GroupId = null
            },
            new ()
            {
                Id = 2, FormId = 1, PermissionFlag = (byte)PermissionFlag.View, UserId = Guid.Parse("1F4BC577-78A3-4778-9EFA-2C6993CCD0E0"), IsOwner = false,
                Group = null, Position = null, PositionId = null, GroupId = null
            },
            new ()
            {
                Id = 3, FormId = 1, PermissionFlag = (byte)PermissionFlag.UserActionable, GroupId = Guid.NewGuid(), IsOwner = false,
                Group = new AdfGroup {AdfGroupMembers = new List<AdfGroupMember>{new (){MemberId = Guid.Parse("7D22B670-AC6D-40AD-B1D6-305914F3936A")}}}, Position = null
            },
            new ()
            {
                Id = 4, FormId = 2, PermissionFlag = (byte)PermissionFlag.UserActionable, UserId = Guid.NewGuid(), IsOwner = true, Group = null, Position = null, PositionId = null, GroupId = null
            },
        };
    }
}