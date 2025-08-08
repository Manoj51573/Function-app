using System.Linq;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using FluentValidation;

namespace eforms_middleware.Validators;

public class CoIOtherRequesterFormValidator : AbstractValidator<CoIOtherRequesterForm>
{
    public CoIOtherRequesterFormValidator()
    {
        RuleFor(x => x.ConflictType).NotNull().WithMessage("Please enter type of conflict");
        RuleFor(x => x.PeriodOfConflict).NotNull().WithMessage("Please select the period of conflict");
        RuleFor(x => x.From).NotNull().WithMessage("Please enter the start date");
        When(x => x.PeriodOfConflict == ConflictOfInterest.EngagementFrequency.OneOff, () =>
        {
            RuleFor(x => x.To).NotNull().WithMessage("Please enter the end date");
            RuleFor(x => x).Must(x =>
                {
                    if (!x.From.HasValue || !x.To.HasValue) return true;
                    var toMax = x.From.Value.AddYears(1);
                    return x.To <= toMax;
                })
                .WithMessage("Date to cannot exceed 12 months of date from");
            RuleFor(x => x).Must(x => !x.To.HasValue || !x.From.HasValue || x.From.Value < x.To.Value)
                .WithMessage("Date to cannot be before or the same date as Date from");
        });
        RuleFor(x => x.DeclarationDetails).NotEmpty().WithMessage("Please enter details for your declaration");
        RuleFor(x => x.DeclarationDetails).MaximumLength(500)
            .WithMessage("You have exceeded the max length of 500 characters");
        When(
            x => x.ConflictType is ConflictOfInterest.ConflictType.Actual or ConflictOfInterest.ConflictType.Perceived
                or ConflictOfInterest.ConflictType.Potential,
            () =>
            {
                RuleFor(x => x.ProposedPlan).NotEmpty().WithMessage("Please enter your proposed plan to manage the conflict");
                RuleFor(x => x.ProposedPlan).MaximumLength(500)
                    .WithMessage("You have exceeded the max length of 500 characters");
            });
        RuleFor(x => x.AdditionalInformation).MaximumLength(500)
            .WithMessage("You have exceeded the max length of 500 characters");
        RuleFor(cpr => cpr.IsDeclarationAcknowledged).Custom((isAcknowledged, context) =>
        {
            var action = context.RootContextData.SingleOrDefault(c => c.Key == "FormAction").Value as FormStatus?;

            if (action is FormStatus.Submitted && isAcknowledged is not true)
            {
                context.AddFailure("You must acknowledge the declaration to submit");
            }
        });
    }
}