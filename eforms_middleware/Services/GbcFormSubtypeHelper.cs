using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure.Interfaces;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace eforms_middleware.Services;

public class GbcFormSubtypeHelper : IFormSubtypeHelper<BoardCommitteePAndC>
{
    private readonly IValidator<GbcEmployeeForm> _requestorValidator;
    private readonly IValidator<CoiManagerForm> _managerValidator;
    private readonly IValidator<CoiEndorsementForm> _endorsementValidator;
    private readonly IValidator<CoiRejection> _rejectionValidator;
    private readonly IEmployeeService _employeeService;
    private readonly IPositionService _positionService;

    public GbcFormSubtypeHelper(IValidator<GbcEmployeeForm> requestorValidator, IValidator<CoiManagerForm> managerValidator,
        IValidator<CoiEndorsementForm> endorsementValidator, IValidator<CoiRejection> rejectionValidator, IEmployeeService employeeService,
        IPositionService positionService)
    {
        _requestorValidator = requestorValidator;
        _managerValidator = managerValidator;
        _endorsementValidator = endorsementValidator;
        _rejectionValidator = rejectionValidator;
        _employeeService = employeeService;
        _positionService = positionService;
    }
    
    public async Task<IApprovalInfo> GetFinalApprovers()
    {
        var position = await _positionService.GetPositionByIdAsync(ConflictOfInterest
            .ED_PEOPLE_AND_CULTURE_POSITION_ID);
        var nextApprover = position.AdfUserPositions.Count == 1
            ? position.AdfUserPositions.Single().EmployeeEmail
            : position.PositionTitle;
        var approvalInfo = new ApprovalInfo
        {
            PositionId = position.Id, NextApprover = nextApprover
        };

        return approvalInfo;
    }

    public string GetValidSerializedModel(FormInfo dbRecord, FormInfoUpdate request)
    {
        var original = dbRecord.Response != null
            ? JsonConvert.DeserializeObject<BoardCommitteePAndC>(dbRecord.Response)
            : new BoardCommitteePAndC();
        var rejectionReason = JsonConvert.DeserializeObject<CoiRejection>(request.FormDetails.Response)?.RejectionReason;
        switch (Enum.Parse<FormStatus>(request.FormAction))
        {
            case FormStatus.Unsubmitted:
                original.EmployeeForm = JsonConvert.DeserializeObject<GbcEmployeeForm>(request.FormDetails.Response);
                break;
            case FormStatus.Submitted:
                original.EmployeeForm = JsonConvert.DeserializeObject<GbcEmployeeForm>(request.FormDetails.Response);
                original.ManagerForm = null;
                original.EndorsementForm = null;
                original.FinalApprovalForm = null;
                break;
            case FormStatus.Rejected when dbRecord.FormStatusId == (int)FormStatus.Submitted:
                original.ManagerForm = new CoiManagerForm { RejectionReason = rejectionReason };
                break;
            case FormStatus.Approved:
                original.ManagerForm = JsonConvert.DeserializeObject<CoiManagerForm>(request.FormDetails.Response);
                break;
            case FormStatus.Rejected when dbRecord.FormStatusId == (int)FormStatus.Submitted:
                original.ManagerForm = new CoiManagerForm { RejectionReason = rejectionReason };
                break;
            case FormStatus.Rejected when dbRecord.FormStatusId == (int)FormStatus.Approved:
                original.EndorsementForm = new CoiEndorsementForm { RejectionReason = rejectionReason };
                break;
            case FormStatus.Rejected when dbRecord.FormStatusId == (int)FormStatus.Endorsed:
                original.FinalApprovalForm = new CoiEndorsementForm { RejectionReason = rejectionReason };
                break;
            case FormStatus.Endorsed:
                original.EndorsementForm = JsonConvert.DeserializeObject<CoiEndorsementForm>(request.FormDetails.Response);
                break;
            case FormStatus.Completed:
                original.FinalApprovalForm = JsonConvert.DeserializeObject<CoiEndorsementForm>(request.FormDetails.Response);
                break;
            default:
                throw new ArgumentOutOfRangeException(request.FormAction);
        }
        
        return JsonConvert.SerializeObject(original, new StringEnumConverter(new CamelCaseNamingStrategy()));
    }

    public Task<ValidationResult> ValidateInternal(FormInfoUpdate request)
    {
        var formAction = Enum.Parse<FormStatus>(request.FormAction);
        switch (formAction)
        {
            case FormStatus.Unsubmitted:
            case FormStatus.Submitted:
                var employeeRequest = JsonConvert.DeserializeObject<GbcEmployeeForm>(request.FormDetails.Response);
                var employeeContext = new ValidationContext<GbcEmployeeForm>(employeeRequest);
                employeeContext.RootContextData["FormAction"] = Enum.Parse(typeof(FormStatus), request.FormAction) as FormStatus?;
                var employeeValidationResult = _requestorValidator.Validate(employeeContext);
                return Task.FromResult(employeeValidationResult);
            case FormStatus.Recall:
                return Task.FromResult(new ValidationResult());
            case FormStatus.Rejected:
                var rejectionRequest = JsonConvert.DeserializeObject<CoiRejection>(request.FormDetails.Response);
                var rejectionValidationResult = _rejectionValidator.Validate(rejectionRequest);
                return Task.FromResult(rejectionValidationResult);
            case FormStatus.Approved:
                var managerRequest = JsonConvert.DeserializeObject<CoiManagerForm>(request.FormDetails.Response);
                var managerValidationResult = _managerValidator.Validate(managerRequest);
                return Task.FromResult(managerValidationResult);
            case FormStatus.Endorsed:
            case FormStatus.Completed:
                var endorsementRequest = JsonConvert.DeserializeObject<CoiEndorsementForm>(request.FormDetails.Response);
                var endorsementValidationResult = _endorsementValidator.Validate(endorsementRequest);
                return Task.FromResult(endorsementValidationResult);
            default:
                throw new ArgumentOutOfRangeException(request.FormAction);
        }
    }

    public DateTime? GetCompletionReminderDate(FormInfoUpdate request)
    {
        return null;
    }
}