using eforms_middleware.DataModel;
using eforms_middleware.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace DoT.Eforms.Test.Validators;

public class CoiRejectionFormValidatorTest
{
    private readonly CoiRejectionValidator _validator;

    public CoiRejectionFormValidatorTest()
    {
        _validator = new CoiRejectionValidator();
    }
    
    [Fact]
    public void When_Rejection_reason_is_invalid_informs_why()
    {
        var model = new CoiRejection();
        var result = _validator.TestValidate(model);
        
        result.ShouldHaveValidationErrorFor(x => x.RejectionReason).WithErrorMessage("Please enter reason for rejection");
    }

    [Fact]
    public void When_Valid_has_no_errors()
    {
        var model = new CoiRejection { RejectionReason = "Valid rejection reason" };
        var result = _validator.TestValidate(model);
        
        result.ShouldNotHaveAnyValidationErrors();
    }
}