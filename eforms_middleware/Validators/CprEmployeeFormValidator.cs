using System.Linq;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using FluentValidation;

namespace eforms_middleware.Validators;

public class CprEmployeeFormValidator : AbstractValidator<CprEmployeeForm>
{
    public CprEmployeeFormValidator()
    {
        RuleFor(cpr => cpr.From).NotNull().WithMessage("Please enter date from");
        RuleFor(cpr => cpr.ConflictType).NotNull().WithMessage("Please enter type of conflict");
        RuleFor(cpr => cpr.Description).NotEmpty().WithMessage("Please enter description of conflict");
        RuleFor(cpr => cpr.Description).MaximumLength(500).WithMessage("Description of conflict cannot be more than 500 characters");
        RuleFor(cpr => cpr.ProposedPlan).MaximumLength(500).WithMessage("Proposed plan cannot be more than 500 characters");
        RuleFor(cpr => cpr.AdditionalComments).MaximumLength(500).WithMessage("Additional comments cannot be more than 500 characters");
        RuleFor(cpr => cpr.IsDeclarationAcknowledged).Custom((isAcknowledged, context) =>
        {
            var action = context.RootContextData.SingleOrDefault(c => c.Key == "FormAction").Value as FormStatus?;

            if (action is FormStatus.Submitted && isAcknowledged is not true)
            {
                context.AddFailure("Please check the declaration box");
            }
        });
    }
}