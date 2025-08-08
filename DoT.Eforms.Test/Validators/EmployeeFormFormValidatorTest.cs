using System;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Validators;
using FluentValidation;
using FluentValidation.TestHelper;
using Xunit;

namespace DoT.Eforms.Test.Validators;
public class EmployeeFormFormValidatorTest
{
    private readonly CprEmployeeFormValidator _validator;

    public EmployeeFormFormValidatorTest()
    {
        _validator = new CprEmployeeFormValidator();
    }

    [Fact]
    public void Should_have_error_when_From_is_null()
    {
        var model = new CprEmployeeForm { From = null, ConflictType = ConflictOfInterest.ConflictType.Actual};
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(cpr => cpr.From)
            .WithErrorMessage("Please enter date from");
    }
    
    [Fact]
    public void Should_have_error_when_ConflictType_is_null()
    {
        var model = new CprEmployeeForm { From = new DateTime() };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(cpr => cpr.ConflictType)
            .WithErrorMessage("Please enter type of conflict");
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("")]
    public void Should_have_error_when_Description_is_null_or_empty(string description)
    {
        var model = TestModel(new DateTime(), ConflictOfInterest.ConflictType.Actual, description, "Plan");
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(cpr => cpr.Description)
            .WithErrorMessage("Please enter description of conflict");
    }

    [Fact]
    public void Should_have_error_when_Description_is_over_500_characters()
    {
        var description = new string('a', 501);
        var model = TestModel(new DateTime(), ConflictOfInterest.ConflictType.Actual, description, "Plan");
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(cpr => cpr.Description)
            .WithErrorMessage("Description of conflict cannot be more than 500 characters");
    }
    
    [Fact]
    public void Should_have_error_when_ProposedPlan_is_over_500_characters()
    {
        var description = new string('a', 501);
        var model = TestModel(new DateTime(), ConflictOfInterest.ConflictType.Actual, "something", description);
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(cpr => cpr.ProposedPlan)
            .WithErrorMessage("Proposed plan cannot be more than 500 characters");
    }

    [Fact]
    public void Should_have_error_when_AdditionalComments_is_over_500_characters()
    {
        var description = new string('a', 501);
        var model = TestModel(new DateTime(), ConflictOfInterest.ConflictType.Actual, "something", null, null, description);
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(cpr => cpr.AdditionalComments)
            .WithErrorMessage("Additional comments cannot be more than 500 characters");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    public void Should_have_error_when_Declaration_is_null_during_submission(bool? acknowledged)
    {
        var model = TestModel(new DateTime(), ConflictOfInterest.ConflictType.Actual, "Description", "Plan", acknowledged);
        var context = new ValidationContext<CprEmployeeForm>(model);
        context.RootContextData["FormAction"] = FormStatus.Submitted;
        var result = _validator.Validate(context);
        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Please check the declaration box", error.ErrorMessage);
    }

    private static CprEmployeeForm TestModel(DateTime? from = null,
        ConflictOfInterest.ConflictType? conflict = null, string description = null, string proposedPlan = null, 
        bool? acknowledgeDeclaration = null, string additionalComments = null)
    {
        return new CprEmployeeForm
        {
            From = from,
            ConflictType = conflict,
            Description = description,
            ProposedPlan = proposedPlan,
            IsDeclarationAcknowledged = acknowledgeDeclaration,
            AdditionalComments = additionalComments
        };
    }
}