using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using FluentValidation;
using System;

namespace eforms_middleware.Validators;

public class CoiOtherGovOdgReviewFormValidator : AbstractValidator<CoiOtherGovOdgReview>
{
    public CoiOtherGovOdgReviewFormValidator()
    {
        When(x => x.CoiArea == ConflictOfInterest.CoiArea.Other, () =>
        {
            RuleFor(x => x.AreaInformation).NotEmpty()
                .WithMessage("Please enter more information about the area of the conflict");
            RuleFor(x => x.AreaInformation).MaximumLength(500)
                .WithMessage("You have exceeded the max length of 500 characters for Area Information");
        });
        RuleFor(x => x.AdditionalComments).MaximumLength(500)
            .WithMessage("You have exceeded the max length of 500 characters for Additional Information");
        RuleFor(x => x.ReminderDate).InclusiveBetween(DateTime.Today.AddDays(1), DateTime.Today.AddYears(1))
            .WithMessage("Reminder date cannot be before today or more than a year from today");
    }
}