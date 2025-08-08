using System.Linq;
using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace eforms_middleware.Validators;

public class AttachmentValidator : AbstractValidator<IFormFile>
{
    public AttachmentValidator()
    {
        RuleFor(c => c.Length).LessThan(10 * 1024 * 1024).WithMessage("File must be less than 10MB.");
        RuleFor(c => c.FileName).MaximumLength(100).WithMessage("Filename must be less than 100 characters.");
        RuleFor(c => c.FileName).Must(filename =>
        {
            var rx = new Regex(@"(?i)(?:\.pdf+$|\.png+$|\.jpg+$|\.svg+$|\.docx+$|\.xlsx+$)");
            var matches = rx.Matches(filename);
            return matches.Any();
        }).WithMessage("Invalid filetype.");
    }
}