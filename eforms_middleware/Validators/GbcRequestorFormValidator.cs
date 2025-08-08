using System.Linq;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using FluentValidation;

namespace eforms_middleware.Validators;

public class GbcRequestorFormValidator : AbstractValidator<GbcEmployeeForm>
{
    public GbcRequestorFormValidator()
    {
        RuleFor(x => x.EngagementFrequency).NotNull()
            .WithMessage("Please specify board/committee engagement frequency");
        RuleFor(x => x.From).NotNull().WithMessage("Please enter date from");
        When(x => x.EngagementFrequency == ConflictOfInterest.EngagementFrequency.OneOff, () =>
        {
            RuleFor(x => x.To).NotNull().WithMessage("Please enter date to");
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
        RuleFor(x => x.GovernmentBoardCommitteeName).NotEmpty()
            .WithMessage("Please enter government board/committee name");
        RuleFor(x => x.PaidOrVoluntary).NotNull().WithMessage("Please specify if this engagement is paid or voluntary");
        RuleFor(x => x.ConflictType).NotNull().WithMessage("Please enter type of conflict");
        RuleFor(x => x.DescriptionOfConflict).NotEmpty().WithMessage("Please enter description of conflict");
        RuleFor(x => x.DescriptionOfConflict).MaximumLength(500)
            .WithMessage("You have exceeded the max length of 500 characters");
        RuleFor(x => x.ReasonsToSupport).NotEmpty().WithMessage("Please enter reasons to support your assessment");
        RuleFor(x => x.ReasonsToSupport).MaximumLength(500)
            .WithMessage("You have exceeded the max length of 500 characters");
        RuleFor(x => x.ProposedPlan).MaximumLength(500)
            .WithMessage("You have exceeded the max length of 500 characters");
        RuleFor(cpr => cpr.IsDeclarationAcknowledged).Custom((isAcknowledged, context) =>
        {
            var action = context.RootContextData.SingleOrDefault(c => c.Key == "FormAction").Value as FormStatus?;

            if (action is FormStatus.Submitted && isAcknowledged is not true)
            {
                context.AddFailure("You must acknowledge the declaration to submit");
            }
        });
        RuleFor(x => x.AdditionalComments).MaximumLength(500)
            .WithMessage("You have exceeded the max length of 500 characters");
    }
}