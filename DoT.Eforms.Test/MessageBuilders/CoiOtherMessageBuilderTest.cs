using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
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
using Microsoft.Graph;
using Microsoft.VisualBasic;
using Moq;
using Xunit;
using static System.Net.WebRequestMethods;

namespace DoT.Eforms.Test.MessageBuilders;

public class CoiOtherMessageBuilderTest
{
    private const string Skip = "Skipping until refactor finished";
    private readonly Mock<IEmployeeService> _employeeService;
    private readonly Mock<ILogger<CoIOtherMessageBuilder>> _logger;
    private readonly IConfigurationRoot _configuration;
    private readonly CoIOtherMessageBuilder _builder;
    private readonly Mock<IRepository<TaskInfo>> _taskInfoRepo;
    private Mock<IFormHistoryService> _historyService;
    private Mock<IRepository<FormPermission>> _formPermissionRepo;
    private Mock<IRequestingUserProvider> _requestingUserProvider;
    private Mock<IPermissionManager> _permissionManager;

    public CoiOtherMessageBuilderTest()
    {
        _employeeService = new Mock<IEmployeeService>();
        _taskInfoRepo = new Mock<IRepository<TaskInfo>>();
        _logger = new Mock<ILogger<CoIOtherMessageBuilder>>();
        var inMemorySettings = new Dictionary<string, string>
        {
            {"BaseEformsUrl", "https://base-url"},
            {"FromEmail", "forms-dev@transport.wa.gov.au"}
        };
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemorySettings).Build();

        _historyService = new Mock<IFormHistoryService>();
        _formPermissionRepo = new Mock<IRepository<FormPermission>>();
        _requestingUserProvider = new Mock<IRequestingUserProvider>();
        _permissionManager = new Mock<IPermissionManager>();
        _builder = new CoIOtherMessageBuilder(_configuration, _logger.Object, _requestingUserProvider.Object, _permissionManager.Object, _employeeService.Object);

