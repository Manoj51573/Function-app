using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Validators;
using FluentValidation.TestHelper;
using System;
using Xunit;

namespace DoT.Eforms.Test.Validators;

public class CoiOtherGovOdgReviewFormValidatorTest
{
    private readonly CoiOtherGovOdgReviewFormValidator _validator;

    public CoiOtherGovOdgReviewFormValidatorTest()
    {
        _validator = new CoiOtherGovOdgReviewFormValidator();
    }

    [Theory]
    [MemberData(nameof(AreaInformationErrors))]
    public void When_CoiArea_is_other_and_AreaInformation_is_null_or_empty_is_invalid(string areaInformation, string expectedError)
    {
        CoiOtherGovOdgReview model = new() { CoiArea = ConflictOfInterest.CoiArea.Other, AreaInformation = areaInformation };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.AreaInformation)
            .WithErrorMessage(expectedError);
    }

    public static object[][] AreaInformationErrors => new[]
    {
        new object[] { null, "Please enter more information about the area of the conflict" },
        new object[] { "", "Please enter more information about the area of the conflict" },
        new object[] { " ", "Please enter more information about the area of the conflict" },
        new object[] { new string('a', 501), "You have exceeded the max length of 500 characters for Area Information" }
    };


    [Fact]
    public void When_AdditionalInformation_is_over_500_is_invalid()
    {
        CoiOtherGovOdgReview model = new() { AdditionalComments = new string('a', 501) };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.AdditionalComments)
            .WithErrorMessage("You have exceeded the max length of 500 characters for Additional Information");
    }


    [Theory]
    [MemberData(nameof(InvalidReminderDate))]
    public void When_ReminderDate_is_out_of_range_is_invalid(DateTime reminderDate, string expectedError)
    {
        CoiOtherGovOdgReview model = new() { ReminderDate = reminderDate };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ReminderDate).WithErrorMessage(expectedError);
    }

    public static object[][] InvalidReminderDate => new[]
    {
        new object[] { DateTime.Today, "Reminder date cannot be before today or more than a year from today" },
        new object[] { DateTime.Today.AddYears(1).AddDays(1), "Reminder date cannot be before today or more than a year from today" }
    };

    [Theory]
    [MemberData(nameof(ValidForms))]
    public void When_form_is_valid_has_no_errors(CoiOtherGovOdgReview model)
    {
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    public static object[][] ValidForms => new[]
    {
        new object[]
        {
            new CoiOtherGovOdgReview
            {
                NatureOfConflict = ConflictOfInterest.NatureOfConflict.Financial, CoiArea = ConflictOfInterest.CoiArea.Grants,
                ObjFileRef = "Ref file", ReminderDate = DateTime.Today.AddDays(1)
            }
        },
        new object[]
        {
            new CoiOtherGovOdgReview
            {
                NatureOfConflict = ConflictOfInterest.NatureOfConflict.Financial, CoiArea = ConflictOfInterest.CoiArea.Other,
                AreaInformation = "Some information", ObjFileRef = "Ref file", ReminderDate = DateTime.Today.AddDays(7)
            }
        }
    };
}