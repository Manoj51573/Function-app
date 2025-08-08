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

public class GbcMessageBuilderTest
{
    private readonly Mock<IEmployeeService> _employeeService;
    private readonly Mock<ILogger<GbcMessageBuilder>> _logger;
    private readonly GbcMessageBuilder _builder;
    private readonly Mock<IRequestingUserProvider> _requestingUserProvider;
    private readonly Mock<IPermissionManager> _permissionManager;

    public GbcMessageBuilderTest()
    {
        _employeeService = new Mock<IEmployeeService>();
        _logger = new Mock<ILogger<GbcMessageBuilder>>();
        var inMemorySettings = new Dictionary<string, string>
        {
            {"BaseEformsUrl", "https://base-url"},
            {"FromEmail", "forms-dev@transport.wa.gov.au"}
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        new Mock<IRepository<FormPermission>>();
        _requestingUserProvider = new Mock<IRequestingUserProvider>();
        _permissionManager = new Mock<IPermissionManager>();
        _builder = new GbcMessageBuilder(_logger.Object, configuration, _requestingUserProvider.Object, _permissionManager.Object, _employeeService.Object);

        _employeeService.Setup(x => x.GetEmployeeByAzureIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => CoiTestEmployees.GetEmployeeDetails(id));
        _employeeService.Setup(x =>
            x.GetEmployeeByPositionNumberAsync(It.Is<int>(x =>
                x == ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID))).ReturnsAsync(new List<IUserInfo>
                {
                    new EmployeeDetailsDto
                    {
                        EmployeeEmail = "edp&c@email", EmployeeFirstName = "ED", EmployeePreferredName = "ED", EmployeeSurname = "P&C", Directorate = "People and Culture"
                    }
                });
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>())).ReturnsAsync(
            CoiTestPermissions.OwnerViewManagerAction);
    }

    [Fact]
    public async Task Should_return_sumbitted_emails()
    {
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerViewManagerAction);
        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "employee@email",
            FormOwnerName = "Test Employee",
            NextApprover = "ToManager@email",
            FormStatusId = (int)FormStatus.Submitted
        };
        var request = new FormInfoUpdate { FormAction = Enum.GetName(FormStatus.Submitted) };
        _builder.Initialize(dbModel, request);
        var expectedBody = string.Format(EXPECTED_SUBMITTED_MANAGER_BODY, "Manager Employee");

        var result = await _builder.GetMessagesAsync();

        Assert.Collection(result,
            message =>
            {
                Assert.Equal("ToManager@email", message.To[0].Address);
                Assert.Equal(expectedBody, message.Body);
                Assert.Equal("Government board/committee - Conflict of Interest declaration for your review",
                    message.Subject);
            });
        _logger.VerifyLogging("", LogLevel.Error, Times.Never(), true);
    }

    [Fact]
    public async Task Should_return_ED_message()
    {
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerViewEdAction);
        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "employee@email",
            FormOwnerName = "Test Employee",
            NextApprover = "ED@email",
            FormStatusId = (int)FormStatus.Submitted
        };
        var request = new FormInfoUpdate { FormAction = Enum.GetName(FormStatus.Submitted) };
        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        Assert.Collection(result,
            message =>
            {
                Assert.Equal("normal_ed@email", message.To[0].Address);
                Assert.Equal(EXPECTED_TIER3_NO_MANAGER_BODY, message.Body);
                Assert.Equal("Government board/committee - Conflict of interest declaration - for approval",
                    message.Subject);
            });
        _logger.VerifyLogging("", LogLevel.Error, Times.Never(), true);
    }

    [Fact]
    public async Task Should_return_submitted_email_for_owner_and_manager()
    {
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerViewManagerEdAction);
        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "employee@email",
            FormOwnerName = "Test Employee",
            NextApprover = "ED@email",
            FormStatusId = (int)FormStatus.Submitted
        };
        var request = new FormInfoUpdate { FormAction = Enum.GetName(FormStatus.Submitted) };
        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        Assert.Collection(result,
            message =>
            {
                Assert.Equal("normal_ed@email", message.To[0].Address);
                Assert.Equal(EXPECTED_TIER3_NO_MANAGER_BODY, message.Body);
                Assert.Equal("Government board/committee - Conflict of interest declaration - for approval",
                    message.Subject);
            });
        _logger.VerifyLogging("", LogLevel.Error, Times.Never(), true);
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
        Assert.Equal("Reminder: Government board/committee - Conflict of Interest declaration for your review", message.Subject);
        Assert.Equal(expectedBody, message.Body);
    }

    [Fact]
    public async Task Should_send_3_day_ED_no_manager_reminder()
    {
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerViewEdAction);
        _employeeService.Setup(x => x.GetEmployeeByPositionNumberAsync(It.Is<int>(x => x == 3))).ReturnsAsync(
            new List<IUserInfo>
                { CoiTestEmployees.GetEmployeeDetails(new Guid(CoiTestEmployees.NormalExecutiveDirector)) });
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
        Assert.Equal("Reminder: Government board/committee - Conflict of interest declaration - for approval", message.Subject);
        Assert.Equal(EXPECTED_TIER3_NO_MANAGER_BODY, message.Body);
    }

    [Fact]
    public async Task Should_send_3_day_ED_endorse_reminder()
    {
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerViewEdAction);
        _employeeService.Setup(x => x.GetEmployeeByPositionNumberAsync(It.Is<int>(x => x == 3))).ReturnsAsync(
            new List<IUserInfo>
                { CoiTestEmployees.GetEmployeeDetails(new Guid(CoiTestEmployees.NormalExecutiveDirector)) });
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
        Assert.Equal("Reminder: Government board/committee - Conflict of interest declaration - for approval", message.Subject);
        Assert.Equal(EXPECTED_EXECUTIVE_DIRECTOR_APPROVAL, message.Body);
    }

    [Fact]
    public async Task Recall_returns_expected_email()
    {
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerViewManagerAction);
        _employeeService.Setup(x => x.GetEmployeeByPositionNumberAsync(It.Is<int>(x => x == 2))).ReturnsAsync(
            new List<IUserInfo> { CoiTestEmployees.GetEmployeeDetails(new Guid(CoiTestEmployees.NormalManager)) });

        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "requestor@email",
            FormOwnerName = "Test Employee",
            NextApprover = null
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Recall),
            FormDetails = new FormDetailsRequest()
        };

        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        Assert.Collection(result, message =>
        {
            Assert.Equal("employee@email", message.To[0].Address);
            Assert.Empty(message.CC);
            Assert.Equal("Government board/committee - Conflict of Interest declaration recalled", message.Subject);
            Assert.Equal(EXPECTED_OWNER_RECALL_BODY, message.Body);
        }, message =>
        {
            Assert.Equal("ToManager@email", message.To[0].Address);
            Assert.Empty(message.CC);
            Assert.Equal("Government board/committee - Conflict of Interest declaration recalled", message.Subject);
            Assert.Equal(EXPECTED_RECALL_BODY, message.Body);
        });
    }

    [Fact]
    public async Task Reject_when_by_manager_sends_rejection_to_owner()
    {
        _requestingUserProvider.Setup(x => x.GetRequestingUser()).ReturnsAsync(new EmployeeDetailsDto
        {
            ActiveDirectoryId = Guid.Parse(CoiTestEmployees.NormalManager),
            EmployeePositionId = 2
        });
        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "employee@email",
            FormOwnerName = "Test Employee",
            NextApprover = "Next",
            FormStatusId = (int)FormStatus.Submitted,
            Response = JsonConvert.SerializeObject(new BoardCommitteePAndC()),
            ModifiedBy = "ToManager@email"
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Rejected),
            FormDetails = new FormDetailsRequest(),
            UserId = "ToManager@email"
        };
        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        var mail = Assert.Single(result);
        Assert.Equal("employee@email", mail.To[0].Address);
        Assert.Equal("Government board/committee - Conflict of interest declaration not approved by line manager", mail.Subject);
        Assert.Equal(EXPECTED_LINE_MANAGER_REJECTION, mail.Body);
        Assert.Empty(mail.CC);
    }

    [Theory]
    [MemberData(nameof(RejectionParams))]
    public async Task Reject_sends_the_correct_rejection(BoardCommitteePAndC dbForm, string requestor, string expectedCc, string formOwnerEmail,
        string expectedBody, string expectedSubject)
    {
        _requestingUserProvider.Setup(x => x.GetRequestingUser()).ReturnsAsync(() =>
        {
            switch (requestor)
            {
                case CoiTestEmployees.NormalExecutiveDirector:
                    return new EmployeeDetailsDto
                    { EmployeePositionId = 3, ActiveDirectoryId = Guid.Parse(CoiTestEmployees.NormalExecutiveDirector) };
                case CoiTestEmployees.EdPAndC:
                    return new EmployeeDetailsDto
                    {
                        EmployeePositionId = ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID,
                        ActiveDirectoryId = Guid.Parse(CoiTestEmployees.EdPAndC)
                    };
                default:
                    return new EmployeeDetailsDto();
            }
        });
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerActionManagerViewEdView);

        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = formOwnerEmail,
            FormOwnerName = "Test Employee",
            NextApprover = "Next",
            FormStatusId = (int)FormStatus.Submitted,
            Response = JsonConvert.SerializeObject(dbForm),
            ModifiedBy = requestor
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Rejected),
            FormDetails = new FormDetailsRequest(),
            UserId = requestor
        };
        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        Assert.Collection(result, message =>
        {
            Assert.Equal("employee@email", message.To[0].Address);
            Assert.Empty(message.CC);
            Assert.Equal(expectedSubject, message.Subject);
            Assert.Equal(EXPECTED_TIER_3_REJECTION, message.Body);
        }, message =>
        {
            Assert.Equal("ToManager@email", message.To[0].Address);
            Assert.Empty(message.CC);
            Assert.Equal(expectedSubject, message.Subject);
            Assert.Equal(expectedBody, message.Body);
        });
    }

    public static IEnumerable<object[]> RejectionParams()
    {
        return new[]
        {
            new object[]
            {
                new BoardCommitteePAndC
                {
                    ManagerForm = new CoiManagerForm { ConflictType = ConflictOfInterest.ConflictType.Perceived }
                }, CoiTestEmployees.NormalExecutiveDirector,
                "ToManager@email", "employee@email",EXPECTED_TIER_3_REJECTION_FOR_MANAGER,
                "Government board/committee - Conflict of interest declaration not approved by senior management"
            },
            new object[]
            {
                new BoardCommitteePAndC
                {
                    ManagerForm = new CoiManagerForm { ConflictType = ConflictOfInterest.ConflictType.Perceived }
                }, CoiTestEmployees.EdPAndC,
                "ToManager@email", "employee@email", EXPECTED_TIER_3_REJECTION_FOR_MANAGER,
                "Government board/committee - Conflict of interest declaration not approved by senior management"
            }
        };
    }

    [Fact]
    public async Task Approved_returns_approved_message()
    {
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerViewEdAction);
        _employeeService.Setup(x => x.GetEmployeeByPositionNumberAsync(It.Is<int>(x => x == 3))).ReturnsAsync(
            new List<IUserInfo>
                { CoiTestEmployees.GetEmployeeDetails(new Guid(CoiTestEmployees.NormalExecutiveDirector)) });
        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "employee@email",
            FormOwnerName = "Test Employee",
            NextApprover = "ED@email",
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
        Assert.Equal("Government board/committee - Conflict of interest declaration - for approval", message.Subject);
        Assert.Equal(EXPECTED_EXECUTIVE_DIRECTOR_APPROVAL, message.Body);
    }

    [Theory]
    [InlineData(true, "edp&c@email", "ED P&C")]
    [InlineData(false, "normal_ed@email", "Exec Employee")]
    public async Task Endorsed_returns_endorsed_message(bool isEdPAndC, string nextApprover, string expectedTo)
    {
        if (isEdPAndC)
        {
            _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
                .ReturnsAsync(CoiTestPermissions.OwnerViewEdPAndCAction);
        }
        else
        {
            _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
                .ReturnsAsync(CoiTestPermissions.OwnerViewEdAction);
            _employeeService.Setup(x => x.GetEmployeeByPositionNumberAsync(It.Is<int>(x => x == 3))).ReturnsAsync(
                new List<IUserInfo>
                    { CoiTestEmployees.GetEmployeeDetails(new Guid(CoiTestEmployees.NormalExecutiveDirector)) });
        }
        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = "employee@email",
            FormOwnerName = "Test Employee",
            FormStatusId = (int)FormStatus.Endorsed
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Endorsed),
            FormDetails = new FormDetailsRequest()
        };
        _builder.Initialize(dbModel, request);
        var expectedBody = string.Format(EXPECTED_ENDORSED, expectedTo);

        var result = await _builder.GetMessagesAsync();

        var message = Assert.Single(result);
        Assert.Equal(nextApprover, message.To[0].Address);
        Assert.Equal("Government board/committee - Conflict of interest declaration - for approval", message.Subject);
        Assert.Equal(expectedBody, message.Body);
    }

    [Theory]
    [InlineData(FormStatus.Endorsed)]
    [InlineData(FormStatus.Completed)]
    public async Task Completed_returns_expected_message_to_owner_and_manager_when_final_approver_actions(
        FormStatus formAction)
    {
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerManagerViewEdPAndCAction);
        _requestingUserProvider.Setup(x => x.GetRequestingUser())
            .ReturnsAsync(CoiTestEmployees.GetEmployeeDetails(new Guid(CoiTestEmployees.EdPAndC)));
        _employeeService.Setup(x => x.GetEmployeeByPositionNumberAsync(It.Is<int>(x => x == 2))).ReturnsAsync(
            new List<IUserInfo> { CoiTestEmployees.GetEmployeeDetails(new Guid(CoiTestEmployees.NormalManager)) });
        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerName = "Test Employee",
            NextApprover = "edp&c@email",
            FormStatusId = (int)FormStatus.Completed
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(formAction),
            FormDetails = new FormDetailsRequest()
        };
        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();
        result = result.DistinctBy(x => x.To).ToList();

        Assert.Collection(result, message =>
        {
            Assert.Equal("employee@email", message.To[0].Address);
            Assert.Empty(message.CC);
            Assert.Equal("Government board/committee - Conflict of interest declaration outcome", message.Subject);
        }, message =>
        {
            Assert.Equal("ToManager@email", message.To[0].Address);
            Assert.Empty(message.CC);
            Assert.Equal("Government board/committee - Conflict of interest declaration outcome", message.Subject);
        });
    }


    [Theory]
    //[InlineData("managereless@email", FormStatus.Completed)]
    [InlineData("managereless@email", FormStatus.Approved)]
    public async Task Completed_returns_completed_message_to_user_only_when_no_manager(string formOwnerEmail, FormStatus formStatus)
    {
        _requestingUserProvider.Setup(x => x.GetRequestingUser())
            .ReturnsAsync(CoiTestEmployees.GetEmployeeDetails(new Guid(CoiTestEmployees.EdPAndC)));
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.ManagerlessOwnerViewEdPAndCAction);
        var dbModel = new FormInfo
        {
            FormInfoId = 33,
            FormOwnerEmail = formOwnerEmail,
            FormOwnerName = "Test Employee",
            NextApprover = "edp&c@email",
            FormStatusId = (int)FormStatus.Completed
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(formStatus),
            FormDetails = new FormDetailsRequest()
        };
        _builder.Initialize(dbModel, request);

        var result = await _builder.GetMessagesAsync();

        var message = result.First();
        Assert.Equal("managerless_employee@email", message.To[0].Address);
        Assert.Empty(message.CC);
        Assert.Equal("Government board/committee - Conflict of interest declaration outcome", message.Subject);
        Assert.Equal(EXPECTED_COMPLETED, message.Body);
        //Assert.Collection(result, message =>
        //{
        //    Assert.Equal("managerless_employee@email", message.To[0].Address);
        //    Assert.Empty(message.CC);
        //    Assert.Equal("Government board/committee - Conflict of interest declaration outcome", message.Subject);
        //    Assert.Equal(EXPECTED_COMPLETED, message.Body);
        //});

    }

    [Fact]
    public async Task Escalate_returns_message_to_ED_and_manager()
    {
        _employeeService.Setup(x => x.GetEmployeeByAzureIdAsync(It.IsAny<Guid>())).ReturnsAsync(() =>
            CoiTestEmployees.GetEmployeeDetails(Guid.Parse(CoiTestEmployees.NormalEmployee)));
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(CoiTestPermissions.OwnerAndManagerViewEdAction);

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
            Assert.Empty(message.CC);
            Assert.Equal("Government board/committee - Conflict of interest declaration - for approval", message.Subject);
            Assert.Equal(EXPECTED_ESCALATION, message.Body);
        }, message =>
        {
            Assert.Equal("ToManager@email", message.To[0].Address);
            Assert.Empty(message.CC);
            Assert.Equal("Government board/committee - Conflict of interest declaration - for approval", message.Subject);
            Assert.Equal(EXPECTED_ESCALATION_TO_MANAGER, message.Body);
        });
    }

    [Theory]
    [InlineData(CoiTestEmployees.ManagerlessEmployee)]
    [InlineData(CoiTestEmployees.NormalEmployee)]
    public async Task Escalate_returns_ed_message_when_no_managers(string ownerId)
    {
        _employeeService.Setup(x => x.GetEmployeeByAzureIdAsync(It.IsAny<Guid>())).ReturnsAsync(() =>
            ownerId == CoiTestEmployees.NormalEmployee
                ? CoiTestEmployees.GetEmployeeDetails(Guid.Parse(CoiTestEmployees.ManagerlessEmployee))
                : CoiTestEmployees.GetEmployeeDetails(Guid.Parse(CoiTestEmployees.NormalEmployee)));
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(() => ownerId == CoiTestEmployees.NormalEmployee ? CoiTestPermissions.OwnerAndManagerViewEdAction : CoiTestPermissions.ManagerlessOwnerViewEdAction);
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
        Assert.Empty(message.CC);
        Assert.Equal("Government board/committee - Conflict of interest declaration - for approval", message.Subject);
        Assert.Equal(EXPECTED_ESCALATION, message.Body);
    }

    const string EXPECTED_SUBMITTED_BODY =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Test Employee,</div><br/>" +
        "<div>Thank you for your conflict of interest declaration in relation to Government board/committee. Your " +
        "declaration has been sent to your manager for review.</div><br/>" +
        "<div>Please <a href=https://base-url/coi-board-committee/summary/33>click here</a> if you want to view, " +
        "recall or make changes to your declaration form.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services " +
        "on (08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    const string EXPECTED_SUBMITTED_MANAGER_BODY =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear {0},</div><br/>" +
        "<div>A conflict of interest declaration regarding Government board/committee has been submitted by Test " +
        "Employee.</div><br/>" +
        "<div>Please <a href=https://base-url/coi-board-committee/summary/33>click here</a> to review and action the " +
        "declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services " +
        "on (08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    const string EXPECTED_TIER3_NO_MANAGER_BODY =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Exec Employee,</div><br/>" +
        "<div>A conflict of interest declaration has been submitted by Test Employee in relation to Government " +
        "board/committee. This form has escalated for your approval due to inaction from the line manager or due to " +
        "Test Employee not having an acting manager.</div><br/>" +
        "<div>Please <a href=https://base-url/coi-board-committee/summary/33>click here</a> to review and action the " +
        "declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services " +
        "on (08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    const string EXPECTED_RECALL_BODY =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Manager Employee,</div><br/>" +
        "<div>The conflict of interest request has been recalled by Test Employee and does not require " +
        "immediate action.</div><br/>" +
        "<div>If you wish to review the form, please " +
        "<a href=https://base-url/coi-board-committee/summary/33>click here</a> to review the declaration.</div><br/>" +
        "<div>Should you require any additional information in relation to this matter, please contact the Employee " +
        "Services on (08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a> to discuss " +
        "further.</div><br/>" +
        "<div>Thank you</div><br/>";

    const string EXPECTED_LINE_MANAGER_REJECTION =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Test Employee,</div><br/>" +
        "<div>The conflict of interest declaration you submitted in relation to Government board/committee has not " +
        "been approved by your line manager.</div><br/>" +
        "<div>Please <a href=https://base-url/coi-board-committee/33>click here</a> to view their comments and amend " +
        "your declaration if required.  You may also want to contact your line manager to discuss further.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services on " +
        "(08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    const string EXPECTED_EXECUTIVE_DIRECTOR_APPROVAL =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Exec Employee,</div><br/>" +
        "<div>A conflict of interest declaration has been submitted in relation to Government board/committee and " +
        "has been approved by the line manager for Test Employee.</div><br/>" +
        "<div>Please <a href=https://base-url/coi-board-committee/summary/33>click here</a> to review and action the " +
        "declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services on " +
        "(08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    const string EXPECTED_TIER_3_REJECTION =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Test Employee,</div><br/>" +
        "<div>The conflict of interest declaration you submitted in relation to Government board/committee has not " +
        "been approved.</div><br/>" +
        "<div>Please <a href=https://base-url/coi-board-committee/33>click here</a> to view the decision maker’s " +
        "comments and make any necessary amendments to your initial declaration. You may also want to contact your " +
        "line manager to discuss further.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services on " +
        "(08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    const string EXPECTED_TIER_3_REJECTION_FOR_MANAGER =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Manager Employee,</div><br/>" +
        "<div>The conflict of interest declaration submitted by Test Employee in relation to Government board/committee has not " +
        "been approved.</div><br/>" +
        "<div>Please <a href=https://base-url/coi-board-committee/summary/33>click here</a> to view the decision maker’s " +
        "comments.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services on " +
        "(08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    const string EXPECTED_ENDORSED =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear {0},</div><br/>" +
        "<div>A conflict of interest declaration has been submitted by Test Employee in relation to Government " +
        "board/committee.</div><br/>" +
        "<div>Please <a href=https://base-url/coi-board-committee/summary/33>click here</a> to review and action the " +
        "declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services on " +
        "(08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    const string EXPECTED_COMPLETED =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Test Employee,</div><br/>" +
        "<div>Your conflict of interest declaration regarding Government board/committee has been approved by senior " +
        "management.</div><br/>" +
        "<div>Please <a href=https://base-url/coi-board-committee/summary/33>click here</a> to view the form " +
        "including any approved plan to manage the conflict.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services on " +
        "(08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>" +
        "<div>On behalf of Ed Director P&C</div><br/>" +
        "<div>Executive Director</div><br/>" +
        "<div>People and Culture</div><br/>";

    const string EXPECTED_COMPLETED_TO_MANAGER =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Manager Employee,</div><br/>" +
        "<div>The conflict of interest declaration submitted by Test Employee regarding Government board/committee has been approved by senior " +
        "management.</div><br/>" +
        "<div>Please <a href=https://base-url/coi-board-committee/summary/33>click here</a> to view the form " +
        "including any approved plan to manage the conflict.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services on " +
        "(08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>" +
        "<div>On behalf of Ed Director P&C</div><br/>" +
        "<div>Executive Director</div><br/>" +
        "<div>People and Culture</div><br/>";

    const string EXPECTED_ESCALATION =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Exec Employee,</div><br/>" +
        "<div>A conflict of interest declaration has been submitted by Test Employee in relation to Government " +
        "board/committee. This form has escalated for your approval due to inaction from the line manager or due to " +
        "Test Employee not having an acting manager.</div><br/>" +
        "<div>Please <a href=https://base-url/coi-board-committee/summary/33>click here</a> to review and action the " +
        "declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services on " +
        "(08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    const string EXPECTED_ESCALATION_TO_MANAGER =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Manager Employee,</div><br/>" +
        "<div>A conflict of interest declaration has been submitted by Test Employee in relation to Government " +
        "board/committee. This form has escalated to Exec Employee for approval.</div><br/>" +
        "<div>Please <a href=https://base-url/coi-board-committee/summary/33>click here</a> to view the declaration.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or Employee Services on " +
        "(08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    private const string EXPECTED_OWNER_RECALL_BODY =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Test Employee,</div><br/>" +
        "<div>Your conflict of interest declaration was successfully recalled.</div><br/>" +
        "<div>If you wish to re-submit your eForm, please " +
        "<a href=https://base-url/coi-board-committee/33>click here</a> to review the declaration.</div><br/>" +
        "<div>Should you require any additional information in relation to this matter, please contact the Employee " +
        "Services on (08) 6551 6888 / " +
        "<a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a> to discuss " +
        "further.</div><br/>" +
        "<div>Thank you</div><br/>";
}