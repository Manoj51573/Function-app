using DoT.Eforms.Test.Shared;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels;
using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure.Interfaces;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Services;
using eforms_middleware.Validators;
using eforms_middleware.Workflows;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace DoT.Eforms.Test;

public class CoIOtherFormServiceTest
{
    private readonly CoIFormService<CoIOther> _service;
    private readonly Mock<ITaskManager> _mockTaskManager;
    private readonly Mock<IRepository<RefFormStatus>> _mockFormStatusRepository;
    private readonly Mock<IFormHistoryService> _mockFormHistoryService;
    private readonly Mock<IFormInfoService> _mockFormInfoService;
    private readonly Mock<IMessageFactoryService> _mockMessageFactory;
    private readonly Mock<IEmployeeService> _mockEmployeeService;
    private Mock<ILogger<CoIFormService<CoIOther>>> _log;

    public CoIOtherFormServiceTest()
    {
        _mockTaskManager = new Mock<ITaskManager>();
        _mockFormStatusRepository = new Mock<IRepository<RefFormStatus>>();
        _mockFormHistoryService = new Mock<IFormHistoryService>();
        _mockFormInfoService = new Mock<IFormInfoService>();
        _mockMessageFactory = new Mock<IMessageFactoryService>();
        _mockEmployeeService = new Mock<IEmployeeService>();
        var employeeFormValidator = new CoIOtherRequesterFormValidator();
        var rejectionValidator = new CoiRejectionValidator();
        var coiManagerFormValidator = new CoiManagerFormValidator();
        var endorserValidator = new CoiEndorsementFormValidator();
        var coiOtherGovOdgValidator = new CoiOtherGovOdgReviewFormValidator();
        var coiOtherFormHelper = new CoIOtherSubtypeHelper(employeeFormValidator, rejectionValidator,
            coiManagerFormValidator, endorserValidator, coiOtherGovOdgValidator);

        _log = new Mock<ILogger<CoIFormService<CoIOther>>>();
        _requestingUserProvider = new Mock<IRequestingUserProvider>();
        _permissionManager = new Mock<IPermissionManager>();
        _positionService = new Mock<IPositionService>();
        _service = new CoIFormService<CoIOther>(_log.Object, _mockTaskManager.Object,
            _mockFormStatusRepository.Object, _mockFormHistoryService.Object,
            _mockFormInfoService.Object, _mockMessageFactory.Object,
            _mockEmployeeService.Object, coiOtherFormHelper, _requestingUserProvider.Object, _permissionManager.Object, _positionService.Object);

        _mockFormInfoService.Setup(x =>
            x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>())).Returns(
            (FormInfoUpdate request, FormInfo dbRecord) => Task.FromResult(dbRecord));
        _mockEmployeeService.Setup(x => x.GetEmployeeByAzureIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid employeeId) => CoiTestEmployees.GetEmployeeDetails(employeeId));

        _mockEmployeeService
            .Setup(x => x.GetEmployeeByPositionNumberAsync(It.Is<int>(p =>
                p == ConflictOfInterest.ED_ODG_POSITION_ID))).ReturnsAsync(new List<IUserInfo>
                {
                    new EmployeeDetailsDto
                    {
                        EmployeeEmail = "EdOdg@email.com", EmployeePositionId = ConflictOfInterest.ED_ODG_POSITION_ID
                    }
                });
        _mockFormInfoService.Setup(x => x.GetExistingOrNewFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<bool>())).ReturnsAsync(
            (FormInfoUpdate request, bool addAttachments) =>
            {
                switch (request.FormDetails.FormInfoId)
                {
                    case null:
                        return new FormInfo { AllFormsId = (int)FormType.CoI_Other };
                    case 1:
                        return new FormInfo { FormInfoId = 1, FormStatusId = (int)FormStatus.Unsubmitted, AllFormsId = (int)FormType.CoI_Other };
                    case 2:
                        return new FormInfo { FormInfoId = 2, FormStatusId = (int)FormStatus.Submitted, AllFormsId = (int)FormType.CoI_Other, FormOwnerEmail = "normal.employee@email", NextApprover = "manager@email" };
                    case 3:
                        return new FormInfo { FormInfoId = 3, FormStatusId = (int)FormStatus.Approved, AllFormsId = (int)FormType.CoI_Other };
                    case 4:
                        return new FormInfo { FormInfoId = 4, FormStatusId = (int)FormStatus.Endorsed, AllFormsId = (int)FormType.CoI_Other };
                }

                return null;
            });
        _requestingUserProvider.Setup(x => x.GetRequestingUser()).ReturnsAsync(new EmployeeDetailsDto());
    }

    [Theory]
    [MemberData(nameof(HappyPathData))]
    public async Task Happy_path_should_work(FormStatus formStatus, int formInfoId, string response,
        string nextApprover, Guid requestingUserId, string expectedNextApprover, FormStatus expectedFormStatus)
    {
        _requestingUserProvider.Setup(x => x.GetRequestingUser())
            .ReturnsAsync(CoiTestEmployees.GetEmployeeDetails(requestingUserId));
        var formInfo = new FormInfoUpdate
        {
            FormAction = Enum.GetName(formStatus),
            FormDetails = new FormDetailsRequest
            {
                FormInfoId = formInfoId,
                AllFormsId = (int)FormType.CoI_Other,
                Response = response,
                NextApprover = nextApprover
            }
        };

        var result = await _service.ProcessRequest(formInfo);

        Assert.True(result.Success);
        _mockFormInfoService.Verify(x => x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.Is<FormInfo>(f =>
            f.FormStatusId == (int)expectedFormStatus &&
            f.FormSubStatus == Enum.GetName(expectedFormStatus) &&
            f.NextApprover == expectedNextApprover)), Times.Once);
    }

    [Fact]
    public async Task Complete_should_create_a_Task_for_reminder()
    {
        var formInfo = new FormInfoUpdate
        {
            FormAction = FormStatus.Completed.ToString(),
            FormDetails = new FormDetailsRequest
            {
                FormInfoId = 4,
                AllFormsId = (int)FormType.CoI_Other,
                Response = JsonConvert.SerializeObject(ValidOdgReview),
                NextApprover = null
            }
        };

        await _service.ProcessRequest(formInfo);

        _mockTaskManager.Verify(
            x => x.AddFormTaskAsync(It.Is<int>(t => t == 4), It.Is<TaskInfo>(t => t.SpecialReminderDate == DateTime.Today.AddDays(1) &&
                                                         t.EscalationDate == null && t.Escalation == false && t.SpecialReminder == true &&
                                                         t.TaskStatus == FormStatus.Completed.ToString() && t.FormInfoId == 4 &&
                                                         t.SpecialReminderTo == ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL)), Times.Once);
    }

    [Fact]
    public async Task IndependentReview_fails_when_not_valid_guid()
    {
        var formInfo = new FormInfoUpdate
        {
            FormAction = FormStatus.IndependentReview.ToString(),
            FormDetails = new FormDetailsRequest
            {
                FormInfoId = 4,
                AllFormsId = (int)FormType.CoI_Other,
                Response = null,
                NextApprover = "not-a-guid"
            }
        };

        var result = await _service.ProcessRequest(formInfo);

        Assert.False(result.Success);
        _mockFormInfoService.Verify(
            x => x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(),
                It.IsAny<FormInfo>()), Times.Never);
        _mockTaskManager.Verify(x => x.AddFormTaskAsync(It.IsAny<int>(), It.IsAny<TaskInfo>()), Times.Never);
        _mockFormHistoryService.Verify(
            x => x.AddFormHistoryAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>(), It.Is<Guid?>(y => !y.HasValue),
                It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task IndependentReview_should_be_available_at_endorse()
    {
        _mockEmployeeService.Setup(x => x.GetEmployeeByAzureIdAsync(It.IsAny<Guid>())).ReturnsAsync(new EmployeeDetailsDto
        {
            EmployeeEmail = "ExpectedApproverEmail",
            EmployeePositionId = 1
        });
        var formInfo = new FormInfoUpdate
        {
            FormAction = FormStatus.IndependentReview.ToString(),
            FormDetails = new FormDetailsRequest
            {
                FormInfoId = 4,
                AllFormsId = (int)FormType.CoI_Other,
                Response = null,
                NextApprover = "67e06f22-9733-45ae-8308-f8ba64a96a64"
            }
        };

        await _service.ProcessRequest(formInfo);

        _mockFormInfoService.Verify(
            x => x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(),
                It.Is<FormInfo>(f => f.NextApprover == "ExpectedApproverEmail" &&
                                     f.FormStatusId == (int)FormStatus.IndependentReview &&
                                     f.FormSubStatus == Enum.GetName(FormStatus.IndependentReview))), Times.Once);
        _mockTaskManager.Verify(x => x.AddFormTaskAsync(It.IsAny<int>(), It.IsAny<TaskInfo>()), Times.Once);
        _mockFormHistoryService.Verify(
            x => x.AddFormHistoryAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>(), It.Is<Guid?>(y => !y.HasValue),
               It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ProcessRequest_returns_404_if_no_employee_found()
    {
        _mockEmployeeService.Setup(x => x.GetEmployeeByAzureIdAsync(It.IsAny<Guid>())).ReturnsAsync(default(EmployeeDetailsDto));
        var formInfo = new FormInfoUpdate
        {
            FormAction = FormStatus.IndependentReview.ToString(),
            FormDetails = new FormDetailsRequest
            {
                FormInfoId = 4,
                AllFormsId = (int)FormType.CoI_Other,
                Response = null,
                NextApprover = "67e06f22-9733-45ae-8308-f8ba64a96a64"
            }
        };

        var result = await _service.ProcessRequest(formInfo);

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
        _mockFormInfoService.Verify(
            x => x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(),
                It.IsAny<FormInfo>()), Times.Never);
        _mockTaskManager.Verify(x => x.AddFormTaskAsync(It.IsAny<int>(), It.IsAny<TaskInfo>()), Times.Never);
        _mockFormHistoryService.Verify(
            x => x.AddFormHistoryAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>(), It.IsAny<Guid?>(),
                It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private static readonly CoIOtherRequesterForm ValidRequesterForm = new()
    {
        ConflictType = ConflictOfInterest.ConflictType.NoConflict,
        PeriodOfConflict = ConflictOfInterest.EngagementFrequency.Ongoing,
        From = DateTime.Today,
        DeclarationDetails = "Something",
        IsDeclarationAcknowledged = true
    };

    private static readonly CoiManagerForm ValidManagerForm = new()
    {
        ConflictType = ConflictOfInterest.ConflictType.NoConflict,
        IsDeclarationAcknowledged = true
    };

    private static readonly CoiEndorsementForm ValidTier3Form = new()
    {
        AdditionalComments = "Some comment"
    };

    private static readonly CoiOtherGovOdgReview ValidOdgReview = new()
    {
        NatureOfConflict = ConflictOfInterest.NatureOfConflict.Financial,
        CoiArea = ConflictOfInterest.CoiArea.Grants,
        ObjFileRef = "Test",
        AdditionalComments = "Some comments",
        ReminderDate = DateTime.Today.AddDays(1)
    };

    private Mock<IRequestingUserProvider> _requestingUserProvider;
    private Mock<IPermissionManager> _permissionManager;
    private Mock<IPositionService> _positionService;

    public static object[][] HappyPathData => new[]
    {
        new object[] { FormStatus.Submitted, 1, JsonConvert.SerializeObject(ValidRequesterForm), "ED",
                        Guid.Parse(CoiTestEmployees.NormalEmployee), "ToManager@email", FormStatus.Submitted },
        new object[] { FormStatus.Submitted, 1, JsonConvert.SerializeObject(ValidRequesterForm), null, Guid.Parse(CoiTestEmployees.NormalEmployee),
                        "ToManager@email",
                        FormStatus.Submitted},
        new object[] { FormStatus.Approved, 2, JsonConvert.SerializeObject(ValidManagerForm), null, Guid.Parse(CoiTestEmployees.NormalManager), "ED@email",
                        FormStatus.Approved},
        new object[] { FormStatus.Endorsed, 3, JsonConvert.SerializeObject(ValidTier3Form), null, Guid.Parse(CoiTestEmployees.NormalExecutiveDirector), ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL,
                        FormStatus.Endorsed},
        new object[] { FormStatus.Completed, 4, JsonConvert.SerializeObject(ValidOdgReview), null, null, null, FormStatus.Completed },
    };
}