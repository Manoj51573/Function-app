using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DoT.Infrastructure.DbModels;
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

public class CoIOtherSubtypeHelper : IFormSubtypeHelper<CoIOther>
{
    private readonly IValidator<CoIOtherRequesterForm> _employeeFormValidator;
    private readonly IValidator<CoiRejection> _rejectionValidator;
    private readonly IValidator<CoiManagerForm> _managerValidator;
    private readonly IValidator<CoiEndorsementForm> _endorserValidator;
    private readonly IValidator<CoiOtherGovOdgReview> _odgReviewValidator;

    public CoIOtherSubtypeHelper(IValidator<CoIOtherRequesterForm> employeeFormValidator, IValidator<CoiRejection> rejectionValidator,
        IValidator<CoiManagerForm> managerValidator, IValidator<CoiEndorsementForm> endorserValidator, IValidator<CoiOtherGovOdgReview> odgReviewValidator)
    {
        _employeeFormValidator = employeeFormValidator;
        _rejectionValidator = rejectionValidator;
        _managerValidator = managerValidator;
        _endorserValidator = endorserValidator;
        _odgReviewValidator = odgReviewValidator;
    }

    public Task<IApprovalInfo> GetFinalApprovers()
    {
        var approvalInfo = new ApprovalInfo
        {
            NextApprover = ConflictOfInterest.ODG_AUDIT_GROUP_EMAIL, GroupId = ConflictOfInterest.ODG_AUDIT_GROUP_ID
        };
        return Task.FromResult((IApprovalInfo)approvalInfo);
    }

    public string GetValidSerializedModel(FormInfo dbForm, FormInfoUpdate request)
    {
        var original = dbForm.Response != null
            ? JsonConvert.DeserializeObject<CoIOther>(dbForm.Response)
            : new CoIOther();
        var formAction = Enum.Parse<FormStatus>(request.FormAction);
        var rejectionReason = formAction == FormStatus.Attachment
            ? ""
            : JsonConvert.DeserializeObject<CoiRejection>(request.FormDetails.Response)?.RejectionReason;
        switch (formAction)
        {
            case FormStatus.Attachment:
                break;
            case FormStatus.Unsubmitted:
                original.EmployeeForm = JsonConvert.DeserializeObject<CoIOtherRequesterForm>(request.FormDetails.Response);
                break;
            case FormStatus.Submitted:
                original.EmployeeForm = JsonConvert.DeserializeObject<CoIOtherRequesterForm>(request.FormDetails.Response);
                original.ManagerForm = null;
                original.EndorsementForm = null;
                original.FinalApprovalForm = null;
                break;
            case FormStatus.Rejected when dbForm.FormStatusId == (int)FormStatus.Submitted:
                original.ManagerForm = new CoiManagerForm { RejectionReason = rejectionReason };
                break;
            case FormStatus.Approved:
                original.ManagerForm = JsonConvert.DeserializeObject<CoiManagerForm>(request.FormDetails.Response);
                break;
            case FormStatus.Rejected when dbForm.FormStatusId == (int)FormStatus.Submitted:
                original.ManagerForm = new CoiManagerForm { RejectionReason = rejectionReason };
                break;
            case FormStatus.Rejected when dbForm.FormStatusId == (int)FormStatus.Approved:
                original.EndorsementForm = new CoiEndorsementForm { RejectionReason = rejectionReason };
                break;
            case FormStatus.Rejected when dbForm.FormStatusId is (int)FormStatus.Endorsed or (int)FormStatus.IndependentReview or (int)FormStatus.Delegated:
                original.FinalApprovalForm = new CoiOtherGovOdgReview { RejectionReason = rejectionReason };
                break;
            case FormStatus.Endorsed:
                original.EndorsementForm = JsonConvert.DeserializeObject<CoiEndorsementForm>(request.FormDetails.Response);
                break;
            case FormStatus.IndependentReviewCompleted:
                original.EndorsementForm = JsonConvert.DeserializeObject<CoiEndorsementForm>(request.FormDetails.Response);
                break;
            case FormStatus.Completed:
                original.FinalApprovalForm = JsonConvert.DeserializeObject<CoiOtherGovOdgReview>(request.FormDetails.Response);
                break;
            default:
                throw new ArgumentOutOfRangeException(request.FormAction);
        }
        return JsonConvert.SerializeObject(original, new StringEnumConverter(new CamelCaseNamingStrategy()));
    }

    public Task<ValidationResult> ValidateInternal(FormInfoUpdate request)
    {
        ValidationResult result;
        var action = Enum.Parse<FormStatus>(request.FormAction);
        switch (action)
        {
            case FormStatus.Unsubmitted:
            case FormStatus.Submitted:
                var requesterForm = JsonConvert.DeserializeObject<CoIOtherRequesterForm>(request.FormDetails.Response);
                var employeeContext = new ValidationContext<CoIOtherRequesterForm>(requesterForm);
                employeeContext.RootContextData["FormAction"] = Enum.Parse(typeof(FormStatus), request.FormAction) as FormStatus?;
                result = _employeeFormValidator.Validate(employeeContext);
                break;
            case FormStatus.Delegated:
            case FormStatus.IndependentReview:
                if (!Guid.TryParse(request.FormDetails.NextApprover, out Guid guidResult))
                {
                    var failures = new List<ValidationFailure>
                        { new ("NextApprover", "Selected approver ID is not valid") };
                    result = new ValidationResult(failures);
                }
                else
                {
                    result = new ValidationResult();
                }
                break;
            case FormStatus.Attachment:
            case FormStatus.Recall:
                result = new ValidationResult();
                break;
            case FormStatus.Rejected:
                var rejectionRequest = JsonConvert.DeserializeObject<CoiRejection>(request.FormDetails.Response);
                result = _rejectionValidator.Validate(rejectionRequest);
                break;
            case FormStatus.Approved:
                result = new ValidationResult();
                break;
            case FormStatus.IndependentReviewCompleted:
            case FormStatus.Endorsed:
                var endorserRequest = JsonConvert.DeserializeObject<CoiEndorsementForm>(request.FormDetails.Response);
                result = _endorserValidator.Validate(endorserRequest);
                break;
            case FormStatus.Completed:
                var endorsementRequest = JsonConvert.DeserializeObject<CoiOtherGovOdgReview>(request.FormDetails.Response);
                result = _odgReviewValidator.Validate(endorsementRequest);
                break;
            default:
                throw new ArgumentOutOfRangeException(request.FormAction);
        }

        return Task.FromResult(result);
    }

    public DateTime? GetCompletionReminderDate(FormInfoUpdate request)
    {
        var form = JsonConvert.DeserializeObject<CoiOtherGovOdgReview>(request.FormDetails.Response);
        return form.ReminderDate;
    }
}