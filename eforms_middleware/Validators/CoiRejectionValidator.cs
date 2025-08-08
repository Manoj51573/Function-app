using eforms_middleware.DataModel;
using FluentValidation;

namespace eforms_middleware.Validators;

public class CoiRejectionValidator : AbstractValidator<CoiRejection>
{
    public CoiRejectionValidator()
    {
        RuleFor(x => x.RejectionReason).NotEmpty().WithMessage("Please enter reason for rejection");
    }
}