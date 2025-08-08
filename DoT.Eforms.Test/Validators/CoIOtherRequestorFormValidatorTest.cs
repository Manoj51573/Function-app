using System;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Validators;
using FluentValidation;
using Xunit;
using FluentValidation.TestHelper;

namespace DoT.Eforms.Test.Validators;

public class CoIOtherRequestorFormValidatorTest
{
    private readonly CoIOtherRequesterFormValidator _validator;

    public CoIOtherRequestorFormValidatorTest()
    {
        _validator = new CoIOtherRequesterFormValidator();
    }

    [Fact]
    public void When_ConflictType_is_missing_should_have_error()
    {
        var model = new CoIOtherRequesterForm();
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ConflictType)
            .WithErrorMessage("Please enter type of conflict");
    }

    [Fact]
    public void When_PeriodOfConflict_is_missing_should_have_error()
    {
        var model = new CoIOtherRequesterForm();
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.PeriodOfConflict)
            .WithErrorMessage("Please select the period of conflict");
    }

    [Fact]
    public void Should_have_error_when_From_date_is_null()
    {
        var model = new CoIOtherRequesterForm { PeriodOfConflict = ConflictOfInterest.EngagementFrequency.Ongoing};
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.From).WithErrorMessage("Please enter the start date");
    }
    
    [Fact]
    public void Should_have_error_when_ToDate_is_null_if_EngagementFrequency_is_OneOff()
    {
        var model = new CoIOtherRequesterForm { PeriodOfConflict = ConflictOfInterest.EngagementFrequency.OneOff, From = DateTime.Today};
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.To).WithErrorMessage("Please enter the end date");
    }

    [Fact]
    public void Should_have_error_when_ToDate_is_out_of_range()
    {
        var oneYearAndADay = DateTime.Today.AddYears(1).AddDays(1);
        var model = new CoIOtherRequesterForm
        {
            PeriodOfConflict = ConflictOfInterest.EngagementFrequency.OneOff, From = DateTime.Today,
            To = oneYearAndADay
        };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x).WithErrorMessage("Date to cannot exceed 12 months of date from");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void Should_have_error_when_ToDate_is_less_or_equal_to_FromDate(int dayOffset)
    {
        var yesterday = DateTime.Today.AddDays(dayOffset);
        var model = new CoIOtherRequesterForm
        {
            PeriodOfConflict = ConflictOfInterest.EngagementFrequency.OneOff, From = DateTime.Today,
            To = yesterday
        };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x).WithErrorMessage("Date to cannot be before or the same date as Date from");
    }

    [Theory]
    [MemberData(nameof(DeclarationDetailsErrorParams))]
    public void Should_have_error_when_DeclarationDetails_is_null_or_empty(string declarationDetails, string expectedMessage)
    {
        var model = new CoIOtherRequesterForm
        {
            DeclarationDetails = declarationDetails
        };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.DeclarationDetails).WithErrorMessage(expectedMessage);
    }
    
    public static object[][] DeclarationDetailsErrorParams => new[]
    {
        new object[] { null, "Please enter details for your declaration" },
        new object[] { "", "Please enter details for your declaration" },
        new object[] { new string('a', 501), "You have exceeded the max length of 500 characters" }
    };

    [Theory]
    [MemberData(nameof(ProposedPlanErrorParams))]
    public void Should_have_error_when_ProposedPlan_is_invalid(string inputValue, string expectedMessage)
    {
        var conditionsWithError = new[] { ConflictOfInterest.ConflictType.Actual, ConflictOfInterest.ConflictType.Perceived, ConflictOfInterest.ConflictType.Potential };
        foreach (var condition in conditionsWithError)
        {
            var model = new CoIOtherRequesterForm
            {
                ProposedPlan = inputValue, ConflictType = condition
            };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.ProposedPlan).WithErrorMessage(expectedMessage);
        }
    }
    
    public static object[][] ProposedPlanErrorParams => new[]
    {
        new object[] { null, "Please enter your proposed plan to manage the conflict" },
        new object[] { "", "Please enter your proposed plan to manage the conflict" },
        new object[] { new string('a', 501), "You have exceeded the max length of 500 characters" }
    };

    [Fact]
    public void Should_have_error_when_AdditionalInformation_is_over_500_characters()
    {
        var model = new CoIOtherRequesterForm
        {
            AdditionalInformation = new string('a', 501)
        };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.AdditionalInformation).WithErrorMessage("You have exceeded the max length of 500 characters");
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    public void Should_have_error_when_DeclarationAcknowledged_is_invalid(bool? declaration)
    {
        var model = new CoIOtherRequesterForm
        {
            ConflictType = ConflictOfInterest.ConflictType.NoConflict, 
            PeriodOfConflict = ConflictOfInterest.EngagementFrequency.Ongoing,
            From = DateTime.Today,
            DeclarationDetails = "Something",
            IsDeclarationAcknowledged = declaration
        };
        var context = new ValidationContext<CoIOtherRequesterForm>(model);
        context.RootContextData["FormAction"] = FormStatus.Submitted;
        var result = _validator.Validate(context);
        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("You must acknowledge the declaration to submit", error.ErrorMessage);
    }

    [Theory]
    [InlineData(ConflictOfInterest.EngagementFrequency.Ongoing, false, false)]
    [InlineData(ConflictOfInterest.EngagementFrequency.OneOff, true, false)]
    [InlineData(ConflictOfInterest.EngagementFrequency.Ongoing, false, true)]
    public void When_Valid_should_have_no_errors(ConflictOfInterest.EngagementFrequency frequency, bool hasTo, bool needsPlan)
    {
        var model = new CoIOtherRequesterForm
        {
            ConflictType = needsPlan ? ConflictOfInterest.ConflictType.Actual : ConflictOfInterest.ConflictType.NoConflict, 
            PeriodOfConflict = frequency,
            From = DateTime.Today,
            To = hasTo ? DateTime.Today.AddDays(2) : null,
            DeclarationDetails = "Something",
            ProposedPlan = needsPlan ? "something" : null
        };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
}