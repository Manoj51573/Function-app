using eforms_middleware.DataModel;
using eforms_middleware.Validators;
using FluentValidation.TestHelper;
using Xunit;
using static eforms_middleware.Constants.ConflictOfInterest.ConflictType;

namespace DoT.Eforms.Test.Validators;

public class CoiManagerFormValidatorTest
{
    private readonly CoiManagerFormValidator _validator;

    public CoiManagerFormValidatorTest()
    {
        _validator = new CoiManagerFormValidator();
    }

    [Fact]
    public void Should_have_no_errors_when_valid()
    {
        var model = new CoiManagerForm
            { ConflictType = Perceived, ActionsTaken = new string('a', 500), IsDeclarationAcknowledged = true };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Fact]
    public void Should_have_error_when_ConflictType_is_null()
    {
        var model = new CoiManagerForm();
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(cpr => cpr.ConflictType)
            .WithErrorMessage("Please enter type of conflict");
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("")]
    public void Should_have_error_when_ActionsTaken_is_required_and_null_or_empty(string proposedActions)
    {
        var model = new CoiManagerForm { ConflictType = Actual, ActionsTaken = proposedActions };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(cpr => cpr.ActionsTaken)
            .WithErrorMessage("Please describe the action/s that will be taken");
    }

    [Fact]
    public void Should_not_have_error_when_ActionsTaken_is_not_required()
    {
        var model = new CoiManagerForm { ConflictType = NoConflict, ActionsTaken = null, IsDeclarationAcknowledged = true };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_have_error_when_ActionsTaken_is_more_than_500_characters()
    {
        var tooLong = new string('a', 501);
        var model = new CoiManagerForm { ActionsTaken = tooLong };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(cpr => cpr.ActionsTaken)
            .WithErrorMessage("You have exceeded the maximum length of 500 characters");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(null)]
    public void Should_have_error_when_Declaration_is_not_acknowledged(bool? acknowledgement)
    {
        var model = new CoiManagerForm {IsDeclarationAcknowledged = acknowledgement};
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(cpr => cpr.IsDeclarationAcknowledged)
            .WithErrorMessage("Please ensure that you read and agree to the declarations");
    }
}