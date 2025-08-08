using DoT.Eforms.Test.Shared;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels;
using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure.Interfaces;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.MessageBuilders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace DoT.Eforms.Test.MessageBuilders;

public class CprMessageBuilderTest
{
    private readonly Mock<IEmployeeService> _employeeService;
    private readonly Mock<ILogger<CprMessageBuilder>> _logger;
    private readonly CprMessageBuilder _builder;
    private readonly Mock<IRequestingUserProvider> _requestingUserProvider;
    private readonly Mock<IPermissionManager> _permissionManager;

    public CprMessageBuilderTest()
    {
        _employeeService = new Mock<IEmployeeService>();
        _logger = new Mock<ILogger<CprMessageBuilder>>();
        var inMemorySettings = new Dictionary<string, string>
        {
            {"BaseEformsUrl", "https://base-url"},
            {"FromEmail", "forms-dev@transport.wa.gov.au"}
        };
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();
        _requestingUserProvider = new Mock<IRequestingUserProvider>();
        _permissionManager = new Mock<IPermissionManager>();
        _builder = new CprMessageBuilder(_logger.Object, configuration, _requestingUserProvider.Object, _permissionManager.Object, _employeeService.Object);
        _employeeService.Setup(x => x.GetEmployeeByAzureIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => CoiTestEmployees.GetEmployeeDetails(id));
        _requestingUserProvider.Setup(x => x.GetRequestingUser()).ReturnsAsync(new EmployeeDetailsDto { EmployeePositionId = 62 });
        _employeeService
            .Setup(x => x.GetEmployeeByPositionNumberAsync(It.Is<int>(p =>
                p == ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID))).ReturnsAsync(new List<IUserInfo>
                {
                    new EmployeeDetailsDto()
                    {
                        EmployeeEmail = "edP&C@email.com", EmployeeFirstName = "ED", EmployeePreferredName = "ED", EmployeeSurname = "P&C",
                        EmployeePositionId = ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID
                    }
                });
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerViewManagerAction);
    }

    [Fact]
    public async Task ShouldProduceExpectedTemplate()
    {
        _requestingUserProvider.Setup(x => x.GetRequestingUser())
            .ReturnsAsync(CoiTestEmployees.GetEmployeeDetails(new Guid(CoiTestEmployees.NormalEmployee)));
        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "employee@email",
            FormOwnerName = "Test Employee",
            NextApprover = "ToManager@email",
            FormStatusId = (int)FormStatus.Submitted
        };
        var request = new FormInfoUpdate { FormAction = Enum.GetName(FormStatus.Submitted), FormDetails = new FormDetailsRequest() };
        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        Assert.Collection(result,
            message =>
            {
                Assert.Equal("ToManager@email", message.To[0].Address);
                Assert.Equal(EXPECTED_SUBMITTED_MANAGER_BODY, message.Body);
                Assert.Equal("Close personal relationship - Conflict of Interest declaration for your review", message.Subject);
            });
        _logger.VerifyLogging("", LogLevel.Error, Times.Never(), true);
    }

    [Fact]
    public async Task Should_submit_to_ED_when_no_manager()
    {
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerViewEdAction);
        _requestingUserProvider.Setup(x => x.GetRequestingUser())
            .ReturnsAsync(CoiTestEmployees.GetEmployeeDetails(new Guid(CoiTestEmployees.ManagerlessEmployee)));
        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "managereless@email",
            FormOwnerName = "Test Employee",
            NextApprover = "ED@email",
            FormStatusId = (int)FormStatus.Submitted
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Submitted),
            FormDetails = new FormDetailsRequest()
        };
        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        Assert.Collection(result,
            message =>
            {
                Assert.Equal(EXPECTED_TIER3_NO_MANAGER_BODY, message.Body);
                Assert.Equal("Close personal relationship - Conflict of interest declaration - for approval", message.Subject);
            });
        _logger.VerifyLogging("", LogLevel.Error, Times.Never(), true);
    }

    [Fact]
    public async Task Should_submit_to_ED_PandC_when_requested()
    {
        _requestingUserProvider.Setup(x => x.GetRequestingUser())
            .ReturnsAsync(CoiTestEmployees.GetEmployeeDetails(new Guid(CoiTestEmployees.NormalEmployee)));
        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "employee@email",
            FormOwnerName = "Test Employee",
            NextApprover = "ED@email",
            FormStatusId = (int)FormStatus.Submitted
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Submitted),
            FormDetails = new FormDetailsRequest { NextApprover = "ED" }
        };
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(new List<FormPermission>
            {
                new((byte)PermissionFlag.View, userId: new Guid(CoiTestEmployees.NormalEmployee), isOwner: true),
                new((byte)PermissionFlag.UserActionable,
                    positionId: ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID)
            });
        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        Assert.Collection(result,
            message =>
            {
                Assert.Equal("edP&C@email.com", message.To[0].Address);
                Assert.Equal(EXPECTED_SUBMIT_TO_ED_P_AND_C_BODY, message.Body);
                Assert.Equal("Close personal relationship - Conflict of interest declaration - for approval",
                    message.Subject);
            },
            message =>
            {
                Assert.Equal(EXPECTED_SUBMITTED_TO_ED_P_AND_C_BODY, message.Body);
                Assert.Equal("Close personal relationship - Conflict of interest declaration submitted",
                    message.Subject);
            });
        _logger.VerifyLogging("", LogLevel.Error, Times.Never(), true);
    }

    [Fact]
    public async Task ShouldLogErrors()
    {
        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "broken_employee@email",
            FormOwnerName = "Test Employee",
            NextApprover = "NotED@email",
            FormStatusId = (int)FormStatus.Submitted
        };
        var request = new FormInfoUpdate { FormAction = Enum.GetName(FormStatus.Submitted) };
        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        Assert.Empty(result);
        _logger.VerifyLogging("", LogLevel.Error, Times.Once(), true);
    }

    [Fact]
    public async Task ShouldSendApproved()
    {
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(new List<FormPermission>
            {
                new((byte)PermissionFlag.View, userId: new Guid(CoiTestEmployees.NormalEmployee), isOwner: true),
                new((byte)PermissionFlag.UserActionable, positionId: 3)
            });
        _employeeService.Setup(x => x.GetEmployeeByPositionNumberAsync(It.Is<int>(x => x == 3))).ReturnsAsync(
            new List<IUserInfo>
            {
                CoiTestEmployees.GetEmployeeDetails(new Guid(CoiTestEmployees.NormalExecutiveDirector))
            });
        _requestingUserProvider.Setup(x => x.GetRequestingUser())
            .ReturnsAsync(CoiTestEmployees.GetEmployeeDetails(new Guid(CoiTestEmployees.NormalManager)));
        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "employee@email",
            FormOwnerName = "Test Employee",
            NextApprover = "Tier3@email"
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Approved),
            FormDetails = new FormDetailsRequest()
        };
        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        var message = Assert.Single(result);
        Assert.Equal("normal_ed@email", message.To[0].Address);
        Assert.Equal("Close personal relationship - Conflict of interest declaration - for approval", message.Subject);
        Assert.Equal(EXPECTED_APPROVED_BODY, message.Body);
    }

    [Fact]
    public async Task ShouldSendRejected()
    {
        _requestingUserProvider.Setup(x => x.GetRequestingUser())
            .ReturnsAsync(CoiTestEmployees.GetEmployeeDetails(new Guid(CoiTestEmployees.NormalManager)));
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerActionManagerViewEdView);
        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "employee@email",
            FormOwnerName = "Test Employee",
            Response = JsonConvert.SerializeObject(new ClosePersonalRelationship())
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Rejected),
            FormDetails = new FormDetailsRequest()
        };
        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        var message = Assert.Single(result);
        Assert.Equal("employee@email", message.To[0].Address);
        Assert.Equal("Close personal relationship - Conflict of interest declaration not approved by line manager", message.Subject);
        Assert.Equal(EXPECTED_REJECTION_BODY, message.Body);
    }

    [Fact]
    public async Task Should_send_Endorsed()
    {
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerViewEdPAndCAction);
        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "employee@email",
            FormOwnerName = "Test Employee",
            NextApprover = "edP&C@email"
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Endorsed),
            FormDetails = new FormDetailsRequest()
        };
        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        var message = Assert.Single(result);
        Assert.Equal("edP&C@email.com", message.To[0].Address);
        Assert.Equal("Close personal relationship - Conflict of interest declaration - for approval", message.Subject);
        Assert.Equal(EXPECTED_ENDORSED_BODY, message.Body);
    }

    [Fact]
    public async Task Should_send_Rejected_when_not_supported()
    {
        _requestingUserProvider.Setup(x => x.GetRequestingUser()).ReturnsAsync(new EmployeeDetailsDto
        {
            EmployeePositionId = 5
        });
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerActionManagerViewEdView);
        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "employee@email",
            FormOwnerName = "Test Employee",
            NextApprover = null,
            ModifiedBy = "ED@email",
            Response = JsonConvert.SerializeObject(new ClosePersonalRelationship { ManagerForm = new CoiManagerForm { ConflictType = ConflictOfInterest.ConflictType.Actual } })
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Rejected),
            FormDetails = new FormDetailsRequest()
        };
        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        Assert.Collection(result, mailMessage =>
        {
            Assert.Equal("employee@email", mailMessage.To[0].Address);
            Assert.Equal(
                "Close personal relationship - Conflict of interest declaration not approved by senior management",
                mailMessage.Subject);
            Assert.Equal(EXPECTED_NOT_SUPPORTED_BODY, mailMessage.Body);
        }, mailMessage =>
        {
            Assert.Equal("ToManager@email", mailMessage.To[0].Address);
            Assert.Equal(
                "Close personal relationship - Conflict of interest declaration not approved by senior management",
                mailMessage.Subject);
            Assert.Equal(EXPECTED_NOT_SUPPORTED_TO_MANAGER, mailMessage.Body);
        });
    }

    [Theory]
    [MemberData(nameof(GetData), 0, 2)]
    public async Task Should_send_Approved_and_Completed_Message(List<FormPermission> permissions, string expectedTo, bool isMultipleMails)
    {
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(permissions);
        _requestingUserProvider.Setup(x => x.GetRequestingUser()).ReturnsAsync(new EmployeeDetailsDto
        {
            EmployeeFirstName = "Wrong",
            EmployeePreferredName = "Isabeau",
            EmployeeSurname = "Korpel"
        });

        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "employee@email",
            FormOwnerName = "Test Employee",
            NextApprover = null
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Completed),
            FormDetails = new FormDetailsRequest()
        };
        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        if (!isMultipleMails)
        {
            var message = Assert.Single(result);
            Assert.Equal(expectedTo, message.To[0].Address);
            Assert.Equal("Close personal relationship - Conflict of interest declaration outcome", message.Subject);
            Assert.Equal(EXPECTED_COMPLETED_BODY, message.Body);
        }
        else
        {
            Assert.Collection(result, message =>
            {
                Assert.Equal(expectedTo, message.To[0].Address);
                Assert.Equal("Close personal relationship - Conflict of interest declaration outcome", message.Subject);
                Assert.Equal(EXPECTED_COMPLETED_BODY, message.Body);
            }, message =>
            {
                Assert.Equal("ToManager@email", message.To[0].Address);
                Assert.Equal("Close personal relationship - Conflict of interest declaration outcome", message.Subject);
                Assert.Equal(EXPECTED_COMPLETED_TO_MANAGER_BODY, message.Body);
            });
        }
    }

    public static IEnumerable<object[]> GetData(int skip = 0, int take = 1)
    {
        var list = new List<object[]>
        {
            new object[] {CoiTestPermissions.ManagerlessOwnerView, "managerless_employee@email", false},
            new object[] {CoiTestPermissions.OwnerManagerChanged, "employee@email", true},
            new object[] {CoiTestPermissions.OwnerViewManagerEdAction, "Manager Employee", "ToManager@email"},
            new object[] {CoiTestPermissions.OwnerViewManagerEdAction, "Manager Employee", "ToManager@email"}
        };

        return list.Skip(skip).Take(take);
    }

    [Fact]
    public async Task Recall_returns_expected_email()
    {
        _requestingUserProvider.Setup(x => x.GetRequestingUser())
            .ReturnsAsync(CoiTestEmployees.GetEmployeeDetails(new Guid(CoiTestEmployees.NormalEmployee)));
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerActionManagerViewEdView);

        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "employee@email",
            FormOwnerName = "Test Employee",
            NextApprover = null
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Recall),
            FormDetails = new FormDetailsRequest()
        };
        var expectedManagerBody = string.Format(EXPECTED_RECALL_BODY, "Manager Employee");
        var expectedEdBody = string.Format(EXPECTED_RECALL_BODY, "Exec Employee");

        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        Assert.Collection(result, message =>
        {
            Assert.Equal("employee@email", message.To[0].Address);
            Assert.Empty(message.CC);
            Assert.Equal("Close personal relationship - Conflict of Interest declaration recalled", message.Subject);
            Assert.Equal(EXPECTED_OWNER_RECALL_BODY, message.Body);
        }, message =>
        {
            Assert.Equal("ToManager@email", message.To[0].Address);
            Assert.Empty(message.CC);
            Assert.Equal("Close personal relationship - Conflict of Interest declaration recalled", message.Subject);
            Assert.Equal(expectedManagerBody, message.Body);
        }, message =>
        {
            Assert.Equal("normal_ed@email", message.To[0].Address);
            Assert.Empty(message.CC);
            Assert.Equal("Close personal relationship - Conflict of Interest declaration recalled", message.Subject);
            Assert.Equal(expectedEdBody, message.Body);
        });
    }

    [Fact]
    public async Task Recall_should_only_email_ed_p_and_c_when_no_other_positions()
    {
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerViewEdPAndCAction);

        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "employee@email",
            FormOwnerName = "Test Employee",
            NextApprover = null
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Recall),
            FormDetails = new FormDetailsRequest()
        };
        var expectedManagerBody = string.Format(EXPECTED_RECALL_BODY, "ED P&C");

        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        Assert.Collection(result, message =>
        {
            Assert.Equal("employee@email", message.To[0].Address);
            Assert.Empty(message.CC);
            Assert.Equal("Close personal relationship - Conflict of Interest declaration recalled", message.Subject);
            Assert.Equal(EXPECTED_OWNER_RECALL_BODY, message.Body);
        }, message =>
        {
            Assert.Equal("edP&C@email.com", message.To[0].Address);
            Assert.Empty(message.CC);
            Assert.Equal("Close personal relationship - Conflict of Interest declaration recalled", message.Subject);
            Assert.Equal(expectedManagerBody, message.Body);
        });
    }

    [Fact]
    public async Task Should_send_cancelled_email_to_employee()
    {
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.ManagerlessOwnerView);
        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "employee@email",
            FormOwnerName = "Test Employee",
            NextApprover = null,
            FormStatusId = (int)FormStatus.Unsubmitted
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Submitted),
            FormDetails = new FormDetailsRequest()
        };
        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        var message = Assert.Single(result);
        Assert.Equal("managerless_employee@email", message.To[0].Address);
        Assert.Equal("Close personal relationship - Conflict of Interest declaration has cancelled", message.Subject);
        Assert.Equal(EXPECTED_CANCELLED_BODY, message.Body);
    }

    [Fact]
    public async Task Should_send_cancelled_email_to_manager()
    {
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerViewManagerEdAction);
        _employeeService.Setup(x => x.GetEmployeeByAzureIdAsync(It.IsAny<Guid>())).ReturnsAsync(() =>
            CoiTestEmployees.GetEmployeeDetails(Guid.Parse(CoiTestEmployees.NormalEmployee)));
        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "employee@email",
            FormOwnerName = "Test Employee",
            NextApprover = null,
            FormStatusId = (int)FormStatus.Unsubmitted
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Submitted),
            FormDetails = new FormDetailsRequest()
        };
        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        Assert.Collection(result, message =>
        {
            Assert.Equal("employee@email", message.To[0].Address);
            Assert.Equal("Close personal relationship - Conflict of Interest declaration has cancelled", message.Subject);
            Assert.Equal(EXPECTED_CANCELLED_BODY, message.Body);
        }, message =>
        {
            Assert.Equal("ToManager@email", message.To[0].Address);
            Assert.Equal("Close personal relationship - Conflict of Interest declaration has cancelled", message.Subject);
            Assert.Equal(EXPECTED_CANCELLED_TO_MANAGER_TEMPLATE, message.Body);
        });
    }

    [Fact]
    public async Task Escalate_returns_Escalate_message()
    {

        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerViewManagerEdAction);
        _employeeService.Setup(x => x.GetEmployeeByAzureIdAsync(It.IsAny<Guid>())).ReturnsAsync(() =>
            CoiTestEmployees.GetEmployeeDetails(Guid.Parse(CoiTestEmployees.ManagerlessEmployee)));

        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "employee@email",
            FormOwnerName = "Test Employee",
            NextApprover = "ED@email",
            FormStatusId = (int)FormStatus.Submitted
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Escalated),
            FormDetails = new FormDetailsRequest()
        };
        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        var message = Assert.Single(result);
        Assert.Equal("normal_ed@email", message.To[0].Address);
        Assert.Equal("Close personal relationship - Conflict of interest declaration - for approval", message.Subject);
        Assert.Equal(EXPECTED_ESCALATION, message.Body);
    }

    [Fact]
    public async Task Escalate_should_email_manager()
    {
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerViewManagerEdAction);
        _employeeService.Setup(x => x.GetEmployeeByAzureIdAsync(It.IsAny<Guid>())).ReturnsAsync(() =>
            CoiTestEmployees.GetEmployeeDetails(Guid.Parse(CoiTestEmployees.NormalEmployee)));

        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "employee@email",
            FormOwnerName = "Test Employee",
            NextApprover = "ED@email",
            FormStatusId = (int)FormStatus.Submitted
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Escalated),
            FormDetails = new FormDetailsRequest()
        };
        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        Assert.Collection(result, message =>
        {
            Assert.Equal("normal_ed@email", message.To[0].Address);
            Assert.Equal("Close personal relationship - Conflict of interest declaration - for approval", message.Subject);
            Assert.Equal(EXPECTED_ESCALATION, message.Body);
        }, message =>
        {
            Assert.Equal("ToManager@email", message.To[0].Address);
            Assert.Equal("Close personal relationship - Conflict of interest declaration - for approval", message.Subject);
            Assert.Equal(EXPECTED_MANAGER_ESCALATION, message.Body);
        });
    }

    [Fact]
    public async Task Should_send_3_day_manager_reminder()
    {
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerViewManagerAction);
        _employeeService.Setup(x => x.GetEmployeeByPositionNumberAsync(It.Is<int>(x => x == 2)))
            .ReturnsAsync(new List<IUserInfo> { CoiTestEmployees.GetEmployeeDetails(new Guid(CoiTestEmployees.NormalManager)) });
        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "employee@email",
            FormOwnerName = "Test Employee",
            NextApprover = "ToManager@email",
            Modified = DateTime.Today.AddDays(-3),
            FormStatusId = (int)FormStatus.Submitted
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Submitted),
            FormDetails = new FormDetailsRequest()
        };
        var expectedBody = string.Format(EXPECTED_SUBMITTED_MANAGER_BODY, "Manager Employee");

        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        var message = Assert.Single(result);
        Assert.Equal("normal_manager@email", message.To[0].Address);
        Assert.Empty(message.CC);
        Assert.Equal("Reminder: Close personal relationship - Conflict of Interest declaration for your review", message.Subject);
        Assert.Equal(expectedBody, message.Body);
    }

    [Fact]
    public async Task Should_send_3_day_ED_no_manager_reminder()
    {
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerViewEdAction);
        _employeeService.Setup(x => x.GetEmployeeByPositionNumberAsync(It.Is<int>(x => x == 3)))
            .ReturnsAsync(new List<IUserInfo> { CoiTestEmployees.GetEmployeeDetails(new Guid(CoiTestEmployees.NormalExecutiveDirector)) });
        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "managereless@email",
            FormOwnerName = "Test Employee",
            NextApprover = "ED@email",
            Modified = DateTime.Today.AddDays(-3),
            FormStatusId = (int)FormStatus.Submitted
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Submitted),
            FormDetails = new FormDetailsRequest()
        };

        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        var message = Assert.Single(result);
        Assert.Equal("normal_ed@email", message.To[0].Address);
        Assert.Empty(message.CC);
        Assert.Equal("Reminder: Close personal relationship - Conflict of interest declaration - for approval", message.Subject);
        Assert.Equal(EXPECTED_TIER3_NO_MANAGER_BODY, message.Body);
    }

    [Fact]
    public async Task Should_send_3_day_ED_endorse_reminder()
    {
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerViewManagerEdAction);
        _employeeService.Setup(x => x.GetEmployeeByPositionNumberAsync(It.Is<int>(x => x == 3)))
            .ReturnsAsync(new List<IUserInfo> { CoiTestEmployees.GetEmployeeDetails(new Guid(CoiTestEmployees.NormalExecutiveDirector)) });
        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "employee@email",
            FormOwnerName = "Test Employee",
            NextApprover = "ED@email",
            Modified = DateTime.Today.AddDays(-3),
            FormStatusId = (int)FormStatus.Approved
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Approved),
            FormDetails = new FormDetailsRequest()
        };

        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        var message = Assert.Single(result);
        Assert.Equal("normal_ed@email", message.To[0].Address);
        Assert.Empty(message.CC);
        Assert.Equal("Reminder: Close personal relationship - Conflict of interest declaration - for approval", message.Subject);
        Assert.Equal(EXPECTED_APPROVED_BODY, message.Body);
    }

    [Fact]
    public async Task Should_send_ED_PandC_reminder_without_manager_cc()
    {
        _requestingUserProvider.Setup(x => x.GetRequestingUser())
            .ReturnsAsync(CoiTestEmployees.GetEmployeeDetails(new Guid(CoiTestEmployees.EdPAndC)));
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.ManagerlessOwnerViewEdPAndCAction);
        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "employee@email",
            FormOwnerName = "Test Employee",
            NextApprover = "edP&C@email.com",
            Modified = DateTime.Today.AddDays(-3),
            FormStatusId = (int)FormStatus.Submitted
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Submitted),
            FormDetails = new FormDetailsRequest()
        };

        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        var message = Assert.Single(result);
        Assert.Equal("edP&C@email.com", message.To[0].Address);
        Assert.Empty(message.CC);
        Assert.Equal("Reminder: Close personal relationship - Conflict of interest declaration - for approval",
            message.Subject);
        Assert.Equal(EXPECTED_SUBMIT_TO_ED_P_AND_C_BODY, message.Body);
    }

    private const string EXPECTED_SUBMITTED_BODY =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Exec Employee,</div><br/>" +
        "<div>Thank you for your conflict of interest declaration in relation to Close personal relationship in the " +
        "workplace. Your declaration has been sent to your manager for review.</div><br/>" +
        "<div>Please <a href=https://base-url/close-personal-relationship/summary/33>click here</a> if you want to view, " +
        "recall or make changes to your declaration form.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services " +
        "on (08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    private const string EXPECTED_SUBMITTED_TO_ED_P_AND_C_BODY =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Test Employee,</div><br/>" +
        "<div>Thank you for your conflict of interest declaration in relation to Close personal relationship in the " +
        "workplace. Your declaration has been sent to the Executive Director of People & Culture for review.</div><br/>" +
        "<div>Please <a href=https://base-url/close-personal-relationship/summary/33>click here</a> if you want to " +
        "view, recall or make changes to your declaration form.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services " +
        "on (08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    const string EXPECTED_SUBMITTED_MANAGER_BODY =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Manager Employee,</div><br/>" +
        "<div>A conflict of interest declaration regarding Close personal relationship in the workplace has been submitted by Test " +
        "Employee.</div><br/>" +
        "<div>Please <a href=https://base-url/close-personal-relationship/summary/33>click here</a> to review and action the " +
        "declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services " +
        "on (08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    const string EXPECTED_TIER3_NO_MANAGER_BODY =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Exec Employee,</div><br/>" +
        "<div>A conflict of interest declaration has been submitted by Test Employee in relation to Close " +
        "personal relationship in the workplace. This form has escalated for your approval due to inaction from the line manager or due to " +
        "Test Employee not having an acting manager.</div><br/>" +
        "<div>Please <a href=https://base-url/close-personal-relationship/summary/33>click here</a> to review and action the " +
        "declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services " +
        "on (08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    private const string EXPECTED_SUBMIT_TO_ED_P_AND_C_BODY =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear ED P&C,</div><br/>" +
        "<div>A conflict of interest declaration has been submitted by Test Employee in relation to Close personal " +
        "relationship in the workplace. This has escalated directly for your approval at the request of Test Employee due " +
        "to the sensitive nature of the declaration.</div><br/>" +
        "<div>Please <a href=https://base-url/close-personal-relationship/summary/33>click here</a> to review and action the declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services on " +
        "(08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    private const string EXPECTED_APPROVED_BODY =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Exec Employee,</div><br/>" +
        "<div>A conflict of interest declaration has been submitted in relation to Close personal relationship in the " +
        "workplace and has been approved by the line manager for Test Employee.</div><br/>" +
        "<div>Please <a href=https://base-url/close-personal-relationship/summary/33>click here</a> to review and action the declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services " +
        "on (08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    private const string EXPECTED_REJECTION_BODY =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Test Employee,</div><br/>" +
        "<div>The conflict of interest declaration you submitted in relation to Close personal relationship in the " +
        "workplace has not been approved by your line manager.</div><br/>" +
        "<div>Please <a href=https://base-url/close-personal-relationship/33>click here</a> to view their comments and " +
        "amend your declaration if required.  You may also want to contact your line manager to discuss further.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services on " +
        "(08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    private const string EXPECTED_ENDORSED_BODY =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear ED P&C,</div><br/>" +
        "<div>A conflict of interest declaration has been submitted by Test Employee in relation to Close " +
        "personal relationship in the workplace.</div><br/>" +
        "<div>Please <a href=https://base-url/close-personal-relationship/summary/33>click here</a> to review and action the " +
        "declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services on " +
        "(08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    private const string EXPECTED_NOT_SUPPORTED_BODY =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Test Employee,</div><br/>" +
        "<div>The conflict of interest declaration you submitted in relation to Close personal relationship in the " +
        "workplace has not been approved.</div><br/>" +
        "<div>Please <a href=https://base-url/close-personal-relationship/33>click here</a> to view the " +
        "decision maker’s comments and make any necessary amendments to your initial declaration. You may also want " +
        "to contact your line manager to discuss further.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services on " +
        "(08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    private const string EXPECTED_NOT_SUPPORTED_TO_MANAGER =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Manager Employee,</div><br/>" +
        "<div>The conflict of interest declaration submitted by Test Employee in relation to Close personal relationship in the " +
        "workplace has not been approved.</div><br/>" +
        "<div>Please <a href=https://base-url/close-personal-relationship/summary/33>click here</a> to view the " +
        "decision maker’s comments.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services on " +
        "(08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    private const string EXPECTED_COMPLETED_BODY =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Test Employee,</div><br/>" +
        "<div>Your conflict of interest declaration regarding Close personal relationship in the workplace has been " +
        "approved by senior management.</div><br/>" +
        "<div>Please <a href=https://base-url/close-personal-relationship/summary/33>click here</a> to view the form " +
        "including any approved plan to manage the conflict.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services on " +
        "(08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>" +
        "<div>On behalf of Isabeau Korpel</div><br/>" +
        "<div>Executive Director</div><br/>" +
        "<div>People and Culture</div><br/>";

    private const string EXPECTED_COMPLETED_TO_MANAGER_BODY =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Manager Employee,</div><br/>" +
        "<div>The conflict of interest declaration regarding Close personal relationship in the workplace for Test Employee has been " +
        "approved by senior management.</div><br/>" +
        "<div>Please <a href=https://base-url/close-personal-relationship/summary/33>click here</a> to view the form " +
        "including any approved plan to manage the conflict.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services on " +
        "(08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>" +
        "<div>On behalf of Isabeau Korpel</div><br/>" +
        "<div>Executive Director</div><br/>" +
        "<div>People and Culture</div><br/>";

    private const string EXPECTED_RECALL_BODY =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear {0},</div><br/>" +
        "<div>The conflict of interest request has been recalled by Test Employee and does not require immediate " +
        "action.</div><br/>" +
        "<div>If you wish to review the form, please " +
        "<a href=https://base-url/close-personal-relationship/summary/33>click here</a> to review the declaration.</div><br/>" +
        "<div>Should you require any additional information in relation to this matter, please contact the Employee " +
        "Services on (08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a> to discuss " +
        "further.</div><br/>" +
        "<div>Thank you</div><br/>";

    private const string EXPECTED_OWNER_RECALL_BODY =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Test Employee,</div><br/>" +
        "<div>Your conflict of interest declaration was successfully recalled.</div><br/>" +
        "<div>If you wish to re-submit your eForm, please " +
        "<a href=https://base-url/close-personal-relationship/33>click here</a> to review the declaration.</div><br/>" +
        "<div>Should you require any additional information in relation to this matter, please contact the Employee " +
        "Services on (08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a> to discuss " +
        "further.</div><br/>" +
        "<div>Thank you</div><br/>";

    private const string EXPECTED_CANCELLED_BODY =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Test Employee,</div><br/>" +
        "<div>Your conflict of interest declaration submitted in relation to Close personal relationship " +
        "in the workplace has cancelled due to one or more of the reasons below.</div><br/>" +
        "<div><ul>" +
        "<li>The Tier 3 position for your directorate is currently vacant.</li>" +
        "<li>Both your line manager and Tier 3 Executive Director positions are currently vacant.</li>" +
        "<li>Your line manager did not action the form in the required timeframe and the form escalated to the Tier 3 " +
        "Executive Director, whose position is currently vacant.</li>" +
        "</ul></div><br/>" +
        "<div>Please " +
        "<a href=https://base-url/close-personal-relationship/33>click here</a> to review the declaration and " +
        "re-submit your declaration.</div><br/>" +
        "<div>If you have any further questions please contact the Employee " +
        "Services on (08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a>.</div><br/>";

    private const string EXPECTED_CANCELLED_TO_MANAGER_TEMPLATE =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Manager Employee,</div><br/>" +
        "<div>The conflict of interest declaration submitted by Test Employee in relation to Close personal relationship " +
        "in the workplace has cancelled due to one or more of the reasons below.</div><br/>" +
        "<div><ul>" +
        "<li>The Tier 3 position for your directorate is currently vacant.</li>" +
        "<li>You did not action the form in the required timeframe and the form escalated to the Tier 3 " +
        "Executive Director, whose position is currently vacant.</li>" +
        "</ul></div><br/>" +
        "<div>Please " +
        "<a href=https://base-url/close-personal-relationship/summary/33>click here</a> to view the declaration.</div><br/>" +
        "<div>If you have any further questions please contact the Employee " +
        "Services on (08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a>.</div><br/>";

    const string EXPECTED_ESCALATION =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Exec Employee,</div><br/>" +
        "<div>A conflict of interest declaration has been submitted by Test Employee in relation to Close personal " +
        "relationship in the workplace. This form has escalated for your approval due to inaction from the line manager or due to " +
        "Test Employee not having an acting manager.</div><br/>" +
        "<div>Please <a href=https://base-url/close-personal-relationship/summary/33>click here</a> to review and action the " +
        "declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services on " +
        "(08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    const string EXPECTED_MANAGER_ESCALATION =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Manager Employee,</div><br/>" +
        "<div>A conflict of interest declaration has been submitted by Test Employee in relation to Close personal " +
        "relationship in the workplace. This form has escalated to Exec Employee for approval.</div><br/>" +
        "<div>Please <a href=https://base-url/close-personal-relationship/summary/33>click here</a> to view the " +
        "declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services on " +
        "(08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    public const string APPROVED_AND_COMPLETED_TEMPLATE =
        "<div>Dear {0},</div><br/>" +
        "<div>Your conflict of interest declaration regarding Close personal relationship in the workplace has been " +
        "approved by senior management.</div><br/>" +
        "<div>Please {1} to view the form including any approved plan to manage the conflict.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services on " +
        "(08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>" +
        "<div>On behalf of Isabeau Korpel</div><br/>" +
        "<div>Executive Director</div><br/>" +
        "<div>People and Culture</div>";
}