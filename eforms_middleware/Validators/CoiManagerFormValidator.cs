using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using FluentValidation;

namespace eforms_middleware.Validators;

public class CoiManagerFormValidator : AbstractValidator<CoiManagerForm>
{
    public CoiManagerFormValidator()
    {
        RuleFor(x => x.ConflictType).NotNull().WithMessage("Please enter type of conflict");
        When(x => x.ConflictType != ConflictOfInterest.ConflictType.NoConflict, () =>
        {
            RuleFor(x => x.ActionsTaken).NotEmpty().WithMessage("Please describe the action/s that will be taken");
        });
        RuleFor(x => x.ActionsTaken).MaximumLength(500)
            .WithMessage("You have exceeded the maximum length of 500 characters");
        RuleFor(x => x.IsDeclarationAcknowledged).Must(x => x.HasValue && x.Value)
            .WithMessage("Please ensure that you read and agree to the declarations");
        RuleFor(x => x.RejectionReason).Null()
            .WithMessage("You cannot add a rejection reason when approving the form.");
    }
}