using System.Collections.Generic;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Validators;
using FluentValidation;
using Xunit;

namespace DoT.Eforms.Test.Validators;

public class E29FormValidatorTest
{
    private readonly E29FormValidator _validator;

    public E29FormValidatorTest()
    {
        _validator = new E29FormValidator();
    }

    [Fact]
    public void Should_have_error_when_previous_month_User_is_removed()
    {
        var model = new E29Form();
        var context = new ValidationContext<E29Form>(model);
        context.RootContextData["FormHistory"] = new E29Form { Users = PreviousMonth() };
        var result = _validator.Validate(context);
        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Users from the previous month cannot be removed.", error.ErrorMessage);
    }
    
    [Fact]
    public void Should_have_error_when_previous_month_User_has_status_of_added_in_error()
    {
        var model = new E29Form { Users = new []{new TeamMember { Email = "should still exist", RemovedReason = (int)TrelisRemovalReason.AddedInError}}};
        var context = new ValidationContext<E29Form>(model);
        context.RootContextData["FormHistory"] = new E29Form { Users = PreviousMonth() };
        var result = _validator.Validate(context);
        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Users from the previous month cannot have removed reason of added in error.", error.ErrorMessage);
    }

    private static List<TeamMember> PreviousMonth()
    {
        return new List<TeamMember>
        {
            new TeamMember { Email = "should still exist", RemovedReason = (int)TrelisRemovalReason.OnLeave }
        };
    }
}