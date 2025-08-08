using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DoT.Eforms.Test.Shared;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.Constants.E29;
using eforms_middleware.DataModel;
using eforms_middleware.GetMasterData;
using eforms_middleware.Interfaces;
using eforms_middleware.Validators;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace DoT.Eforms.Test
{
    public class E29FormServiceTest
    {
        private readonly E29FormService _service;
        private readonly Mock<ITaskManager> _mockTaskManager;
        private readonly Mock<IRepository<RefFormStatus>> _mockFormStatusRepository;
        private readonly Mock<IFormHistoryService> _mockFormHistoryService;
        private readonly Mock<IFormInfoService> _formInfoService;
        private readonly Mock<IMessageFactoryService> _mockMessageFactory;
        private readonly Mock<IEmployeeService> _mockEmployeeService;
        private Mock<IRequestingUserProvider> _requestingUserProvider;
        private Mock<IPermissionManager> _permissionManager;
        private Mock<IRepository<FormPermission>> _formPermissionRepo;
        private Mock<IPositionService> _positionService;

        public E29FormServiceTest()
        {
            var mockLog = new Mock<ILogger<E29FormService>>();
            _mockTaskManager = new Mock<ITaskManager>();
            _mockFormStatusRepository = new Mock<IRepository<RefFormStatus>>();
            _mockFormHistoryService = new Mock<IFormHistoryService>();
            var validator = new E29FormValidator();
            _formInfoService = new Mock<IFormInfoService>();
            _mockMessageFactory = new Mock<IMessageFactoryService>();
            _mockEmployeeService = new Mock<IEmployeeService>();

            _requestingUserProvider = new Mock<IRequestingUserProvider>();
            _permissionManager = new Mock<IPermissionManager>();
            _formPermissionRepo = new Mock<IRepository<FormPermission>>();
            _positionService = new Mock<IPositionService>();
            _service = new E29FormService(mockLog.Object, _mockTaskManager.Object,
                _mockFormStatusRepository.Object, _mockFormHistoryService.Object, validator,
                _formInfoService.Object, _mockMessageFactory.Object, _mockEmployeeService.Object,
                _requestingUserProvider.Object, _permissionManager.Object, _formPermissionRepo.Object, _positionService.Object );
            
            _formInfoService.Setup(x =>
                x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>())).Returns(
                (FormInfoUpdate request, FormInfo dbRecord) => Task.FromResult(dbRecord));
            _requestingUserProvider.Setup(x => x.GetRequestingUser()).ReturnsAsync(new EmployeeDetailsDto
            {
                EmployeeEmail = "the.owner@email.com",
                ActiveDirectoryId = new Guid(CoiTestEmployees.NormalEmployee)
            });
        }
        
        [Fact]
        public async Task ProcessAsync_WhenInvalid_ReturnsIssue()
        {
            _formInfoService.Setup(x => x.GetExistingOrNewFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.Is<bool>(x => x)))
                .ReturnsAsync(new FormInfo
                {
                    FormInfoId = 2, AllFormsId = (int)FormType.e29, FormStatusId = (int)FormStatus.Unsubmitted,
                    Response = JsonConvert.SerializeObject(new E29Form
                    {
                        PreviousMonthFormId = 1, BranchName = "Branch 1", Month = 3, Year = 2022,
                        RejectionReason = null,
                        Users = new List<TeamMember>()
                    })
                });
            _formInfoService.Setup(x => x.GetFormInfoByIdAsync(It.IsAny<int>())).ReturnsAsync(new FormInfo
            {
                FormInfoId = 1, AllFormsId = (int)FormType.e29, FormStatusId = (int)FormStatus.Unsubmitted,
                Response = JsonConvert.SerializeObject(new E29Form
                {
                    PreviousMonthFormId = null, BranchName = "Branch 1", Month = 2, Year = 2022,
                    RejectionReason = null,
                    Users = new[]
                    {
                        new TeamMember
                        {
                            Email = "User@email", FromDate = null, ToDate = null, Name = "TestUser",
                            PositionTitle = "Position", RemovedReason = null, TransferBranch = null
                        }
                    }
                })
            });
            var request = new FormInfoUpdate
            { FormAction = "Unsubmitted", FormDetails = new FormDetailsRequest { FormInfoId = 2 } };
            var result = await _service.ProcessRequest(request);
            Assert.False(result.Success);
            _mockFormHistoryService.Verify(
                x => x.AddFormHistoryAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>(), It.IsAny<Guid>(),
                    It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
            _mockMessageFactory.Verify(
                x => x.SendEmailAsync(It.IsAny<FormInfo>(), It.IsAny<FormInfoUpdate>()), Times.Never);
        }

        [Fact]
        public async Task ProcessAsync_ReturnsSuccessfully()
        {
            var request = new FormInfoUpdate
            {
                FormAction = nameof(FormStatus.Unsubmitted), FormDetails = new FormDetailsRequest
                {
                    AllFormsId = (int)FormType.e29, FormInfoId = 2, NextApprover = null,
                    Response = JsonConvert.SerializeObject(new E29Form
                    {
                        PreviousMonthFormId = null, BranchName = "New Branch", Month = 2, Year = 2022,
                        RejectionReason = null,
                        Users = new[]
                        {
                            new TeamMember
                            {
                                Email = "User@email", FromDate = null, ToDate = null, Name = "TestUser",
                                PositionTitle = "Position", RemovedReason = null, TransferBranch = null
                            }
                        }
                    })
                },
                UserId = "test@email"
            };
            _formInfoService.Setup(x => x.GetExistingOrNewFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.Is<bool>(x => x)))
                .ReturnsAsync(new FormInfo
                {
                    FormInfoId = 2, AllFormsId = (int)FormType.e29, FormStatusId = (int)FormStatus.Unsubmitted,
                    FormOwnerEmail = "the.owner@email.com",
                    Response = JsonConvert.SerializeObject(new E29Form
                {
                    PreviousMonthFormId = null, BranchName = "Branch 1", Month = 2, Year = 2022, RejectionReason = null,
                    Users = new []
                    {
                        new TeamMember {
                            Email = "User@email", FromDate = null, ToDate = null, Name = "TestUser",
                            PositionTitle = "Position", RemovedReason = null, TransferBranch = null
                        }
                    }
                })});
            

            var result = await _service.ProcessRequest(request);
            
            Assert.NotNull(result);
            _mockFormHistoryService.Verify(
                x => x.AddFormHistoryAsync(It.Is<FormInfoUpdate>(f => f.FormAction == "Unsubmitted"),
                    It.Is<FormInfo>(f => f.FormStatusId == (int)FormStatus.Unsubmitted),
                    It.Is<Guid?>(s => !s.HasValue),
                    It.IsAny<string>(),
                    It.Is<string>(s => string.IsNullOrWhiteSpace(s))),
                Times.Once);
            _mockMessageFactory.Verify(
                x => x.SendEmailAsync(It.IsAny<FormInfo>(), It.IsAny<FormInfoUpdate>()), Times.Never);
        }

        [Fact]
        public async Task Process_ShouldRecordHistory()
        {
            var request = new FormInfoUpdate
            {
                FormAction = nameof(FormStatus.Submitted), FormDetails = new FormDetailsRequest
                {
                    AllFormsId = (int)FormType.e29, FormInfoId = 2, NextApprover = null,
                    Response = JsonConvert.SerializeObject(new E29Form
                    {
                        PreviousMonthFormId = null, BranchName = "New Branch", Month = 2, Year = 2022,
                        RejectionReason = null,
                        Users = new[]
                        {
                            new TeamMember
                            {
                                Email = "User@email", FromDate = null, ToDate = null, Name = "TestUser",
                                PositionTitle = "Position", RemovedReason = null, TransferBranch = null
                            }
                        }
                    })
                },
                UserId = "test@email"
            };
            _formInfoService.Setup(x => x.GetExistingOrNewFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<bool>()))
                .ReturnsAsync(new FormInfo
                {
                    FormInfoId = 2, AllFormsId = (int)FormType.e29, FormStatusId = (int)FormStatus.Unsubmitted,
                    NextApprover = "some.group@email.com", FormOwnerEmail = "the.owner@email.com",
                    FormPermissions = new List<FormPermission>
                    {
                        new () { PermissionFlag = (byte)PermissionFlag.UserActionable, GroupId = E29Constants.TRELIS_ACCESS_MANAGEMENT_ID }
                    },
                    Response = JsonConvert.SerializeObject(new E29Form
                    {
                        PreviousMonthFormId = null, BranchName = "Branch 1", Month = 2, Year = 2022,
                        RejectionReason = null,
                        Users = new[]
                        {
                            new TeamMember
                            {
                                Email = "User@email", FromDate = null, ToDate = null, Name = "TestUser",
                                PositionTitle = "Position", RemovedReason = null, TransferBranch = null
                            }
                        }
                    })
                });

            await _service.ProcessRequest(request);
            
            _mockFormHistoryService.Verify(
                x => x.AddFormHistoryAsync(It.Is<FormInfoUpdate>(f => f.FormAction == "Submitted"),
                    It.Is<FormInfo>(f => f.FormStatusId == (int)FormStatus.Submitted),
                    It.Is<Guid?>(s => s == E29Constants.TRELIS_ACCESS_MANAGEMENT_ID),
                    It.Is<string>(s => string.IsNullOrWhiteSpace(s)),
                    It.Is<string>(s => string.IsNullOrWhiteSpace(s))),
                Times.Once);
            _mockMessageFactory.Verify(
                x => x.SendEmailAsync(It.IsAny<FormInfo>(), It.IsAny<FormInfoUpdate>()), Times.Once);
        }

        [Fact]
        public async Task Process_WhenDelegationInHistory_ShouldBlockDelegate()
        {
            var request = new FormInfoUpdate
            {
                FormAction = nameof(FormStatus.Delegate), FormDetails = new FormDetailsRequest
                {
                    AllFormsId = (int)FormType.e29, FormInfoId = 2, NextApprover = "some@email.com",
                    Response = JsonConvert.SerializeObject(new E29Form
                    {
                        PreviousMonthFormId = null, BranchName = "New Branch", Month = 2, Year = 2022,
                        RejectionReason = null,
                        Users = new[]
                        {
                            new TeamMember
                            {
                                Email = "User@email", FromDate = null, ToDate = null, Name = "TestUser",
                                PositionTitle = "Position", RemovedReason = null, TransferBranch = null
                            }
                        }
                    })
                },
                UserId = "test@email"
            };
            _formInfoService.Setup(x => x.GetExistingOrNewFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.Is<bool>(x => x)))
                .ReturnsAsync(new FormInfo
                {
                    FormInfoId = 2, AllFormsId = (int)FormType.e29, FormStatusId = (int)FormStatus.Unsubmitted,
                    Response = JsonConvert.SerializeObject(new E29Form
                    {
                        PreviousMonthFormId = 1, BranchName = "Branch 1", Month = 2, Year = 2022, RejectionReason = null,
                        Users = new []
                        {
                            new TeamMember {
                                Email = "User@email", FromDate = null, ToDate = null, Name = "TestUser",
                                PositionTitle = "Position", RemovedReason = null, TransferBranch = null
                            }
                        }
                    })});
            _mockFormHistoryService
                .Setup(x => x.GetFirstOrDefaultHistoryBySpecification(It.IsAny<ISpecification<FormHistory>>()))
                .ReturnsAsync(new FormHistory
            {
                FormStatusId = (int)FormStatus.Delegate, AllFormsId = (int)FormType.e29, FormInfoId = 2
            });
            _mockEmployeeService.Setup(x => x.GetEmployeeDetailsAsync(It.IsAny<string>()))
                .ReturnsAsync(new EmployeeDetailsDto { EmployeeEmail = "some@email.com"});

            var result = await _service.ProcessRequest(request);
            
            Assert.False(result.Success);
            var jsonResult = JsonConvert.SerializeObject(result.Value);
            Assert.Equal("[{\"Message\":\"Delegate not available\"}]", jsonResult);
            _mockFormHistoryService.Verify(
                x => x.AddFormHistoryAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>(), It.IsAny<Guid?>(),
                    It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
            _mockMessageFactory.Verify(
                x => x.SendEmailAsync(It.IsAny<FormInfo>(), It.IsAny<FormInfoUpdate>()), Times.Never);
        }

        [Fact]
        public async Task Process_WhenRejecting_ShouldRecordStatusAsUnsubmitted()
        {
            var request = new FormInfoUpdate
            {
                FormAction = nameof(FormStatus.Rejected), FormDetails = new FormDetailsRequest
                {
                    AllFormsId = (int)FormType.e29, FormInfoId = 2, NextApprover = "some@email.com",
                    Response = JsonConvert.SerializeObject(new E29Form
                    {
                        PreviousMonthFormId = null, BranchName = "New Branch", Month = 2, Year = 2022,
                        RejectionReason = "Passed reason for failing the request",
                        Users = new[]
                        {
                            new TeamMember
                            {
                                Email = "User@email", FromDate = null, ToDate = null, Name = "TestUser",
                                PositionTitle = "Position", RemovedReason = null, TransferBranch = null
                            }
                        }
                    })
                },
                UserId = "test@email"
            };
            _formInfoService.Setup(x => x.GetExistingOrNewFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.Is<bool>(x => x)))
                .ReturnsAsync(new FormInfo
                {
                    FormInfoId = 2, AllFormsId = (int)FormType.e29, FormStatusId = (int)FormStatus.Submitted,
                    FormPermissions = new List<FormPermission>
                    {
                        new () { PermissionFlag = (byte)PermissionFlag.UserActionable, GroupId = E29Constants.TRELIS_ACCESS_MANAGEMENT_ID }
                    },
                    Response = JsonConvert.SerializeObject(new E29Form
                    {
                        PreviousMonthFormId = null, BranchName = "Branch 1", Month = 2, Year = 2022, RejectionReason = null,
                        Users = new []
                        {
                            new TeamMember {
                                Email = "User@email", FromDate = null, ToDate = null, Name = "TestUser",
                                PositionTitle = "Position", RemovedReason = null, TransferBranch = null
                            }
                        }
                    }),
                    FormOwnerEmail = "some@email.com", NextApprover = "group@email.com"
                });
            _mockEmployeeService.Setup(x => x.GetEmployeeDetailsAsync(It.IsAny<string>()))
                .ReturnsAsync(new EmployeeDetailsDto { EmployeeEmail = "some@email.com"});
            _mockFormHistoryService
                .Setup(x => x.GetFirstOrDefaultHistoryBySpecification(It.IsAny<ISpecification<FormHistory>>()))
                .ReturnsAsync(new FormHistory
                {
                    ActionBy = "some@email.com"
                });
            _formPermissionRepo.Setup(x => x.SingleOrDefaultAsync(It.IsAny<ISpecification<FormPermission>>()))
                .ReturnsAsync(new FormPermission {PositionId = 3, Position = new AdfPosition { AdfUserReportsToPositions = new List<AdfUser> {new () {EmployeeEmail = "some@email"}}}});

            await _service.ProcessRequest(request);
            _mockFormHistoryService.Verify(
                x => x.AddFormHistoryAsync(It.Is<FormInfoUpdate>(f => f.FormAction == "Rejected"),
                    It.Is<FormInfo>(f => f.FormStatusId == (int)FormStatus.Unsubmitted),
                    It.Is<Guid?>(s => s == E29Constants.TRELIS_ACCESS_MANAGEMENT_ID),
                     It.IsAny<string>(),
                    It.Is<string>(s => s == "Passed reason for failing the request")
                   ),
                Times.Once);
            _mockMessageFactory.Verify(
                x => x.SendEmailAsync(It.IsAny<FormInfo>(), It.IsAny<FormInfoUpdate>()), Times.Once);
        }
    }
}