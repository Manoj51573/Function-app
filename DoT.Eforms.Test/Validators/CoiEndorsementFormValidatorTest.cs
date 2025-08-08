using eforms_middleware.DataModel;
using eforms_middleware.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace DoT.Eforms.Test.Validators;

public class CoiEndorsementFormValidatorTest
{
    [Fact]
    public void When_AdditionalComments_is_over_500_it_should_return_an_error()
    {
        var validator = new CoiEndorsementFormValidator();
        var additionalComments = new string('a', 501);
        var model = new CoiEndorsementForm { AdditionalComments = additionalComments };

        var result = validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(x => x.AdditionalComments).WithErrorMessage("You have exceeded the max length of 500 characters");
    }
}