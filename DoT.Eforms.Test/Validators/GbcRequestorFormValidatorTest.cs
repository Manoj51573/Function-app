using System;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Validators;
using FluentValidation;
using FluentValidation.TestHelper;
using Xunit;

namespace DoT.Eforms.Test.Validators;

public class GbcRequestorFormValidatorTest
{
    private readonly GbcRequestorFormValidator _validator;

    public GbcRequestorFormValidatorTest()
    {
        _validator = new GbcRequestorFormValidator();
    }

    [Fact]
    public void Should_have_error_when_EngagementFrequency_is_null()
    {
        var model = new GbcEmployeeForm();
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.EngagementFrequency)
            .WithErrorMessage("Please specify board/committee engagement frequency");
    }

    [Fact]
    public void Should_have_error_when_FromDate_is_null()
    {
        var model = new GbcEmployeeForm { EngagementFrequency = ConflictOfInterest.EngagementFrequency.Ongoing };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.From).WithErrorMessage("Please enter date from");
    }

    [Fact]
    public void Should_have_error_when_ToDate_is_null_if_EngagementFrequency_is_Ongoing()
    {
        var model = new GbcEmployeeForm { EngagementFrequency = ConflictOfInterest.EngagementFrequency.OneOff, From = DateTime.Today};
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.To).WithErrorMessage("Please enter date to");
    }

    [Fact]
    public void Should_have_error_when_ToDate_is_out_of_range()
    {
        var oneYearAndADay = DateTime.Today.AddYears(1).AddDays(1);
        var model = new GbcEmployeeForm
        {
            EngagementFrequency = ConflictOfInterest.EngagementFrequency.OneOff, From = DateTime.Today,
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
        var model = new GbcEmployeeForm
        {
            EngagementFrequency = ConflictOfInterest.EngagementFrequency.OneOff, From = DateTime.Today,
            To = yesterday
        };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x).WithErrorMessage("Date to cannot be before or the same date as Date from");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Should_have_error_when_GovernmentBoardCommitteeName_is_null_or_empty(string text)
    {
        var model = new GbcEmployeeForm
        {
            EngagementFrequency = ConflictOfInterest.EngagementFrequency.Ongoing, From = DateTime.Today,
            GovernmentBoardCommitteeName = text
        };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.GovernmentBoardCommitteeName)
            .WithErrorMessage("Please enter government board/committee name");
    }

    [Fact]
    public void Should_have_error_when_PaidOrVoluntary_is_null()
    {
        var model = new GbcEmployeeForm { PaidOrVoluntary = null };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.PaidOrVoluntary)
            .WithErrorMessage("Please specify if this engagement is paid or voluntary");
    }

    [Fact]
    public void Should_have_error_when_ConflictType_is_null()
    {
        var model = new GbcEmployeeForm();
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ConflictType)
            .WithErrorMessage("Please enter type of conflict");
    }

    [Theory]
    [InlineData(null, "Please enter description of conflict")]
    [InlineData(501, "You have exceeded the max length of 500 characters")]
    public void Should_have_error_when_DescriptionOfConflict_is_invalid(int? stringLength, string expectedError)
    {
        var providedString = stringLength.HasValue ? new String('a', stringLength.Value) : null;
        var model = new GbcEmployeeForm { DescriptionOfConflict = providedString };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.DescriptionOfConflict)
            .WithErrorMessage(expectedError);
    }
    
    [Theory]
    [InlineData(null, "Please enter reasons to support your assessment")]
    [InlineData(501, "You have exceeded the max length of 500 characters")]
    public void Should_have_error_when_ReasonsToSupport_is_invalid(int? stringLength, string expectedError)
    {
        var providedString = stringLength.HasValue ? new String('a', stringLength.Value) : null;
        var model = new GbcEmployeeForm { ReasonsToSupport = providedString };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ReasonsToSupport)
            .WithErrorMessage(expectedError);
    }

    [Fact]
    public void Should_have_error_when_ProposedPlan_is_longer_than_500_chars()
    {
        var providedString = new String('a', 501);
        var model = new GbcEmployeeForm { ProposedPlan = providedString };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ProposedPlan)
            .WithErrorMessage("You have exceeded the max length of 500 characters");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    public void Should_have_error_when_DeclarationAcknowledged_is_invalid(bool? declaration)
    {
        var model = new GbcEmployeeForm
        {
            From = DateTime.Today, EngagementFrequency = ConflictOfInterest.EngagementFrequency.Ongoing,
            ConflictType = ConflictOfInterest.ConflictType.Actual, DescriptionOfConflict = "test",
            PaidOrVoluntary = ConflictOfInterest.EngagementRenumeration.Paid, ReasonsToSupport = "test",
            GovernmentBoardCommitteeName = "name", IsDeclarationAcknowledged = declaration
        };
        var context = new ValidationContext<GbcEmployeeForm>(model);
        context.RootContextData["FormAction"] = FormStatus.Submitted;
        var result = _validator.Validate(context);
        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("You must acknowledge the declaration to submit", error.ErrorMessage);
    }
    
    [Fact]
    public void Should_have_error_when_AdditionalComments_is_longer_than_500_chars()
    {
        var providedString = new String('a', 501);
        var model = new GbcEmployeeForm { AdditionalComments = providedString };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.AdditionalComments)
            .WithErrorMessage("You have exceeded the max length of 500 characters");
    }

    [Fact]
    public void Should_have_no_errors_when_valid()
    {
        var model = new GbcEmployeeForm
        {
            From = DateTime.Today, EngagementFrequency = ConflictOfInterest.EngagementFrequency.Ongoing,
            ConflictType = ConflictOfInterest.ConflictType.Actual, DescriptionOfConflict = "test",
            PaidOrVoluntary = ConflictOfInterest.EngagementRenumeration.Paid, ReasonsToSupport = "test",
            GovernmentBoardCommitteeName = "name", IsDeclarationAcknowledged = true
        };
        var context = new ValidationContext<GbcEmployeeForm>(model);
        context.RootContextData["FormAction"] = FormStatus.Submitted;
        var result = _validator.Validate(context);
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}