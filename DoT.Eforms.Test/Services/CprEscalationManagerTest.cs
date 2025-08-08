using DoT.Eforms.Test.Shared;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels;
using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure.Interfaces;
using eforms_middleware.Constants;
using eforms_middleware.Interfaces;
using eforms_middleware.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace DoT.Eforms.Test;

public class CprEscalationManagerTest
{
    private Mock<IEmployeeService> _employeeService;
    private readonly CprEscalationManager _escalationManager;
    private Mock<IPermissionManager> _permissionManager;
    private Mock<ILogger<CprEscalationManager>> _logger;
    private Mock<IPositionService> _positionService;

    public CprEscalationManagerTest()
    {
        _employeeService = new Mock<IEmployeeService>();
        _permissionManager = new Mock<IPermissionManager>();
        _logger = new Mock<ILogger<CprEscalationManager>>();
        _positionService = new Mock<IPositionService>();
        _escalationManager = new CprEscalationManager(_employeeService.Object, _permissionManager.Object, _logger.Object, _positionService.Object);
        _employeeService.Setup(x => x.GetEmployeeByAzureIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid guid) => CoiTestEmployees.GetEmployeeDetails(guid));
        _employeeService.Setup(x =>
                x.GetEmployeeByPositionNumberAsync(It.Is<int>(x => x == ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID)))
            .ReturnsAsync(new List<IUserInfo>
            {
                new EmployeeDetailsDto {EmployeeEmail = "edp&c@email", EmployeePositionTitle = "ED P&C"}
            });
        _positionService
            .Setup(x => x.GetPositionByIdAsync(
                It.Is<int>(p => p == ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID), It.IsAny<bool>())).ReturnsAsync(
                new AdfPosition
                {
                    PositionTitle = "ED P&C",
                    AdfUserPositions = new List<AdfUser>
                    {
                        new () { EmployeeEmail = "edp&c@email" }
                    }
                });
    }

    [Fact]
    public async Task EscalateFormAsync_sets_correct_next_approver()
    {
        // ED less is probably what is required
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerViewManagerAction);
        var expectedPermission = new FormPermission
        { PositionId = ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID, PermissionFlag = (byte)PermissionFlag.UserActionable };

        var result = await _escalationManager.EscalateFormAsync(new FormInfo());

        Assert.Equal("normal_ed@email", result.UpdatedForm.NextApprover);
        Assert.Equal("Executive Director ", result.UpdatedForm.NextApprovalLevel);
        Assert.False(result.DoesEscalate);
        Assert.Equal(expectedPermission.PermissionFlag, result.PermissionUpdate.PermissionFlag);
        Assert.Equal(expectedPermission.UserId, result.PermissionUpdate.UserId);
    }

    [Fact]
    public async Task EscalateFormAsync_sets_correct_fields_when_Ed_p_and_c()
    {
        // ED less is probably what is required
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerViewEdAction);

        var expectedPermission = new FormPermission
        { PositionId = ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID, PermissionFlag = (byte)PermissionFlag.UserActionable };

        var result = await _escalationManager.EscalateFormAsync(new FormInfo());

        Assert.Equal("edp&c@email", result.UpdatedForm.NextApprover);
        Assert.Equal("ED P&C", result.UpdatedForm.NextApprovalLevel);
        Assert.False(result.DoesEscalate);
        Assert.Equal(expectedPermission.PermissionFlag, result.PermissionUpdate.PermissionFlag);
        Assert.Equal(expectedPermission.UserId, result.PermissionUpdate.UserId);
    }

    [Fact]
    public async Task EscalateFormAsync_sets_correct_fields_when_cancelling()
    {
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.EdlessOwnerViewManagerAction);
        var expectedPermission = new FormPermission
        { UserId = new Guid(CoiTestEmployees.EdlessEmployee), PermissionFlag = (byte)PermissionFlag.UserActionable, IsOwner = true };

        var result = await _escalationManager.EscalateFormAsync(new FormInfo());

        Assert.Null(result.UpdatedForm.NextApprover);
        Assert.Null(result.UpdatedForm.NextApprovalLevel);
        Assert.Equal((int)FormStatus.Unsubmitted, result.UpdatedForm.FormStatusId);
        Assert.Equal(Enum.GetName(FormStatus.Unsubmitted), result.UpdatedForm.FormSubStatus);
        Assert.False(result.DoesEscalate);
        Assert.Equal(expectedPermission.PermissionFlag, result.PermissionUpdate.PermissionFlag);
        Assert.Equal(expectedPermission.UserId, result.PermissionUpdate.UserId);
    }
}