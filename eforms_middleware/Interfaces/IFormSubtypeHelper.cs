using System;
using System.Threading.Tasks;
using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure.Interfaces;
using eforms_middleware.DataModel;
using FluentValidation.Results;

namespace eforms_middleware.Interfaces;

public interface IFormSubtypeHelper<T>
{
    Task<IApprovalInfo> GetFinalApprovers();
    string GetValidSerializedModel(FormInfo dbForm, FormInfoUpdate request);
    Task<ValidationResult> ValidateInternal(FormInfoUpdate request);
    DateTime? GetCompletionReminderDate(FormInfoUpdate request);
}