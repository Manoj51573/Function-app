using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.Interfaces;
using eforms_middleware.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DoT.Eforms.Test;

public class PermissionManagerTest
{
    private Mock<IRepository<FormPermission>> _permissionRepo;
    private readonly PermissionManager _service;
    private Mock<IRequestingUserProvider> _userProvider;
    private Guid _requestorActiveDirectoryId;
    private Mock<ILogger<PermissionManager>> _logger;

    public PermissionManagerTest()
    {
        _requestorActiveDirectoryId = new Guid("7C90A77B-BEBF-4264-A882-038FADF63EFB");
        _permissionRepo = new Mock<IRepository<FormPermission>>();
        _userProvider = new Mock<IRequestingUserProvider>();
        _logger = new Mock<ILogger<PermissionManager>>();
        _service = new PermissionManager(_permissionRepo.Object, _userProvider.Object, _logger.Object );
        _permissionRepo.Setup(x => x.ListAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(new List<FormPermission>());
        _userProvider.Setup(x => x.GetRequestingUser()).ReturnsAsync(new EmployeeDetailsDto { ActiveDirectoryId = _requestorActiveDirectoryId });
    }
    
    // When Adding permissions
    // should create when no permissions
    // should create a read permission for User who performs an action
    // should set existing permission to view only unless creating owner permission
    [Fact]
    public async Task AddPermissionAsync_when_no_permissions_Should_create_new_permission()
    {
        _permissionRepo.Setup(x => x.AddAsync(It.IsAny<FormPermission>())).ReturnsAsync((FormPermission permission) =>
        {
            permission.Id = 1;
            return permission;
        });
        
        var result = await _service.UpdateFormPermissionsAsync(1, new List<FormPermission>{ new FormPermission{FormId = 1, PermissionFlag = (byte)PermissionFlag.UserActionable, IsOwner = true, UserId = _requestorActiveDirectoryId}});
        
        Assert.Equal(1, result);
        _permissionRepo.Verify(x => x.AddAsync(It.IsAny<FormPermission>()), Times.Once);
        _permissionRepo.Verify(x => x.Update(It.IsAny<FormPermission>()), Times.Never);
    }

    [Fact]
    public async Task AddPermissionAsync_does_not_create_User_Permission_if_not_required()
    {
        
    }

    [Fact]
    public async Task AddPermissionAsync_when_existing_permission_Should_set_old_permission_to_read_only()
    {
        _permissionRepo.Setup(x => x.ListAsync(It.IsAny<ISpecification<FormPermission>>())).ReturnsAsync(
            new List<FormPermission>
            {
                new() {PermissionFlag = 0x3, PositionId = 1}
            });
        _permissionRepo.Setup(x => x.AddAsync(It.IsAny<FormPermission>())).ReturnsAsync(new FormPermission { Id = 1 });

        await _service.UpdateFormPermissionsAsync(1, new List<FormPermission>{new FormPermission{FormId = 1, PermissionFlag = (byte)PermissionFlag.UserActionable, IsOwner = false, PositionId = 2}});
        
        _permissionRepo.Verify(x => x.Update(It.Is<FormPermission>(p => 
            p.PermissionFlag == (byte)PermissionFlag.View)), Times.Once);
        _permissionRepo.Verify(x => x.AddAsync(It.Is<FormPermission>(p => 
            p.FormId == 1 && p.PermissionFlag == (byte)PermissionFlag.View && p.UserId == _requestorActiveDirectoryId)), Times.Once);
    }
    
    // If permission for group, position, or user already exists
    // shouldn't create a new record
    // should update the permission of the existing record
    [Fact]
    public async Task AddPermissionAsync_when_permission_entry_exists_Should_update_old_permission()
    {
        _permissionRepo.Setup(x => x.ListAsync(It.IsAny<ISpecification<FormPermission>>())).ReturnsAsync(
            new List<FormPermission>
            {
                new() {PermissionFlag = (byte)PermissionFlag.View, PositionId = 1},
                new() {PermissionFlag = (byte)PermissionFlag.View, UserId = _requestorActiveDirectoryId}
            });
        
        await _service.UpdateFormPermissionsAsync(1, new List<FormPermission>{new FormPermission{FormId = 1, PermissionFlag = (byte)PermissionFlag.UserActionable, IsOwner = false, PositionId = 1}});
        
        _permissionRepo.Verify(x => x.Update(It.Is<FormPermission>(p => 
            p.PermissionFlag == (byte)PermissionFlag.UserActionable && p.PositionId == 1)));
        _permissionRepo.Verify(x => x.AddAsync(It.IsAny<FormPermission>()), Times.Never);
    }
    
    // When completing a form should set all permissions to read only
    [Fact]
    public async Task UpdateFormPermissionsAsync_when_no_new_permission_Should_update_existing_permission()
    {
        _permissionRepo.Setup(x => x.ListAsync(It.IsAny<ISpecification<FormPermission>>())).ReturnsAsync(
            new List<FormPermission>
            {
                new() {PermissionFlag = (byte)PermissionFlag.View, PositionId = 1},
                new() {PermissionFlag = (byte)PermissionFlag.UserActionable, UserId = _requestorActiveDirectoryId}
            });
        
        await _service.UpdateFormPermissionsAsync(1,new List<FormPermission>{new FormPermission{FormId = 1, PermissionFlag = (byte)PermissionFlag.View, IsOwner = false}});
        
        _permissionRepo.Verify(x => x.Update(It.Is<FormPermission>(p => 
            p.PermissionFlag == (byte)PermissionFlag.View && p.UserId == _requestorActiveDirectoryId)));
        _permissionRepo.Verify(x => x.AddAsync(It.IsAny<FormPermission>()), Times.Never);
    }
}