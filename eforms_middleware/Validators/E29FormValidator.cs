using System.Linq;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using FluentValidation;

namespace eforms_middleware.Validators;

public class E29FormValidator : AbstractValidator<E29Form>
{
    public E29FormValidator()
    {
        RuleFor(e29 => e29.Users).Custom((users, context) =>
        {
            var historicForm = context.RootContextData.SingleOrDefault(c => c.Key == "FormHistory").Value as E29Form;

            if (historicForm.Users.Any())
            {
                if (historicForm.Users.Any(o => users.All(r =>
                        r.Email != o.Email &&
                        (o.RemovedReason == (int)TrelisRemovalReason.OnLeave || o.RemovedReason == null))))
                {
                    context.AddFailure("Users from the previous month cannot be removed.");
                }
                
                if (historicForm.Users.Any(o => users.Any(r =>
                        r.RemovedReason == (int)TrelisRemovalReason.AddedInError && r.Email == o.Email &&
                        (o.RemovedReason == (int)TrelisRemovalReason.OnLeave || o.RemovedReason == null))))
                {
                    context.AddFailure("Users from the previous month cannot have removed reason of added in error.");
                }
            }
        });
    }
}