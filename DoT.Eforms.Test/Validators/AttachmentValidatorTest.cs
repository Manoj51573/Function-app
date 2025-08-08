using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using eforms_middleware.Validators;
using FluentValidation.TestHelper;

namespace DoT.Eforms.Test.Validators;

public class AttachmentValidatorTest
{
    private readonly AttachmentValidator _validator;

    public AttachmentValidatorTest()
    {
        _validator = new AttachmentValidator();
    }
    
    [Fact]
    public void Validate_WhenFileToBig_ShouldReturnValidationError()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.SetupGet(x => x.FileName).Returns("test.png");
        mockFile.SetupGet(x => x.Length).Returns(10 * 1024 * 1024);
        
        var validationResult = _validator.TestValidate(mockFile.Object);
        
        validationResult.ShouldHaveValidationErrorFor(x => x.Length).WithErrorMessage("File must be less than 10MB.");
    }

    [Fact]
    public void Validate_WhenUnsupportedFileType_ShouldReturnValidationError()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.SetupGet(x => x.FileName).Returns("test.exe");
        mockFile.SetupGet(x => x.Length).Returns(9 * 1024 * 1024);
        
        var validationResult = _validator.TestValidate(mockFile.Object);
        
        validationResult.ShouldHaveValidationErrorFor(x => x.FileName).WithErrorMessage("Invalid filetype.");
    }

    [Fact]
    public void Validate_WhenFileIsValid_ShouldSucceed()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.SetupGet(x => x.FileName).Returns("test.png");
        mockFile.SetupGet(x => x.Length).Returns(9 * 1024 * 1024);

        var validationResult = _validator.TestValidate(mockFile.Object);

        Assert.True(validationResult.IsValid);
    }

    [Fact]
    public void Validate_WhenFilenameIsTooLong_ShouldReturnValidationError()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.SetupGet(x => x.FileName).Returns("some-stupidly-long-filename-that-a-user-should-really-not-think-is-appropriate-but-here-we-are-and-adding-a-test-to-make-sure-it-won't-happen-again.png");
        mockFile.SetupGet(x => x.Length).Returns(9 * 1024 * 1024);
        
        var validationResult = _validator.TestValidate(mockFile.Object);
        
        validationResult.ShouldHaveValidationErrorFor(x => x.FileName).WithErrorMessage("Filename must be less than 100 characters.");
    }
}