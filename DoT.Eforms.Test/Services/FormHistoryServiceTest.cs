using DoT.Infrastructure;
using DoT.Infrastructure.DbModels;
using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure.Interfaces;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Services;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace DoT.Eforms.Test;

public class FormHistoryServiceTest
{
    private readonly Mock<ILogger<FormHistoryService>> _mockLog;
    private readonly Mock<IRepository<FormHistory>> _mockHistoryRepo;
    private readonly Mock<IRepository<FormInfo>> _mockFormInfoRepo;
    private readonly FormHistoryService _service;
    private readonly Mock<IRequestingUserProvider> _requestingUserProvider;
    private readonly Mock<IPermissionManager> _permissionManager;
    private readonly Mock<IFormInfoService> _formInfoService;

    public FormHistoryServiceTest()
    {
        _mockLog = new Mock<ILogger<FormHistoryService>>();
        _mockHistoryRepo = new Mock<IRepository<FormHistory>>();
        _mockFormInfoRepo = new Mock<IRepository<FormInfo>>();

        _requestingUserProvider = new Mock<IRequestingUserProvider>();
        _permissionManager = new Mock<IPermissionManager>();
        _formInfoService = new Mock<IFormInfoService>();

        _service = new FormHistoryService(_mockLog.Object, _mockHistoryRepo.Object, _requestingUserProvider.Object, _permissionManager.Object,
                            _formInfoService.Object);
    }

