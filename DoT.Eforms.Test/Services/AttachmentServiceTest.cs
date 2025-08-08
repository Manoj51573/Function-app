using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs.Models;
using DoT.Eforms.Test.Shared;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Services;
using eforms_middleware.Validators;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DoT.Eforms.Test;

public class AttachmentServiceTest
{
    private readonly Mock<IBlobService> _blobService;
    private readonly AttachmentService _service;
    private readonly Mock<IFormInfoService> _formInfoService;
    private readonly Mock<ILogger<AttachmentService>> _logger;
    private readonly Mock<IAttachmentRecordService> _attachmentRecordService;

    public AttachmentServiceTest()
    {
        _blobService = new Mock<IBlobService>();
        _formInfoService = new Mock<IFormInfoService>();
        var attachmentValidator = new AttachmentValidator();
        _logger = new Mock<ILogger<AttachmentService>>();
        _attachmentRecordService = new Mock<IAttachmentRecordService>();
        _service = new AttachmentService(_attachmentRecordService.Object, _formInfoService.Object, _blobService.Object, attachmentValidator, _logger.Object);
        _formInfoService.Setup(x => x.GetFormByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new FormDetails { CanAction = true });
    }

    [Theory]
    [MemberData(nameof(GetData), 0, 2)]
    public async Task AddAttachmentAsync_WhenCantAction_ReturnsError(FormDetails formDetails)
    {
        _formInfoService.Setup(x => x.GetFormByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(formDetails);
        
        var result = await _service.AddAttachmentsAsync(5, null);
        
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.Forbidden, result.StatusCode);
        var failureArray = Assert.IsAssignableFrom<Array>(result.Value);
        var failureList = (FailureItem[])failureArray;
        var expectedFailure =  Assert.IsType<FailureItem>(failureList.First());
        Assert.Equal("Forbidden - User does not have access to the requested attachment.", expectedFailure.ErrorMessage);
        _blobService.Verify(x => x.UploadAttachmentsAsync(It.IsAny<List<AttachmentRecordFile>>()), Times.Never);
        _attachmentRecordService.Verify(
            x => x.CreateOrUpdateAttachmentRecordAsync(It.IsAny<int>(), It.IsAny<IFormFileCollection>()), Times.Never);
    }

