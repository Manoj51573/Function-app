using System.Collections.Generic;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.Interfaces;
using eforms_middleware.Services;
using Moq;
using Xunit;

namespace DoT.Eforms.Test;

public class CoiOtherEscalationTest
{
    private Mock<IEmployeeService> _employeeService;
    private readonly CoiOtherEscalationService _service;
    private Mock<IPermissionManager> _permissionManager;

    public CoiOtherEscalationTest()
    {
        _employeeService = new Mock<IEmployeeService>();
        _permissionManager = new Mock<IPermissionManager>();
        _service = new CoiOtherEscalationService(_employeeService.Object, _permissionManager.Object);
    }
    
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DoesEscalate_returns_correct_result_for_form(bool canEscalate)
    {
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(
                () => canEscalate
                    ? new List<FormPermission> { new((byte)PermissionFlag.UserActionable, positionId: 2) }
                    : new List<FormPermission>
                    {
                        new((byte)PermissionFlag.UserActionable, groupId: ConflictOfInterest.ODG_AUDIT_GROUP_ID)
                    });
        var formInfo = new FormInfo{ FormInfoId = 3};
        var willEscalate = await _service.DoesEscalate(formInfo);
        Assert.Equal(canEscalate, willEscalate);
    }
}