    [Theory]
    [MemberData(nameof(TestData))]
    public async Task AddFormHistoryAsync_MapsCorrectly(FormInfoUpdate request, FormInfo updatedForm, Guid? groupId, IUserInfo user, FormPermission nextApprover, string additionalInformation, FormHistory expectedHistory)
    {
        _requestingUserProvider.Setup(x => x.GetRequestingUser()).ReturnsAsync(() =>
            user);
        _permissionManager.Setup(x => x.GetPermissionsBySpecificationAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(new List<FormPermission> { nextApprover });

        await _service.AddFormHistoryAsync(request, updatedForm, groupId, additionalInformation);

        _mockHistoryRepo.Verify(
            x => x.AddAsync(It.Is<FormHistory>(history => AssertHistoryMatches(expectedHistory, history))), Times.Once());
    }

    [Fact]
    public async Task GetFormHistoryDetailsByID_ReturnsCorrectlyFormattedHistory()
    {
        const int formInfoId = 5;
        const int allFormId = 1;
        _formInfoService.Setup(x => x.GetFormInfoByIdAsync(It.IsAny<int>())).ReturnsAsync(new FormInfo
        {
            FormInfoId = formInfoId,
            AllFormsId = allFormId,
            FormStatusId = (int)FormStatus.Completed
        });
        _mockHistoryRepo.Setup(x => x.ListAsync(It.IsAny<ISpecification<FormHistory>>())).ReturnsAsync(new List<FormHistory>
        {
            new () { FormHistoryId = 1, FormInfoId = formInfoId, AllFormsId=allFormId, FormStatusId = (int)FormStatus.Unsubmitted, ActionBy = "owning_user@email", ActionType = "Saved", ActionByPosition = "Standard_Position"},
            new () { FormHistoryId = 2, FormInfoId = formInfoId, AllFormsId=allFormId, FormStatusId = (int)FormStatus.Submitted, ActionBy = "owning_user@email", ActionType = "Submitted", ActionByPosition = "Standard_Position" },
            new () { FormHistoryId = 3, FormInfoId = formInfoId, AllFormsId=allFormId, FormStatusId = (int)FormStatus.Recall, ActionBy = "owning_user@email", ActionType = "Recalled", ActionByPosition = "Standard_Position" },
            new () { FormHistoryId = 4, FormInfoId = formInfoId, AllFormsId=allFormId, FormStatusId = (int)FormStatus.Rejected, ActionBy = "owners_manager@email", ActionType = "Rejected", AditionalComments = "Rejection Reason", ActionByPosition = "Manager_Position" },
            new () { FormHistoryId = 5, FormInfoId = formInfoId, AllFormsId=allFormId, FormStatusId = (int)FormStatus.Approved, ActionBy = "owners_manager@email", ActionType = "Approved", ActionByPosition = "Manager_Position" },
            new () { FormHistoryId = 6, FormInfoId = formInfoId, AllFormsId=allFormId, FormStatusId = (int)FormStatus.Endorsed, ActionBy = "owners_ed@email", ActionType = "Endorsed", ActionByPosition = "ED_Position" },
            new () { FormHistoryId = 7, FormInfoId = formInfoId, AllFormsId=allFormId, FormStatusId = (int)FormStatus.Completed, ActionBy = "group_member@email", ActionType = "Completed", GroupName = "Group_Name", ActionByPosition = "Group_Position" }
        });

        var historyList = await _service.GetFormHistoryDetailsByID(formInfoId);

        Assert.Collection(historyList, dto =>
        {
            Assert.Equal("Saved by owning_user@email", dto.ActionType);
            Assert.Equal("owning_user@email", dto.ActionBy);
            Assert.Equal("Unsubmitted", dto.FormStatus);
            Assert.Null(dto.AdditionalComments);
            Assert.Equal("Standard_Position", dto.ActionByPosition);
        }, dto =>
        {
            Assert.Equal("Submitted by owning_user@email", dto.ActionType);
            Assert.Equal("owning_user@email", dto.ActionBy);
            Assert.Equal("Submitted", dto.FormStatus);
            Assert.Null(dto.AdditionalComments);
            Assert.Equal("Standard_Position", dto.ActionByPosition);
        }, dto =>
        {
            Assert.Equal("Recalled by owning_user@email", dto.ActionType);
            Assert.Equal("owning_user@email", dto.ActionBy);
            Assert.Equal("Recall", dto.FormStatus);
            Assert.Null(dto.AdditionalComments);
            Assert.Equal("Standard_Position", dto.ActionByPosition);
        }, dto =>
        {
            // here
            Assert.Equal("Rejected by owners_manager@email", dto.ActionType);
            Assert.Equal("owners_manager@email", dto.ActionBy);
            Assert.Equal("Rejected", dto.FormStatus);
            Assert.Equal("Rejection Reason", dto.AdditionalComments);
            Assert.Equal("Manager_Position", dto.ActionByPosition);
        }, dto =>
        {
            Assert.Equal("Approved by owners_manager@email", dto.ActionType);
            Assert.Equal("owners_manager@email", dto.ActionBy);
            Assert.Equal("Approved", dto.FormStatus);
            Assert.Null(dto.AdditionalComments);
            Assert.Equal("Manager_Position", dto.ActionByPosition);
        }, dto =>
        {
            Assert.Equal("Endorsed by owners_ed@email", dto.ActionType);
            Assert.Equal("owners_ed@email", dto.ActionBy);
            Assert.Equal("Endorsed", dto.FormStatus);
            Assert.Null(dto.AdditionalComments);
            Assert.Equal("ED_Position", dto.ActionByPosition);
        }, dto =>
        {
            Assert.Equal("Completed by group_member@email on behalf of Group_Name", dto.ActionType);
            Assert.Equal("group_member@email", dto.ActionBy);
            Assert.Equal("Completed", dto.FormStatus);
            Assert.Null(dto.AdditionalComments);
            Assert.Equal("Group_Position", dto.ActionByPosition);
        });
    }

    public static object[][] TestData => new[]
    {
        new object[]
        {
            new FormInfoUpdate { FormAction = "Unsubmitted" },
            new FormInfo { FormInfoId = 1, FormStatusId = 3, AllFormsId = 3, Modified = new DateTime(2022, 10, 1), FormOwnerEmail = "test@user.com"},
            null,
            new EmployeeDetailsDto {EmployeeEmail = "test@user.com", EmployeePositionTitle = "Standard_Position" },
            new FormPermission { PermissionFlag = (byte)PermissionFlag.UserActionable, IsOwner = true },
            null,
            new FormHistory
            {
                FormInfoId = 1, Created = new DateTime(2022, 10, 1), ActionBy = "test@user.com", ActionType = "Saved",
                AditionalComments = null, AllFormsId = 3, FormStatusId = 3, ActiveRecord = true, ActionByPosition = "Standard_Position"
            }
        },
        new object[]
        {
            new FormInfoUpdate { FormAction = "Delegate" },
            new FormInfo { FormInfoId = 1, FormStatusId = 3, AllFormsId = 3, Modified = new DateTime(2022, 10, 1), NextApprover = "not_this_value@user.com"},
            null,
            new EmployeeDetailsDto {EmployeeEmail = "test@user.com", EmployeePositionTitle = "Standard_Position"  },
            new FormPermission { PermissionFlag = (byte)PermissionFlag.UserActionable, User = new AdfUser { EmployeeEmail = "delegated@user.com" } },
            null,
            new FormHistory
            {
                FormInfoId = 1, Created = new DateTime(2022, 10, 1), ActionBy = "test@user.com", ActionType = "Delegated to delegated@user.com",
                AditionalComments = null, AllFormsId = 3, FormStatusId = 3, ActiveRecord = true, ActionByPosition = "Standard_Position"
            }
        },
        new object[]
        {
            new FormInfoUpdate { FormAction = "Submitted" },
            new FormInfo { FormInfoId = 1, FormStatusId = 1, AllFormsId = 3, Modified = new DateTime(2022, 10, 1)},
            null,
            new EmployeeDetailsDto {EmployeeEmail = "test@user.com", EmployeePositionTitle = "Standard_Position" },
            new FormPermission { PermissionFlag = (byte)PermissionFlag.UserActionable },
            null,
            new FormHistory
            {
                FormInfoId = 1, Created = new DateTime(2022, 10, 1), ActionBy = "test@user.com", ActionType = "Submitted",
                AditionalComments = null, AllFormsId = 3, FormStatusId = 1, ActiveRecord = true, ActionByPosition = "Standard_Position"
            }
        },
        new object[]
        {
            new FormInfoUpdate { FormAction = "Approved" },
            new FormInfo { FormInfoId = 1, FormStatusId = 3, AllFormsId = 3, Modified = new DateTime(2022, 10, 1) },
            null,
            new EmployeeDetailsDto {EmployeeEmail = "test@user.com", EmployeePositionTitle = "Standard_Position" },
            new FormPermission { PermissionFlag = (byte)PermissionFlag.UserActionable },
            null,
            new FormHistory
            {
                FormInfoId = 1, Created = new DateTime(2022, 10, 1), ActionBy = "test@user.com", ActionType = "Approved",
                AditionalComments = null, AllFormsId = 3, FormStatusId = 3, ActiveRecord = true, ActionByPosition = "Standard_Position"
            }
        },
        new object[]
        {
            new FormInfoUpdate { FormAction = FormStatus.Escalated.ToString() },
            new FormInfo { FormInfoId = 1, FormStatusId = 3, AllFormsId = 3, Modified = new DateTime(2022, 10, 1) },
            null,
            null,
            new FormPermission
            {
                PermissionFlag = (byte)PermissionFlag.UserActionable,
                Position = new AdfPosition
                {
                    AdfUserPositions = new List<AdfUser>
                    {
                        new () { EmployeeEmail = "next_approver@email" }
                    }
                }
            },
            null,
            new FormHistory
            {
                FormInfoId = 1, Created = new DateTime(2022, 10, 1), ActionBy = "System", ActionType = "Escalated to next_approver@email",
                AditionalComments = null, AllFormsId = 3, FormStatusId = 3, ActiveRecord = true, ActionByPosition = null
            }
        },
        new object[]
        {
            new FormInfoUpdate { FormAction = FormStatus.Escalated.ToString() },
            new FormInfo { FormInfoId = 1, FormStatusId = 3, AllFormsId = 3, Modified = new DateTime(2022, 10, 1) },
            null,
            null,
            new FormPermission
            {
                PermissionFlag = (byte)PermissionFlag.UserActionable,
                Position = new AdfPosition
                {
                    PositionTitle = "Position_Title",
                    AdfUserPositions = new List<AdfUser>
                    {
                        new (), new ()
                    }
                }
            },
            null,
            new FormHistory
            {
                FormInfoId = 1, Created = new DateTime(2022, 10, 1), ActionBy = "System", ActionType = "Escalated to Position_Title",
                AditionalComments = null, AllFormsId = 3, FormStatusId = 3, ActiveRecord = true, ActionByPosition = null
            }
        },
        new object[]
        {
            new FormInfoUpdate { FormAction = FormStatus.Escalated.ToString() },
            new FormInfo { FormInfoId = 1, FormStatusId = 3, AllFormsId = 3, Modified = new DateTime(2022, 10, 1) },
            null,
            null,
            new FormPermission
            {
                PermissionFlag = (byte)PermissionFlag.UserActionable,
                Group = new AdfGroup { GroupName = "Group_Name" }, GroupId = Guid.Parse("93E9BCBF-2B1F-4A3B-98CE-91530477F6A0")
            },
            null,
            new FormHistory
            {
                FormInfoId = 1, Created = new DateTime(2022, 10, 1), ActionBy = "System", ActionType = "Escalated to Group_Name",
                AditionalComments = null, AllFormsId = 3, FormStatusId = 3, ActiveRecord = true, ActionByPosition = null
            }
        },
        new object[]
        {
            new FormInfoUpdate { FormAction = FormStatus.Approved.ToString() },
            new FormInfo
            {
                FormInfoId = 1, FormStatusId = 3, AllFormsId = 3, Modified = new DateTime(2022, 10, 1)
            },
            Guid.Parse("93E9BCBF-2B1F-4A3B-98CE-91530477F6A0"),
            new EmployeeDetailsDto {EmployeeEmail = "group_member@email", EmployeePositionTitle = "Group_Member_Position" },
            new FormPermission
            {
                PermissionFlag = (byte)PermissionFlag.View,
                Group = new AdfGroup { GroupName = "Group_Name" }, GroupId = Guid.Parse("93E9BCBF-2B1F-4A3B-98CE-91530477F6A0"),
            },
            null,
            new FormHistory
            {
                FormInfoId = 1, Created = new DateTime(2022, 10, 1), ActionBy = "group_member@email", ActionType = "Approved",
                AditionalComments = null, AllFormsId = 3, FormStatusId = 3, ActiveRecord = true, GroupName = "Group_Name", ActionByPosition = "Group_Member_Position"
            }
        }
    };

    private static bool AssertHistoryMatches(FormHistory expectedHistory, FormHistory actualHistory)
    {
        return expectedHistory.Created == actualHistory.Created && expectedHistory.ActionBy == actualHistory.ActionBy &&
               expectedHistory.ActionType == actualHistory.ActionType &&
               expectedHistory.ActiveRecord == actualHistory.ActiveRecord &&
               expectedHistory.AditionalComments == actualHistory.AditionalComments &&
               expectedHistory.AllFormsId == actualHistory.AllFormsId &&
               expectedHistory.FormInfoId == actualHistory.FormInfoId &&
               expectedHistory.FormStatusId == actualHistory.FormStatusId &&
               expectedHistory.GroupName == actualHistory.GroupName &&
               expectedHistory.ActionByPosition == actualHistory.ActionByPosition;
    }
}