    [Fact]
    public async Task AddAttachmentsAsync_WhenInvalidFiles_ReturnsError()
    {
        var result = await _service.AddAttachmentsAsync(5, null);
        
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.UnprocessableEntity, result.StatusCode);
        var failureArray = Assert.IsAssignableFrom<Array>(result.Value);
        var failureList = (FailureItem[])failureArray;
        var expectedFailure =  Assert.IsType<FailureItem>(failureList.First());
        Assert.Equal("Attachment Error - No attachments are supplied.", expectedFailure.ErrorMessage);
        _blobService.Verify(x => x.UploadAttachmentsAsync(It.IsAny<List<AttachmentRecordFile>>()), Times.Never);
    }

    [Theory]
    [MemberData(nameof(GetData), 2, 3)]
    public async Task AddAttachmentsAsync_WhenFileValidationError_ReturnsError(string filename, long sizeInMegaBytes, string expectedMessage)
    {
        var file = new Mock<IFormFile>();
        file.SetupGet(x => x.FileName).Returns(filename);
        file.SetupGet(x => x.Length).Returns(sizeInMegaBytes);
        var formFileCollection = new FormFileCollection { file.Object };

        var result = await _service.AddAttachmentsAsync(5, formFileCollection);
        
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.UnprocessableEntity, result.StatusCode);
        var failureArray = Assert.IsAssignableFrom<Array>(result.Value);
        var failureList = (FailureItem[])failureArray;
        var failure =  Assert.IsType<FailureItem>(failureList.First());
        Assert.Equal(expectedMessage, failure.ErrorMessage);
        _blobService.Verify(x => x.UploadAttachmentsAsync(It.IsAny<List<AttachmentRecordFile>>()), Times.Never);
    }

    [Fact]
    public async Task AddAttachmentsAsync_WhenDbUpdateFails_ReturnsError()
    {
        var file = new Mock<IFormFile>();
        file.SetupGet(x => x.FileName).Returns("filename.pdf");
        file.SetupGet(x => x.Length).Returns(9 * 1024 * 1024);
        var formFileCollection = new FormFileCollection { file.Object };
        _attachmentRecordService
            .Setup(x => x.CreateOrUpdateAttachmentRecordAsync(It.IsAny<int>(), It.IsAny<IFormFileCollection>()))
            .ThrowsAsync(new ConnectionAbortedException());

        var result = await _service.AddAttachmentsAsync(5, formFileCollection);
        
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.InternalServerError, result.StatusCode);
        var failureArray = Assert.IsAssignableFrom<Array>(result.Value);
        var failureList = (FailureItem[])failureArray;
        var failure =  Assert.IsType<FailureItem>(failureList.First());
        Assert.Equal("An unexpected error occurred. Please try again later.", failure.ErrorMessage);
        _blobService.Verify(x => x.UploadAttachmentsAsync(It.IsAny<List<AttachmentRecordFile>>()), Times.Never);
        _logger.VerifyLogging(null, LogLevel.Error, Times.Once(), true);
    }

    [Fact]
    public async Task AddAttachmentAsync_WhenBlobUploadFails_ReturnsError()
    {
        var expectedGuid = Guid.Parse("5819BB96-6E8A-479B-9F83-7AC0C1A0F2C0");
        var file = new Mock<IFormFile>();
        file.SetupGet(x => x.FileName).Returns("filename.pdf");
        file.SetupGet(x => x.Length).Returns(9 * 1024 * 1024);
        var formFileCollection = new FormFileCollection { file.Object };
        _attachmentRecordService
            .Setup(x => x.CreateOrUpdateAttachmentRecordAsync(It.IsAny<int>(), It.IsAny<IFormFileCollection>()))
            .ReturnsAsync(new List<AttachmentRecordFile>
            {
                new () { Id = expectedGuid, FormInfoId = 5, FormPermissionId = 7 }
            });
        _blobService.Setup(x => x.UploadAttachmentsAsync(It.IsAny<IEnumerable<AttachmentRecordFile>>()))
            .ThrowsAsync(new FileLoadException());

        var result = await _service.AddAttachmentsAsync(5, formFileCollection);
        
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.InternalServerError, result.StatusCode);
        var failureArray = Assert.IsAssignableFrom<Array>(result.Value);
        var failureList = (FailureItem[])failureArray;
        var failure =  Assert.IsType<FailureItem>(failureList.First());
        Assert.Equal("An unexpected error occurred. Please try again later.", failure.ErrorMessage);
        _logger.VerifyLogging(null, LogLevel.Error, Times.Once(), true);
    }

    [Fact]
    public async Task AddAttachmentAsync_ShouldAddANewAttachment()
    {
        var expectedGuid = Guid.Parse("5819BB96-6E8A-479B-9F83-7AC0C1A0F2C0");
        var file = new Mock<IFormFile>();
        file.SetupGet(x => x.FileName).Returns("filename.pdf");
        file.SetupGet(x => x.Length).Returns(9 * 1024 * 1024);
        var formFileCollection = new FormFileCollection { file.Object };
        _attachmentRecordService
            .Setup(x => x.CreateOrUpdateAttachmentRecordAsync(It.IsAny<int>(), It.IsAny<IFormFileCollection>())).ReturnsAsync(new List<AttachmentRecordFile>
        {
            new () { Id = expectedGuid, FormInfoId = 5, FormPermissionId = 7 }
        });

        var result = await _service.AddAttachmentsAsync(5, formFileCollection);
        
        Assert.True(result.Success);
        Assert.Equal((int)HttpStatusCode.OK, result.StatusCode);
        _attachmentRecordService.Verify(x => x.CreateOrUpdateAttachmentRecordAsync(It.Is<int>(i => i == 5), It.IsAny<IFormFileCollection>()), Times.Once);
        _blobService.Verify(x => x.UploadAttachmentsAsync(It.IsAny<IEnumerable<AttachmentRecordFile>>()), Times.Once);
    }

    [Theory]
    [MemberData(nameof(GetData), 0, 2)]
    public async Task RemoveAttachmentAsync_WhenCantAction_ReturnsError(FormDetails formDetails)
    {
        _formInfoService.Setup(x => x.GetFormByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(formDetails);
        
        var result = await _service.RemoveAttachmentAsync(5, Guid.NewGuid());
        
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.Forbidden, result.StatusCode);
        var failureArray = Assert.IsAssignableFrom<Array>(result.Value);
        var failureList = (FailureItem[])failureArray;
        var expectedFailure =  Assert.IsType<FailureItem>(failureList.First());
        Assert.Equal("Forbidden - User does not have access to the requested form to alter attachments.", expectedFailure.ErrorMessage);
        _attachmentRecordService.Verify(x => x.RemoveAttachmentAsync(It.IsAny<int>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task RemoveAttachmentAsync_ReturnsSuccess()
    {
        var result = await _service.RemoveAttachmentAsync(5, Guid.NewGuid());
        
        Assert.True(result.Success);
        Assert.Equal((int)HttpStatusCode.NoContent, result.StatusCode);
        _attachmentRecordService.Verify(x => x.RemoveAttachmentAsync(It.IsAny<int>(),It.IsAny<Guid>()), Times.Once);
    }

    [Theory]
    [MemberData(nameof(GetData), 1, 1)]
    public async Task ViewAttachmentAsync_WhenCantAction_ReturnsError(FormDetails formInfo)
    {
        _formInfoService.Setup(x => x.GetFormByIdAsync(It.IsAny<int>())).ReturnsAsync(formInfo);

        var result = await _service.ViewAttachmentAsync(5, Guid.NewGuid());
        
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.Forbidden, result.StatusCode);
        Assert.Equal("Forbidden - User does not have access to the requested Form.", result.MoreInfo);
        _blobService.Verify(x => x.DownloadDocumentAsync(It.IsAny<int>(), It.IsAny<Guid>()), Times.Never);
        _attachmentRecordService.Verify(x => x.AttachmentExistsAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ViewAttachmentAsync_WhenNoAttachment_ReturnsNotFound()
    {
        _formInfoService.Setup(x => x.GetFormByIdAsync(It.IsAny<int>())).ReturnsAsync(new FormDetails());
        _attachmentRecordService.Setup(x => x.AttachmentExistsAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var result = await _service.ViewAttachmentAsync(5, Guid.NewGuid());
        
        Assert.False(result.Success);
        Assert.Equal((int)HttpStatusCode.NotFound, result.StatusCode);
        _blobService.Verify(x => x.DownloadDocumentAsync(It.IsAny<int>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ViewAttachmentAsync_WhenBlobThrowsError_ReturnsError()
    {
        _formInfoService.Setup(x => x.GetFormByIdAsync(It.IsAny<int>())).ReturnsAsync(new FormDetails());
        _attachmentRecordService.Setup(x => x.AttachmentExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);
        _blobService.Setup(x => x.DownloadDocumentAsync(It.IsAny<int>(), It.IsAny<Guid>()))
            .ThrowsAsync(new RequestFailedException("Fail"));

        await Assert.ThrowsAsync<RequestFailedException>(() => _service.ViewAttachmentAsync(5, Guid.NewGuid()));

        _logger.VerifyLogging(times: Times.Never(), isAnyString: true);
        _blobService.Verify(x => x.DownloadDocumentAsync(It.IsAny<int>(), It.IsAny<Guid>()), Times.Once);
    }

    [Fact]
    public async Task ViewAttachmentAsync_Succeeds()
    {
        _formInfoService.Setup(x => x.GetFormByIdAsync(It.IsAny<int>())).ReturnsAsync(new FormDetails());
        _attachmentRecordService.Setup(x => x.AttachmentExistsAsync(It.IsAny<Guid>())).ReturnsAsync(true);
        _blobService.Setup(x => x.DownloadDocumentAsync(It.IsAny<int>(), It.IsAny<Guid>()))
            .ReturnsAsync(BlobRequestResult.Succeeded(null));
        
        var result = await _service.ViewAttachmentAsync(5, Guid.NewGuid());
        
        Assert.True(result.Success);
    }

    public static IEnumerable<object[]> GetData(int skip, int take)
    {
        var allData = new List<object[]>
        {
            new object[] { new FormDetails { CanAction = false } },
            new object[] { null },
            new object[] {"filename.png", 10 * 1024 * 1024, "filename.png Invalid: File must be less than 10MB."},
            new object[] {"filename.exe", 9 * 1024 * 1024, "filename.exe Invalid: Invalid filetype."},
            new object[] {"filename.exe", 10 * 1024 * 1024, "filename.exe Invalid: File must be less than 10MB. Invalid filetype."}
        };
        return allData.Skip(skip).Take(take);
    }
}