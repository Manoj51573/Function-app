using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Services;
using Moq;
using Xunit;

namespace DoT.Eforms.Test;

public class EscalationServiceTest
{
    private Mock<IFormInfoService> _formInfoService;
    private readonly EscalationService _service;
    private Mock<ITaskManager> _taskManager;
    private Mock<IEscalationFactoryService> _escalationFactoryService;
    private Mock<IFormHistoryService> _formHistoryService;
    private Mock<IPermissionManager> _permissionManager;
    private Mock<IWorkflowBtnService> _workflowManager;
    public EscalationServiceTest()
    {
        _formInfoService = new Mock<IFormInfoService>();
        _taskManager = new Mock<ITaskManager>();
        _escalationFactoryService = new Mock<IEscalationFactoryService>();
        _formHistoryService = new Mock<IFormHistoryService>();
        _permissionManager = new Mock<IPermissionManager>();
        _workflowManager = new Mock<IWorkflowBtnService>();
        _service = new EscalationService(_formInfoService.Object, _taskManager.Object, _escalationFactoryService.Object,
            _formHistoryService.Object, _permissionManager.Object, _workflowManager.Object);
    }
    
    [Fact]
    public async Task EscalateFormAsync_should_update_form()
    {
        var escalationManager = new Mock<EscalationManagerBase>();
        escalationManager.Setup(x => x.EscalateFormAsync(It.IsAny<FormInfo>())).ReturnsAsync((FormInfo formInfo) => new EscalationResult
        {
            UpdatedForm = formInfo, DoesEscalate = true, PermissionUpdate = new FormPermission()
        });
        _escalationFactoryService.Setup(x => x.GetEscalationManager(It.IsAny<FormType>()))
            .Returns(escalationManager.Object);
        _formInfoService
            .Setup(x => x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>()))
            .ReturnsAsync((FormInfoUpdate request, FormInfo dbRecord) => dbRecord);
        await _service.EscalateFormAsync(new TaskInfo
        {
            FormInfo = new FormInfo {AllFormsId = (int)FormType.CoI_CPR, FormStatusId = (int)FormStatus.Submitted, FormSubStatus = Enum.GetName(FormStatus.Submitted)}
        });
        _formInfoService.Verify(x => x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>()), Times.Once);
        _taskManager.Verify(x => x.AddFormTaskAsync(It.IsAny<TaskInfo>(), It.IsAny<TaskInfo>()), Times.Once);
        _permissionManager.Verify(x => x.UpdateFormPermissionsAsync(It.IsAny<int>(), It.IsAny<List<FormPermission>>()), Times.Once);
    }

    [Theory]
    [InlineData(true, 5)]
    [InlineData(false, null)]
    public async Task EscalateFormAsync_should_create_right_task(bool expectedEscalation, int? datetimeOffset)
    {
        var expectedEscalationDate = datetimeOffset.HasValue ? DateTime.Today.AddDays(datetimeOffset.Value) : (DateTime?)null;
        var escalationManager = new Mock<EscalationManagerBase>();
        escalationManager.Setup(x => x.EscalateFormAsync(It.IsAny<FormInfo>())).ReturnsAsync((FormInfo formInfo) => new EscalationResult
        {
            UpdatedForm = formInfo, DoesEscalate = expectedEscalation, PermissionUpdate = new FormPermission()
        });
        _escalationFactoryService.Setup(x => x.GetEscalationManager(It.IsAny<FormType>()))
            .Returns(escalationManager.Object);
        _formInfoService
            .Setup(x => x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>()))
            .ReturnsAsync((FormInfoUpdate request, FormInfo dbRecord) => dbRecord);
        await _service.EscalateFormAsync(new TaskInfo
        {
            FormInfo = new FormInfo {AllFormsId = (int)FormType.CoI_CPR, FormStatusId = (int)FormStatus.Submitted, FormSubStatus = Enum.GetName(FormStatus.Submitted)}
        });
        _taskManager.Verify(x => x.AddFormTaskAsync(It.IsAny<TaskInfo>(),
            It.Is<TaskInfo>(t => t.Escalation == expectedEscalation && t.EscalationDate == expectedEscalationDate)), Times.Once);
        _permissionManager.Verify(x => x.UpdateFormPermissionsAsync(It.IsAny<int>(), It.IsAny<List<FormPermission>>()), Times.Once);
    }

    [Fact]
    public async Task EscalateFormAsync_should_not_create_task_when_cancelling()
    {
        var escalationManager = new Mock<EscalationManagerBase>();
        escalationManager.Setup(x => x.EscalateFormAsync(It.IsAny<FormInfo>())).ReturnsAsync((FormInfo formInfo) =>
        {
            formInfo.FormStatusId = (int)FormStatus.Unsubmitted;
            formInfo.FormSubStatus = Enum.GetName(FormStatus.Unsubmitted);
            return new EscalationResult
            {
                UpdatedForm = formInfo, DoesEscalate = false, PermissionUpdate = new FormPermission()
            };
        });
        _escalationFactoryService.Setup(x => x.GetEscalationManager(It.IsAny<FormType>()))
            .Returns(escalationManager.Object);
        _formInfoService
            .Setup(x => x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>()))
            .ReturnsAsync((FormInfoUpdate request, FormInfo dbRecord) => dbRecord);
        var result = await _service.EscalateFormAsync(new TaskInfo
        {
            FormInfo = new FormInfo {AllFormsId = (int)FormType.CoI_CPR, FormStatusId = (int)FormStatus.Submitted, FormSubStatus = Enum.GetName(FormStatus.Submitted)}
        });
        _formInfoService.Verify(x => x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>()), Times.Once);
        _taskManager.Verify(x => x.AddFormTaskAsync(It.IsAny<TaskInfo>(), null), Times.Once);
        _permissionManager.Verify(x => x.UpdateFormPermissionsAsync(It.IsAny<int>(), It.IsAny<List<FormPermission>>()), Times.Once);
    }
}