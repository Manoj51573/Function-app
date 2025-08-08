using DoT.Eforms.Test.Shared;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels;
using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure.Interfaces;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Workflows;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace DoT.Eforms.Test.Services
{
    public class CoIFormServiceTest
    {
        private readonly CoIFormService<CoIForm> _service;
        private readonly Mock<IFormHistoryService> _mockFormHistoryService;
        private readonly Mock<IFormInfoService> _mockFormInfoService;
        private readonly Mock<IRequestingUserProvider> _requestingUserProvider;
        private readonly Mock<IFormSubtypeHelper<CoIForm>> _formSubtypeHelper;

        public CoIFormServiceTest()
        {
            var mockTaskManager = new Mock<ITaskManager>();
            var mockFormStatusRepository = new Mock<IRepository<RefFormStatus>>();
            _mockFormHistoryService = new Mock<IFormHistoryService>();
            _mockFormInfoService = new Mock<IFormInfoService>();
            var mockMessageFactory = new Mock<IMessageFactoryService>();
            var mockEmployeeService = new Mock<IEmployeeService>();
            var positionService = new Mock<IPositionService>();


            var log = new Mock<ILogger<CoIFormService<CoIForm>>>();
            _requestingUserProvider = new Mock<IRequestingUserProvider>();
            var permissionManager = new Mock<IPermissionManager>();
            _formSubtypeHelper = new Mock<IFormSubtypeHelper<CoIForm>>();
            _service = new CoIFormService<CoIForm>(log.Object, mockTaskManager.Object,
                mockFormStatusRepository.Object, _mockFormHistoryService.Object,
                _mockFormInfoService.Object, mockMessageFactory.Object,
                mockEmployeeService.Object, _formSubtypeHelper.Object, _requestingUserProvider.Object, permissionManager.Object, positionService.Object);

            _mockFormInfoService.Setup(x =>
                x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>())).Returns(
                (FormInfoUpdate _, FormInfo dbRecord) => Task.FromResult(dbRecord));
            mockEmployeeService.Setup(x => x.GetEmployeeByAzureIdAsync(It.IsAny<Guid>()))
                .ReturnsAsync((Guid id) => CoiTestEmployees.GetEmployeeDetails(id));
            mockEmployeeService
                .Setup(x => x.GetEmployeeByPositionNumberAsync(It.Is<int>(p =>
                    p == ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID))).ReturnsAsync(new List<IUserInfo> {new EmployeeDetailsDto
                {
                    EmployeeEmail = "edP&C@email.com", EmployeeFirstName = "ED", EmployeeSurname = "P&C",
                    EmployeePositionId = ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID
                }});
            mockEmployeeService
                .Setup(x => x.GetEmployeeByPositionNumberAsync(It.Is<int>(p =>
                    p == ConflictOfInterest.ED_ODG_POSITION_ID))).ReturnsAsync(new List<IUserInfo> { new EmployeeDetailsDto
                {
                    EmployeeEmail = "EdOdg@email.com", EmployeePositionId = ConflictOfInterest.ED_ODG_POSITION_ID
                }});
            _mockFormInfoService.Setup(x => x.GetExistingOrNewFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<bool>())).ReturnsAsync(
                (FormInfoUpdate request, bool addAttachments) =>
                {
                    switch (request.FormDetails.FormInfoId)
                    {
                        case null:
                            return new FormInfo { AllFormsId = (int)FormType.CoI_CPR };
                        case 1:
                            return new FormInfo { FormInfoId = 1, FormStatusId = (int)FormStatus.Unsubmitted, AllFormsId = (int)FormType.CoI_CPR };
                        case 2:
                            return new FormInfo { FormInfoId = 2, FormStatusId = (int)FormStatus.Submitted, AllFormsId = (int)FormType.CoI_CPR, FormOwnerEmail = "normal.employee@email", NextApprover = "edP&C@email.com" };
                        case 3:
                            return new FormInfo { FormInfoId = 3, FormStatusId = (int)FormStatus.Approved, AllFormsId = (int)FormType.CoI_CPR };
                        case 4:
                            return new FormInfo { FormInfoId = 4, FormStatusId = (int)FormStatus.Unsubmitted, AllFormsId = (int)FormType.CoI_GBC };
                    }

                    return null;
                });
            _requestingUserProvider.Setup(x => x.GetRequestingUser()).ReturnsAsync(new EmployeeDetailsDto()
            { EmployeeEmail = "normal.employee@email", EmployeeNumber = "1234", EmployeePositionId = 1, ActiveDirectoryId = Guid.Parse(CoiTestEmployees.NormalEmployee) });
            _formSubtypeHelper.Setup(x => x.GetFinalApprovers()).ReturnsAsync(new ApprovalInfo
            {
                PositionId = ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID,
                NextApprover = "edP&C@email.com"
            });
            _formSubtypeHelper.Setup(x => x.ValidateInternal(It.IsAny<FormInfoUpdate>()))
                .ReturnsAsync(new ValidationResult());
            _formSubtypeHelper.Setup(x => x.GetValidSerializedModel(It.IsAny<FormInfo>(), It.IsAny<FormInfoUpdate>()))
                .Returns("");
            positionService
                .Setup(x => x.GetPositionByIdAsync(It.Is<int>(p => p == ConflictOfInterest.ED_ODG_POSITION_ID), It.IsAny<bool>()))
                .ReturnsAsync(new AdfPosition
                {
                    PositionTitle = "Executive Director Office of the Director General",
                    AdfUserPositions = new List<AdfUser>
                    {
                        new () { EmployeeEmail = "EdOdg@email.com" }
                    }
                });

        }

        [Fact]
        public async Task Process_WhenNotCreated_Succeeds()
        {
            var employeeForm = new CprEmployeeForm
            {
                From = DateTime.Now,
                ConflictType = ConflictOfInterest.ConflictType.Actual,
                Description = "Description"
            };
            var serializedForm = JsonConvert.SerializeObject(employeeForm);
            var formInfo = new FormInfoUpdate
            {
                FormAction = nameof(FormStatus.Unsubmitted),
                FormDetails = new FormDetailsRequest
                {
                    Response = serializedForm
                }
            };

            var result = await _service.ProcessRequest(formInfo);

            Assert.True(result.Success);
            _mockFormInfoService.Verify(x => x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.Is<FormInfo>(f =>
                f.FormStatusId == (int)FormStatus.Unsubmitted &&
                f.FormSubStatus == Enum.GetName(FormStatus.Unsubmitted))), Times.Once);
        }

        [Theory]
        [InlineData(FormStatus.Submitted, FormStatus.Submitted, null, 1234, "ToManager@email", CoiTestEmployees.NormalEmployee)]
        [InlineData(FormStatus.Submitted, FormStatus.Submitted, null, 1234, "Dual Occupant Position", CoiTestEmployees.DualManager)]
        [InlineData(FormStatus.Endorsed, FormStatus.Endorsed, null, ConflictOfInterest.DIRECTOR_GENERAL_POSITION_NUMBER, "EdOdg@email.com", CoiTestEmployees.DirectorGeneral)]
        [InlineData(FormStatus.Submitted, FormStatus.Submitted, null, 123, "normal_ed@email", CoiTestEmployees.ManagerlessEmployee)]
        [InlineData(FormStatus.Submitted, FormStatus.Submitted, "ED", 123, "edP&C@email.com", CoiTestEmployees.NormalEmployee)]
        [InlineData(FormStatus.Endorsed, FormStatus.Endorsed, null, ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID, "MdDot@email", CoiTestEmployees.EdPAndC)]
        [InlineData(FormStatus.Endorsed, FormStatus.Endorsed, "ED", ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID, "MdDot@email", CoiTestEmployees.EdPAndC)]
        [InlineData(FormStatus.Endorsed, FormStatus.Endorsed, "ED", ConflictOfInterest.DIRECTOR_GENERAL_POSITION_NUMBER, "edP&C@email.com", CoiTestEmployees.DirectorGeneral)]
        public async Task Process_WhenUpdating_Succeeds(FormStatus expectedFormStatusId,
            FormStatus expectedFormSubStatus, string requestedNextApprover, int employeePositionNumber,
            string expectedNextApprover, string requestingUserId)
        {
            var formInfo = new FormInfoUpdate
            {
                FormAction = nameof(FormStatus.Submitted),
                FormDetails = new FormDetailsRequest
                {
                    FormInfoId = 1,
                    AllFormsId = (int)FormType.CoI_CPR,
                    Response = JsonConvert.SerializeObject(new CprEmployeeForm
                    {
                        From = DateTime.Now,
                        ConflictType = ConflictOfInterest.ConflictType.Actual,
                        Description = "Description",
                        IsDeclarationAcknowledged = true
                    }),
                    NextApprover = requestedNextApprover
                }
            };
            _requestingUserProvider.Setup(x => x.GetRequestingUser())
                .ReturnsAsync(CoiTestEmployees.GetEmployeeDetails(new Guid(requestingUserId)));

            var result = await _service.ProcessRequest(formInfo);

            Assert.True(result.Success);
            _mockFormInfoService.Verify(x => x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.Is<FormInfo>(f =>
                f.FormStatusId == (int)expectedFormStatusId &&
                f.FormSubStatus == Enum.GetName(expectedFormSubStatus) &&
                f.NextApprover == expectedNextApprover)), Times.Once);
        }

        [Theory]
        [InlineData(CoiTestEmployees.ManagerlessAndEdlessEmployee, FormStatus.Submitted)]
        [InlineData(CoiTestEmployees.EdlessEmployee, FormStatus.Approved)]
        public async Task ProcessAsync_WhenNoManagerOrEd_ShouldBlockProgressAndInform(string employeeId, FormStatus formStatus)
        {
            _requestingUserProvider.Setup(x => x.GetRequestingUser())
                .ReturnsAsync(CoiTestEmployees.GetEmployeeDetails(new Guid(employeeId)));
            string serializedForm;
            string expectedMessage;
            if (formStatus == FormStatus.Approved)
            {
                _formSubtypeHelper.Setup(x => x.GetFinalApprovers()).ReturnsAsync((ApprovalInfo)null);
                expectedMessage = "Employee does not have a Tier 3 assigned currently. Try again later.";
                _mockFormInfoService.Setup(x => x.GetExistingOrNewFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.Is<bool>(x => x)))
                    .ReturnsAsync(new FormInfo
                    {
                        FormStatusId = (int)FormStatus.Submitted,
                        FormPermissions = new List<FormPermission>
                        {
                            new () { UserId = Guid.Parse(employeeId), IsOwner = true }
                        }
                    });
                var managerForm = new CoiManagerForm
                {
                    ConflictType = ConflictOfInterest.ConflictType.NoConflict,
                    IsDeclarationAcknowledged = true
                };
                serializedForm = JsonConvert.SerializeObject(managerForm);
            }
            else
            {
                _formSubtypeHelper.Setup(x => x.GetFinalApprovers()).ReturnsAsync(new ApprovalInfo());
                expectedMessage = "You do not have a Tier 3 assigned currently. Save form and try again later.";
                var employeeForm = new CprEmployeeForm
                {
                    From = DateTime.Now,
                    ConflictType = ConflictOfInterest.ConflictType.Actual,
                    Description = "Description",
                    IsDeclarationAcknowledged = true
                };
                serializedForm = JsonConvert.SerializeObject(employeeForm);
            }
            var formInfo = new FormInfoUpdate
            {
                FormAction = Enum.GetName(formStatus),
                FormDetails = new FormDetailsRequest
                {
                    Response = serializedForm
                }
            };

            var exception = await Assert.ThrowsAsync<Exception>(() => _service.ProcessRequest(formInfo));

            Assert.Equal(expectedMessage, exception.Message);
            _mockFormInfoService.Verify(
                x => x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>()),
                Times.Never);
            _mockFormHistoryService.Verify(
                x => x.AddFormHistoryAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>(),
                    It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _requestingUserProvider.Setup(x => x.GetRequestingUser()).ReturnsAsync(new EmployeeDetailsDto());
        }

        [Theory]
        [MemberData(nameof(GetInvalidRequests))]
        public async Task ProcessAsync_ShouldValidateForm(FormInfoUpdate request)
        {
            _formSubtypeHelper.Setup(x => x.ValidateInternal(It.IsAny<FormInfoUpdate>()))
                .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("error", "error") }));

            var result = await _service.ProcessRequest(request);
            Assert.False(result.Success);
            Assert.Equal(StatusCodes.Status422UnprocessableEntity, result.StatusCode);
            _mockFormHistoryService.Verify(
                x => x.AddFormHistoryAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>(),
                    It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData(123, true)]
        [InlineData(null, true)]
        public async Task ProcessAsync_WhenUserIsFfsContractor_BlocksCreate(int? positionId, bool leaveFlag)
        {
            var formInfo = new FormInfoUpdate
            {
                FormAction = nameof(FormStatus.Unsubmitted),
                FormDetails = new FormDetailsRequest
                {
                    Response = JsonConvert.SerializeObject(new CoIForm())
                }
            };
            _requestingUserProvider.Setup(x => x.GetRequestingUser())
                .ReturnsAsync(new EmployeeDetailsDto()
                {
                    EmployeeEmail = "email",
                    // LeaveFlag = leaveFlag ? "Full Day" : null,
                    EmployeeNumber = null,
                    EmployeePositionId = positionId
                });
            var result = await _service.ProcessRequest(formInfo);
            Assert.False(result.Success);
            Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
            _mockFormHistoryService.Verify(
                x => x.AddFormHistoryAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>(),
                    It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Theory]
        [InlineData(FormStatus.Recall, FormStatus.Unsubmitted)]
        [InlineData(FormStatus.Recall, FormStatus.Completed)]
        [InlineData(FormStatus.Approved, FormStatus.Unsubmitted)]
        [InlineData(FormStatus.Submitted, FormStatus.Endorsed)]
        [InlineData(FormStatus.Approved, FormStatus.Endorsed)]
        public async Task Process_WhenActionUnavailable_Fails(FormStatus action, FormStatus dbStatus)
        {
            var formInfoUpdate = new FormInfoUpdate
            {
                FormAction = Enum.GetName(typeof(FormStatus), action),
                FormDetails = new FormDetailsRequest
                {
                    AllFormsId = (int)FormType.CoI_CPR,
                    FormInfoId = 1,
                    Response = ""
                }
            };
            _mockFormInfoService.Setup(x => x.GetExistingOrNewFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.Is<bool>(x => x))).ReturnsAsync(
                new FormInfo
                {
                    FormStatusId = (int)dbStatus
                });

            var result = await _service.ProcessRequest(formInfoUpdate);

            Assert.False(result.Success);
            Assert.Equal(StatusCodes.Status422UnprocessableEntity, result.StatusCode);
            _mockFormInfoService.Verify(
                x => x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>()),
                Times.Never);
            _mockFormHistoryService.Verify(
                x => x.AddFormHistoryAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>(),
                    It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Process_WhenSubmitted_CanRecall()
        {
            var formInfo = GetTestStartingModels(false, (int)FormStatus.Submitted);
            var request = new FormInfoUpdate
            {
                FormAction = nameof(FormStatus.Recall),
                FormDetails = new FormDetailsRequest
                {
                    FormInfoId = 2
                }
            };
            _mockFormInfoService.Setup(x => x.GetExistingOrNewFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.Is<bool>(x => x)))
                .ReturnsAsync(formInfo);

            var result = await _service.ProcessRequest(request);

            Assert.True(result.Success);
            _mockFormInfoService.Verify(
                x => x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(),
                    It.Is<FormInfo>(f => f.FormStatusId == 2 && f.NextApprover == null && f.Response == formInfo.Response)),
                Times.Once);
            _mockFormHistoryService.Verify(
                x => x.AddFormHistoryAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>(),
                    It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task Process_WhenApproving_Succeeds()
        {
            var formInfo = GetTestStartingModels(false, (int)FormStatus.Submitted);
            _requestingUserProvider.Setup(x => x.GetRequestingUser()).ReturnsAsync(new EmployeeDetailsDto()
            { EmployeeEmail = "normal_manager@email", EmployeeNumber = "1234", ActiveDirectoryId = Guid.Parse(CoiTestEmployees.NormalManager) });
            _mockFormInfoService.Setup(x => x.GetExistingOrNewFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.Is<bool>(x => x)))
                .ReturnsAsync(formInfo);
            var request = new FormInfoUpdate
            {
                FormAction = Enum.GetName(FormStatus.Approved),
                FormDetails = new FormDetailsRequest
                {
                    FormInfoId = 2,
                    Response = ""
                }
            };

            var result = await _service.ProcessRequest(request);

            Assert.True(result.Success);
            _mockFormInfoService.Verify(
                x => x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(),
                    It.Is<FormInfo>(f => f.FormStatusId == 3 && f.NextApprover == "ED@email" && f.FormSubStatus == "Approved")),
                Times.Once);
            _mockFormHistoryService.Verify(
                x => x.AddFormHistoryAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>(),
                    It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }

        [Theory]
        [InlineData(FormStatus.Submitted, "ToManager@email")]
        [InlineData(FormStatus.Endorsed, "ed@email.com")]
        [InlineData(FormStatus.Approved, "edP&C@email.com")]
        public async Task Process_WhenRejecting_Succeeds(FormStatus status, string requesterEmail)
        {
            var formInfo = GetTestStartingModels(true, (int)status);
            _requestingUserProvider.Setup(x => x.GetRequestingUser()).ReturnsAsync(new EmployeeDetailsDto
            { EmployeeEmail = requesterEmail, EmployeeNumber = "1234" });
            _mockFormInfoService.Setup(x => x.GetExistingOrNewFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.Is<bool>(x => x))).ReturnsAsync(
                formInfo);
            var requestResponse = new CoiRejection { RejectionReason = "expected rejection reason" };
            var request = new FormInfoUpdate
            {
                FormAction = Enum.GetName(FormStatus.Rejected),
                FormDetails = new FormDetailsRequest
                {
                    FormInfoId = 2,
                    Response = JsonConvert.SerializeObject(requestResponse)
                }
            };

            var result = await _service.ProcessRequest(request);

            Assert.True(result.Success);
            _mockFormInfoService.Verify(
                x => x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(),
                    It.Is<FormInfo>(f =>
                        f.FormStatusId == 2 && f.NextApprover == null && f.FormSubStatus == "Unsubmitted")),
                Times.Once);
            _mockFormHistoryService.Verify(
                x => x.AddFormHistoryAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>(),
                      It.IsAny<Guid?>()
                     , It.IsAny<string>()
                     , It.Is<string>(a => a == "expected rejection reason")
                   ),
                Times.Once);
        }

        [Fact]
        public async Task Process_WhenEndorsing_Succeeds()
        {
            var formInfo = GetTestStartingModels(true, (int)FormStatus.Approved);
            _requestingUserProvider.Setup(x => x.GetRequestingUser()).ReturnsAsync(new EmployeeDetailsDto
            { EmployeeEmail = "normal_ed@email", EmployeeNumber = "1234", ActiveDirectoryId = Guid.Parse(CoiTestEmployees.NormalExecutiveDirector) });
            _mockFormInfoService.Setup(x => x.GetExistingOrNewFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.Is<bool>(x => x))).ReturnsAsync(formInfo);
            var requestResponse = new CoiEndorsementForm { AdditionalComments = "comments" };
            var request = new FormInfoUpdate
            {
                FormAction = Enum.GetName(FormStatus.Endorsed),
                FormDetails = new FormDetailsRequest
                {
                    FormInfoId = 3,
                    Response = JsonConvert.SerializeObject(requestResponse)
                }
            };

            var result = await _service.ProcessRequest(request);

            Assert.True(result.Success);
            _mockFormInfoService.Verify(
                x => x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(),
                    It.Is<FormInfo>(f =>
                        f.FormStatusId == (int)FormStatus.Endorsed && f.NextApprover == "edP&C@email.com" &&
                        f.FormSubStatus == "Endorsed")),
                Times.Once);
            _mockFormHistoryService.Verify(
                x => x.AddFormHistoryAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>(),
                    It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task Process_WhenCompleting_ShouldComplete()
        {
            var formInfo = GetTestStartingModels(true, (int)FormStatus.Endorsed);
            _requestingUserProvider.Setup(x => x.GetRequestingUser()).ReturnsAsync(new EmployeeDetailsDto
            { EmployeeEmail = "EdPAndC@email", EmployeeNumber = "1234", EmployeePositionId = ConflictOfInterest.ED_PEOPLE_AND_CULTURE_POSITION_ID, ActiveDirectoryId = Guid.Parse(CoiTestEmployees.EdPAndC) });
            _mockFormInfoService.Setup(x => x.GetExistingOrNewFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.Is<bool>(x => x))).ReturnsAsync(formInfo);
            var request = new FormInfoUpdate
            {
                FormAction = Enum.GetName(FormStatus.Completed),
                FormDetails = new FormDetailsRequest
                {
                    FormInfoId = 3,
                    Response = ""
                }
            };

            var result = await _service.ProcessRequest(request);

            Assert.True(result.Success);
            _mockFormInfoService.Verify(
                x => x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(),
                    It.Is<FormInfo>(f =>
                        f.FormStatusId == (int)FormStatus.Completed && f.NextApprover == null &&
                        f.FormSubStatus == "Completed")),
                Times.Once);
            _mockFormHistoryService.Verify(
                x => x.AddFormHistoryAsync(It.IsAny<FormInfoUpdate>(), It.IsAny<FormInfo>(),
                    It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task Approve_by_Ed_PAndC_goes_to_complete()
        {
            _requestingUserProvider.Setup(x => x.GetRequestingUser()).ReturnsAsync(new EmployeeDetailsDto
            {
                EmployeeEmail = "EdPAndC@email",
                ActiveDirectoryId = Guid.Parse(CoiTestEmployees.EdPAndC)
            });
            var formInfo = new FormInfoUpdate
            {
                FormAction = nameof(FormStatus.Approved),
                FormDetails = new FormDetailsRequest
                {
                    FormInfoId = 2,
                    AllFormsId = (int)FormType.CoI_CPR,
                    Response = JsonConvert.SerializeObject(new CoiManagerForm
                    {
                        ConflictType = ConflictOfInterest.ConflictType.Actual,
                        IsDeclarationAcknowledged = true,
                        ActionsTaken = "Something"
                    })
                }
            };

            var result = await _service.ProcessRequest(formInfo);

            Assert.True(result.Success);
            _mockFormInfoService.Verify(x => x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.Is<FormInfo>(f =>
                f.FormStatusId == (int)FormStatus.Completed &&
                f.FormSubStatus == Enum.GetName(FormStatus.Completed) &&
                f.NextApprover == null)), Times.Once);
        }

        [Fact]
        public async Task Submit_ShouldIgnoreEDRequest_WhereSubtypeHelperPreventsIt()
        {
            _requestingUserProvider.Setup(x => x.GetRequestingUser())
                .ReturnsAsync(CoiTestEmployees.GetEmployeeDetails(new Guid(CoiTestEmployees.NormalEmployee)));
            var formInfo = new FormInfoUpdate
            {
                FormAction = nameof(FormStatus.Submitted),
                FormDetails = new FormDetailsRequest
                {
                    FormInfoId = 4,
                    AllFormsId = (int)FormType.CoI_GBC,
                    Response = JsonConvert.SerializeObject(new GbcEmployeeForm()),
                    NextApprover = "ED"
                }
            };

            var result = await _service.ProcessRequest(formInfo);

            Assert.True(result.Success);
            _mockFormInfoService.Verify(x => x.SaveFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.Is<FormInfo>(f =>
                f.FormStatusId == (int)FormStatus.Submitted &&
                f.FormSubStatus == Enum.GetName(FormStatus.Submitted) &&
                f.NextApprover == "ToManager@email")), Times.Once);
        }

        [Theory]
        [InlineData(null, 9)]
        [InlineData(9, 9)]
        public async Task Should_allow_attachment_when_coi_other(int? originalFormId, int expectedFormId)
        {
            var request = new FormInfoUpdate
            {
                FormAction = FormStatus.Attachment.ToString(),
                FormDetails = new FormDetailsRequest { AllFormsId = (int)FormType.CoI_Other, FormInfoId = originalFormId }
            };
            // Needs to work for new and existing forms
            var dbRecord = new FormInfo { FormInfoId = originalFormId ?? 0 };
            _mockFormInfoService.Setup(x => x.GetExistingOrNewFormInfoAsync(It.IsAny<FormInfoUpdate>(), It.Is<bool>(x => x)))
                .ReturnsAsync(dbRecord);

            var result = await _service.ProcessRequest(request);

            Assert.True(result.Success);
            // should remain unsubmitted
            _mockFormInfoService.Verify(
                x => x.SaveFormInfoAsync(It.Is<FormInfoUpdate>(f => f == request), It.Is<FormInfo>(d => d == dbRecord)),
                Times.Once);
        }

        public static IEnumerable<Object[]> GetInvalidRequests()
        {
            return new[]
            {
                new object[]
                {
                    new FormInfoUpdate
                    {
                        FormAction = nameof(FormStatus.Unsubmitted),
                        FormDetails = new FormDetailsRequest
                        {
                            FormInfoId = null,
                            Response = JsonConvert.SerializeObject(new CprEmployeeForm())
                        }
                    }
                },
                new object[]
                {
                    new FormInfoUpdate
                    {
                        FormAction = nameof(FormStatus.Unsubmitted),
                        FormDetails = new FormDetailsRequest
                        {
                            FormInfoId = 1,
                            Response = JsonConvert.SerializeObject(new CprEmployeeForm { From = DateTime.Now })
                        }
                    }
                },
                new object[]
                {
                    new FormInfoUpdate
                    {
                        FormAction = Enum.GetName(FormStatus.Approved),
                        FormDetails = new FormDetailsRequest
                        {
                            FormInfoId = 2,
                            Response = JsonConvert.SerializeObject(new CoiManagerForm())
                        }
                    }
                },
                new object[]
                {
                    new FormInfoUpdate
                    {
                        FormAction = Enum.GetName(FormStatus.Rejected),
                        FormDetails = new FormDetailsRequest
                        {
                            FormInfoId = 2,
                            Response = JsonConvert.SerializeObject(new CoiRejection())
                        }
                    }
                },
                new object[]
                {
                    new FormInfoUpdate
                    {
                        FormAction = Enum.GetName(FormStatus.Endorsed),
                        FormDetails = new FormDetailsRequest
                        {
                            FormInfoId = 3, Response = JsonConvert.SerializeObject(new CoiEndorsementForm
                            {
                                AdditionalComments = new string('a', 501)
                            })
                        }
                    }
                }
            };
        }

        private FormInfo GetTestStartingModels(bool hasManagerForm, int formStatusId, string ownerId = CoiTestEmployees.NormalEmployee)
        {
            var managerForm = hasManagerForm ? new CoiManagerForm
            {
                ConflictType = ConflictOfInterest.ConflictType.Actual,
                ActionsTaken = "Some action"
            } : null;
            var form = new CoIForm { ManagerForm = managerForm };
            return new FormInfo
            {
                FormInfoId = 3,
                AllFormsId = 3,
                FormStatusId = formStatusId,
                FormOwnerEmail = "normal.employee@email",
                Response = JsonConvert.SerializeObject(form, new StringEnumConverter(new CamelCaseNamingStrategy())),
                FormPermissions = new List<FormPermission> { new() { IsOwner = true, UserId = Guid.Parse(ownerId) } }
            };
        }
    }
}