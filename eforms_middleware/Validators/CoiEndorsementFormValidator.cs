using eforms_middleware.DataModel;
using FluentValidation;

namespace eforms_middleware.Validators;

public class CoiEndorsementFormValidator : AbstractValidator<CoiEndorsementForm>
{
    public CoiEndorsementFormValidator()
    {
        RuleFor(x => x.AdditionalComments).MaximumLength(500)
            .WithMessage("You have exceeded the max length of 500 characters");
    }
}