        _employeeService.Setup(x => x.GetEmployeeByAzureIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid azureId) => CoiTestEmployees.GetEmployeeDetails(azureId));
        _employeeService.Setup(x =>
            x.GetEmployeeByPositionNumberAsync(It.Is<int>(x =>
                x == ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID))).ReturnsAsync(new List<IUserInfo>
                {
                    new EmployeeDetailsDto { EmployeeEmail = "edp&c@email", EmployeeFirstName = "ED", EmployeePreferredName = "ED", EmployeeSurname = "P&C" }
                });
    }

    [Fact]
    public async Task GetMessageInternalAsync_should_send_special_reminder()
    {
        // No requesting user
        // Assigned to manager
        var dbModel = new FormInfo
        {
            FormInfoId = 33, FormOwnerEmail = "employee@email", FormOwnerName = "Test Employee", NextApprover = null,
            FormStatusId = (int)FormStatus.Completed, CompletedDate = new DateTime(1985,2,21)
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Completed),
            FormDetails = new FormDetailsRequest()
        };
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(new List<FormPermission>
            {
                new((byte)PermissionFlag.View, userId: new Guid(CoiTestEmployees.NormalEmployee), isOwner: true),
                new((byte)PermissionFlag.View, positionId: 2)
            });
        _builder.Initialize(dbModel, request);
        
        var result = await _builder.GetMessagesAsync();
        
        var message = Assert.Single(result);
        Assert.Equal(ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL, message.To[0].Address);
        Assert.Equal("Conflict of Interest (Other) declaration reminder", message.Subject);
        Assert.Equal(EXPECTED_SPECIAL_REMINDER, message.Body);
    }
    
    [Fact]
    public async Task GetMessageInternalAsync_should_send_approval_email_to_ODG_team_when_required()
    {
        _requestingUserProvider.Setup(x => x.GetRequestingUser())
            .ReturnsAsync(CoiTestEmployees.GetEmployeeDetails(new Guid(CoiTestEmployees.NormalEmployee)));
        var dbModel = new FormInfo
        {
            FormInfoId = 33, FormOwnerEmail = "employee@email", FormOwnerName = "Test Employee",
            NextApprover = ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL, FormStatusId = (int)FormStatus.Endorsed
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.Endorsed),
            FormDetails = new FormDetailsRequest()
        };
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>())).ReturnsAsync(
            new List<FormPermission>
            {
                new ((byte)PermissionFlag.View, userId: new Guid(CoiTestEmployees.NormalEmployee), isOwner: true),
                new ((byte)PermissionFlag.UserActionable, groupId: ConflictOfInterest.ODG_AUDIT_GROUP_ID)
            });
        _builder.Initialize(dbModel, request);
        
        var result = await _builder.GetMessagesAsync();
        
        var message = Assert.Single(result);
        Assert.Equal(ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL, message.To[0].Address);
        Assert.Equal("Other - Conflict of interest declaration - for QA review", message.Subject);
        Assert.Equal(EXPECTED_ODG_APPROVAL_EMAIL, message.Body);
    }

    [Fact]
    public async Task GetMessageInternalAsync_should_send_independent_reviewer_email()
    {
        _requestingUserProvider.Setup(x => x.GetRequestingUser())
            .ReturnsAsync(CoiTestEmployees.GetEmployeeDetails(new Guid(CoiTestEmployees.NormalManager)));
        var dbModel = new FormInfo
        {
            FormInfoId = 33, FormOwnerEmail = "employee@email", FormOwnerName = "Test Employee",
            NextApprover = "random.reviewer@email", FormStatusId = (int)FormStatus.IndependentReview
        };
        var request = new FormInfoUpdate
        {
            FormAction = Enum.GetName(FormStatus.IndependentReview),
            FormDetails = new FormDetailsRequest()
        };
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(new List<FormPermission>
            {
                new((byte)PermissionFlag.View, userId: new Guid(CoiTestEmployees.NormalEmployee), isOwner: true),
                new((byte)PermissionFlag.UserActionable, userId: new Guid(CoiTestEmployees.EdPAndC))
            });
        _builder.Initialize(dbModel, request);
        
        var result = await _builder.GetMessagesAsync();
        
        var message = Assert.Single(result);
        Assert.Equal("random.reviewer@email", message.To[0].Address);
        Assert.Equal("Other - Conflict of interest declaration for independent review", message.Subject);
        Assert.Equal(EXPECTED_INDEPENDENT_REVIEW_EMAIL, message.Body);
    }



    private const string EXPECTED_SPECIAL_REMINDER =
    "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>"+
    "<div>Dear ODG Governance & Audit,</div><br/>"+
    "<div>This notification is a reminder of the Conflict of Interest other circumstances declaration submitted by Test Employee and approved by senior management on 21/02/1985.</div><br/>"+
    "<div>Please <a href=https://base-url/coi-other/summary/33>click here</a> to review the declaration and perform the necessary actions.</div><br/>"+
    "<div>Further information about conflicts of interest is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or ODG Governance and Audit via <a href=\"mailto:employeeservices@transport.wa.gov.au\">employeeservices@transport.wa.gov.au</a></div><br/>";

    private const string EXPECTED_ODG_APPROVAL_EMAIL =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear ODG Governance & Audit,</div><br/>" +
        "<div>A conflict of interest declaration has been received from Test Employee regarding other circumstances.</div><br/>" +
        "<div> The declaration has been reviewed and was approved by Test Employee.</div><br/>" +
        "<div>Please <a href=https://base-url/coi-other/summary/33>click here</a> to review the form for quality assurance purposes.</div><br/>";

    private const string EXPECTED_INDEPENDENT_REVIEW_EMAIL =
    "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>"+
    "<div>Dear Ed Director P&C,</div><br/><div>You have been selected to undertake an independent review of a decision regarding a conflict of interest other circumstances declaration by Test Employee.</div><br/>" +
    "<div>Please <a href=https://base-url/coi-other/summary/33>click here</a> to complete the review.</div><br/>" +
    "<div>Further information about conflicts of interest is available from <a href=\"https://transporta/my-dot/34008.asp\">Transporta</a>, or ODG Governance and Audit via <a href=\"mailto:odggovernanceandaudit@transport.wa.gov.au\">odggovernanceandaudit@transport.wa.gov.au</a></div><br/>";

    private const string EXPECTED_INDEPENDENT_REVIEW_COMPLETE_MAIL =
        "<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div><br/>" +
        "<div>Dear Test Employee,</div><br/>" +
        "<div>Your Conflict of Interest (Other) declaration has been independently reviewed.</div><br/>" +
        "<div>Please <a href=https://base-url/coi-other/summary/33>click here</a> to view the form including any " +
        "approved plan to manage the conflict.</div><br/>" +
        "<div>Further information about conflicts of interest is available from Transporta, or ODG Governance and " +
        "Audit on (08) 6552 7083 / " +
        "<a href=\"mailto:odggovernanceandaudit@transport.wa.gov.au\">odggovernanceandaudit@transport.wa.gov.au</a>" +
        "</div><br/>";
}