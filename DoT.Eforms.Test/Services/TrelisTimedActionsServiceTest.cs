using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.Constants.E29;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace DoT.Eforms.Test
{
    public class TrelisTimedActionsServiceTest
    {
        private readonly TrelisTimedActionsService _service;
        private readonly Mock<AppDbContext> _dbContext;
        private readonly Mock<ILogger<TrelisTimedActionsService>> _mockLogger;
        private readonly Mock<IRepository<FormInfo>> _formInfoRepository;
        private readonly Mock<IMessageFactoryService> _messageFactory;
        private Mock<ISpViewRepository<TrelisReportModelDto>> _trelisViewRepo;
        private Mock<IRepository<FormPermission>> _formPermissionRepo;

        public TrelisTimedActionsServiceTest()
        {
            _messageFactory = new Mock<IMessageFactoryService>();
            _formInfoRepository = new Mock<IRepository<FormInfo>>();
            _mockLogger = new Mock<ILogger<TrelisTimedActionsService>>();

            _trelisViewRepo = new Mock<ISpViewRepository<TrelisReportModelDto>>();
            _formPermissionRepo = new Mock<IRepository<FormPermission>>();
            _service = new TrelisTimedActionsService(_messageFactory.Object, _formInfoRepository.Object,
                _mockLogger.Object, _trelisViewRepo.Object, _formPermissionRepo.Object);

            _trelisViewRepo.Setup(x => x.ToListAsync(It.IsAny<string>(), It.IsAny<object[]>())).ReturnsAsync(
                new List<TrelisReportModelDto>
                {
                    new()
                    {
                        Branch = "Cannington", EmployeeEmail = "Cannington.Manager@email.com", EmployeeNumber = "1234",
                        EmployeeName = "Cannington Manager", Directorate = "Employee Directorate",
                        PositionTitle = "Centre Manager", TrelisBranchId = 1
                    },
                    new()
                    {
                        Branch = "Mandurah", EmployeeEmail = "Mandurah.Manager@email.com", EmployeeNumber = "5678",
                        EmployeeName = "Mandurah Manager", Directorate = "Employee Directorate",
                        PositionTitle = "Centre Manager", TrelisBranchId = 2
                    }
                });

            _formInfoRepository.Setup(x => x.AddAsync(It.IsAny<FormInfo>())).ReturnsAsync((FormInfo item) =>
            {
                item.FormInfoId = 1;
                return item;
            });
        }

        [Fact]
        public async Task RunAsync_WhenFirstMonth_ShouldCreateNewList()
        {
            var result = new List<FormInfo>();
            _formInfoRepository.Setup(x => x.ListAsync(It.IsAny<ISpecification<FormInfo>>()))
                .ReturnsAsync(new List<FormInfo>());
            _formInfoRepository.Setup(x => x.AddAsync(It.IsAny<FormInfo>()))
                .ReturnsAsync(
                    (FormInfo form) =>
                    {
                        result.Add(form);
                        form.FormInfoId = 1;
                        return form;
                    });
            
            
            var expectedMonth = DateTime.Now.AddMonths(1).Month;
            var expectedYear = DateTime.Now.AddMonths(1).Year;

            await _service.CreateTrelisForms();
            
            Assert.All(result, info =>
            {
                var trelisForm = JsonConvert.DeserializeObject<E29Form>(info.Response);
                
                Assert.Equal((int)FormStatus.Unsubmitted, info.FormStatusId);
                Assert.Equal(nameof(FormStatus.Unsubmitted), info.FormSubStatus);
                Assert.True(info.ActiveRecord);
                Assert.Equal("E29 Create Function", info.CreatedBy);
                Assert.Equal((int)FormType.e29, info.AllFormsId);
                Assert.Equal("Employee Directorate", info.FormOwnerDirectorate);
                Assert.Equal("Employee Directorate", info.InitiatedForDirectorate);
                Assert.Equal("Centre Manager", info.FormOwnerPositionTitle);
                Assert.Equal(expectedMonth, trelisForm.Month);
                Assert.Equal(expectedYear, trelisForm.Year);
                Assert.Empty(trelisForm.Users);
                Assert.Null(trelisForm.RejectionReason);
                Assert.Null(trelisForm.PreviousMonthFormId);
            });
            Assert.Collection(result, formInfo =>
            {
                var trelisForm = JsonConvert.DeserializeObject<E29Form>(formInfo.Response);
                Assert.Equal("Cannington.Manager@email.com", formInfo.FormOwnerEmail);
                Assert.Equal("Cannington Manager",formInfo.FormOwnerName);
                Assert.Equal("1234", formInfo.FormOwnerEmployeeNo);
                Assert.Equal("Cannington", trelisForm.BranchName);
            }, formInfo =>
            {
                var trelisForm = JsonConvert.DeserializeObject<E29Form>(formInfo.Response);
                Assert.Equal("Mandurah.Manager@email.com", formInfo.FormOwnerEmail);
                Assert.Equal("Mandurah Manager", formInfo.FormOwnerName);
                Assert.Equal("5678", formInfo.FormOwnerEmployeeNo);
                Assert.Equal("Mandurah", trelisForm.BranchName);
            });
        }

        [Fact]
        public async Task RunAsync_WhenSecondMonth_ShouldCreateFromPreviousList()
        {
            var result = new List<FormInfo>();
            var now = DateTime.Now;
            _formInfoRepository.Setup(x => x.ListAsync(It.IsAny<ISpecification<FormInfo>>()))
                .ReturnsAsync(new List<FormInfo>
                {
                    new ()
                    {
                        FormInfoId = 5,
                        Created = now.AddMonths(-1),
                        Response = JsonConvert.SerializeObject(new E29Form
                        {
                            BranchName = "Cannington",
                            BranchId = 1,
                            Users = new []
                            {
                                new TeamMember
                                {
                                    Email = "teammate1@email", FromDate = null, ToDate = null, Name = "Team mate1",
                                    PositionTitle = "Position1", RemovedReason = null, TransferBranch = null
                                },
                                new TeamMember
                                {
                                    Email = "teammate2@email", FromDate = now.AddMonths(-2), ToDate = now.AddMonths(+1),
                                    Name = "Team mate2", PositionTitle = "Position2", RemovedReason = 3,
                                    TransferBranch = null
                                },
                                new TeamMember
                                {
                                    Email = "Resiged@teammate", FromDate = now.AddMonths(-1), ToDate = null,
                                    Name = "Resigned teammate", PositionTitle = "Position5", RemovedReason = 1
                                },
                                new TeamMember
                                {
                                    Email = "NotRequired@teammate", FromDate = now.AddMonths(-1), ToDate = null,
                                    Name = "Not Required teammate", PositionTitle = "Position5", RemovedReason = 2
                                }
                            }
                        })
                    },
                    new ()
                    {
                        FormInfoId = 7,
                        Created = now.AddMonths(-1),
                        Response = JsonConvert.SerializeObject(new E29Form
                        {
                            BranchName = "Mandurah",
                            BranchId = 2,
                            Users = new []
                            {
                                new TeamMember
                                {
                                    Email = "teammate3@email", FromDate = null, ToDate = null, Name = "Team mate3",
                                    PositionTitle = "Position3", RemovedReason = null, TransferBranch = null
                                },
                                new TeamMember
                                {
                                    Email = "teammate4@email", FromDate = null, ToDate = null,
                                    Name = "Team mate4", PositionTitle = "Position4", RemovedReason = null,
                                    TransferBranch = null
                                },
                                new TeamMember
                                {
                                    Email = "Transfer@Teammate", FromDate = null, ToDate = null,
                                    Name = "Team mate5", PositionTitle = "Position5", RemovedReason = 4,
                                    TransferBranch = "Other branch"
                                }
                            }
                        })
                    }
                });
            _formInfoRepository.Setup(x => x.AddAsync(It.IsAny<FormInfo>()))
                .ReturnsAsync(
                    (FormInfo form) =>
                    {
                        result.Add(form);
                        form.FormInfoId = 1;
                        return form;
                    });

            await _service.CreateTrelisForms();
            
            Assert.Collection(result, formInfo =>
            {
                var trelisForm = JsonConvert.DeserializeObject<E29Form>(formInfo.Response);
                Assert.Equal(5, trelisForm.PreviousMonthFormId);
                Assert.Collection(trelisForm.Users, user1 =>
                {
                    Assert.Equal("teammate1@email", user1.Email);
                    Assert.Equal("Team mate1", user1.Name);
                    Assert.Equal("Position1", user1.PositionTitle);
                    Assert.Null(user1.TransferBranch);
                    Assert.Null(user1.FromDate);
                    Assert.Null(user1.ToDate);
                    Assert.Null(user1.RemovedReason);
                }, user1 =>
                {
                    Assert.Equal("teammate2@email", user1.Email);
                    Assert.Equal("Team mate2", user1.Name);
                    Assert.Equal("Position2", user1.PositionTitle);
                    Assert.Null(user1.TransferBranch);
                    Assert.Equal(now.AddMonths(-2), user1.FromDate);
                    Assert.Equal(now.AddMonths(1),user1.ToDate);
                    Assert.Equal(3, user1.RemovedReason);
                });
            }, formInfo =>
            {
                var trelisForm = JsonConvert.DeserializeObject<E29Form>(formInfo.Response);
                Assert.Equal(7, trelisForm.PreviousMonthFormId);
                Assert.Collection(trelisForm.Users, user1 =>
                {
                    Assert.Equal("teammate3@email", user1.Email);
                    Assert.Equal("Team mate3", user1.Name);
                    Assert.Equal("Position3", user1.PositionTitle);
                    Assert.Null(user1.TransferBranch);
                    Assert.Null(user1.FromDate);
                    Assert.Null(user1.ToDate);
                    Assert.Null(user1.RemovedReason);
                }, user1 =>
                {
                    Assert.Equal("teammate4@email", user1.Email);
                    Assert.Equal("Team mate4", user1.Name);
                    Assert.Equal("Position4", user1.PositionTitle);
                    Assert.Null(user1.TransferBranch);
                    Assert.Null(user1.FromDate);
                    Assert.Null(user1.ToDate);
                    Assert.Null(user1.RemovedReason);
                });
            });
        }

        [Fact]
        public async Task Create_should_assign_to_access_management_when_no_owner()
        {
            var result = new FormInfo();
            var now = DateTime.Now;
            _formInfoRepository.Setup(x => x.ListAsync(It.IsAny<ISpecification<FormInfo>>()))
                .ReturnsAsync(new List<FormInfo>
                {
                    new()
                    {
                        FormInfoId = 5,
                        Created = now.AddMonths(-1),
                        Response = JsonConvert.SerializeObject(new E29Form
                        {
                            BranchName = "Cannington",
                            BranchId = 1,
                            Users = new[]
                            {
                                new TeamMember
                                {
                                    Email = "teammate1@email", FromDate = null, ToDate = null, Name = "Team mate1",
                                    PositionTitle = "Position1", RemovedReason = null, TransferBranch = null
                                },
                                new TeamMember
                                {
                                    Email = "teammate2@email", FromDate = now.AddMonths(-2), ToDate = now.AddMonths(+1),
                                    Name = "Team mate2", PositionTitle = "Position2", RemovedReason = 3,
                                    TransferBranch = null
                                },
                                new TeamMember
                                {
                                    Email = "Resiged@teammate", FromDate = now.AddMonths(-1), ToDate = null,
                                    Name = "Resigned teammate", PositionTitle = "Position5", RemovedReason = 1
                                },
                                new TeamMember
                                {
                                    Email = "NotRequired@teammate", FromDate = now.AddMonths(-1), ToDate = null,
                                    Name = "Not Required teammate", PositionTitle = "Position5", RemovedReason = 2
                                }
                            }
                        })
                    }
                });
            _trelisViewRepo.Setup(x => x.ToListAsync(It.IsAny<string>(), It.IsAny<object[]>())).ReturnsAsync(
                new List<TrelisReportModelDto>
                {
                    new()
                    {
                        Branch = "Cannington", EmployeeEmail = null, EmployeeNumber = null, EmployeeName = null,
                        PositionTitle = null, Directorate = "Driver and Vehicle Services",
                        TrelisBranchId = 1, PositionId = 1
                    }
                }
            );
            _formInfoRepository.Setup(x => x.AddAsync(It.IsAny<FormInfo>()))
                .ReturnsAsync(
                    (FormInfo form) =>
                    {
                        result = form;
                        form.FormInfoId = 1;
                        return form;
                    });

            await _service.CreateTrelisForms();
            
            var trelisForm = JsonConvert.DeserializeObject<E29Form>(result.Response);
            Assert.Equal(5, trelisForm.PreviousMonthFormId);
            Assert.Null(result.FormOwnerEmail);
            Assert.Equal(E29Constants.TrelisAccessManagementGroupMail, result.NextApprover);
            Assert.Collection(trelisForm.Users, user1 =>
            {
                Assert.Equal("teammate1@email", user1.Email);
                Assert.Equal("Team mate1", user1.Name);
                Assert.Equal("Position1", user1.PositionTitle);
                Assert.Null(user1.TransferBranch);
                Assert.Null(user1.FromDate);
                Assert.Null(user1.ToDate);
                Assert.Null(user1.RemovedReason);
            }, user1 =>
            {
                Assert.Equal("teammate2@email", user1.Email);
                Assert.Equal("Team mate2", user1.Name);
                Assert.Equal("Position2", user1.PositionTitle);
                Assert.Null(user1.TransferBranch);
                Assert.Equal(now.AddMonths(-2), user1.FromDate);
                Assert.Equal(now.AddMonths(1), user1.ToDate);
                Assert.Equal(3, user1.RemovedReason);
            });
        }
        
        [Fact]
        public async Task SendEmail_ShouldSendTheCorrectEmails()
        {
            _formInfoRepository.Setup(x => x.ListAsync(It.IsAny<ISpecification<FormInfo>>())).ReturnsAsync(
                new List<FormInfo>
                {
                    new() { Created = DateTime.Now.AddDays(-2), NextApprover = "something" },
                    new() { Created = DateTime.Now.AddDays(-2), NextApprover = "something" },
                    new() { Created = DateTime.Now.AddDays(-5), NextApprover = "something" },
                    new() { Created = DateTime.Now.AddDays(-2) },
                    new() { Created = DateTime.Now.AddDays(-5), NextApprover = "something" }
                });

            await _service.SendReminderEmailsAsync();
                
            _messageFactory.Verify(
                x => x.SendEmailAsync(It.IsAny<FormInfo>(),
                    It.Is<FormInfoUpdate>(f => f.FormAction == "Unsubmitted")), Times.Exactly(3));
                
            _messageFactory.Verify(
                x => x.SendEmailAsync(It.IsAny<FormInfo>(),
                    It.Is<FormInfoUpdate>(f => f.FormAction == "Delegate")), Times.Exactly(2));
        }
    }
}