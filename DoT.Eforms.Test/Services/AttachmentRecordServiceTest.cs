using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Services;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace DoT.Eforms.Test;

public class AttachmentRecordServiceTest
{
    private Mock<IRepository<FormPermission>> _repository;
    private readonly AttachmentRecordService _service;
    private Mock<IRepository<FormAttachment>> _attachmentRepo;
    private Mock<IRequestingUserProvider> _requestingUserProvider;

    public AttachmentRecordServiceTest()
    {
        _repository = new Mock<IRepository<FormPermission>>();
        _attachmentRepo = new Mock<IRepository<FormAttachment>>();
        _requestingUserProvider = new Mock<IRequestingUserProvider>();
        _service = new AttachmentRecordService(_repository.Object, _attachmentRepo.Object, _requestingUserProvider.Object);
        _requestingUserProvider.Setup(x => x.GetRequestingUser()).ReturnsAsync(new EmployeeDetailsDto
        {
            EmployeeEmail = "test@email"
        });
    }
    
    [Theory]
    [MemberData(nameof(GetData), 0, 4)]
    public async Task CreateOrUpdateAttachmentRecordAsync_CreatesAndOrUpdates(
        IFormFileCollection collection, bool existingActive, int expectedCount, Times updateTimes, Times addTimes)
    {
        _repository.Setup(x => x.SingleOrDefaultAsync(It.IsAny<ISpecification<FormPermission>>())).ReturnsAsync(
            new FormPermission
            { 
                FormAttachments = new List<FormAttachment>
                    { new () { FileName = "existing.docx", ActiveRecord = existingActive } }
            });
        _attachmentRepo.Setup(x => x.AddAsync(It.IsAny<FormAttachment>())).ReturnsAsync((FormAttachment attachment) => attachment);

        var result = await _service.CreateOrUpdateAttachmentRecordAsync(1, collection);
        
        Assert.Equal(expectedCount, result.Count);
        _attachmentRepo.Verify(x => x.Update(It.IsAny<FormAttachment>()), updateTimes);
        _attachmentRepo.Verify(x => x.AddAsync(It.IsAny<FormAttachment>()), addTimes);
    }

    [Fact]
    public async Task CreateOrUpdateAttachmentRecordAsync_WhenAnExceptionIsThrown_ThrowsWithoutAlteration()
    {
        _repository.Setup(x => x.SingleOrDefaultAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ThrowsAsync(new ConnectionAbortedException());
        
        await Assert.ThrowsAsync<ConnectionAbortedException>(() => _service.CreateOrUpdateAttachmentRecordAsync(1, new FormFileCollection()));
    }

    [Theory]
    [MemberData(nameof(GetData), 4, 3)]
    public async Task RemoveAttachment_DeactivatesRequested(IList<FormAttachment> attachments, Times expectedCalls)
    {
        var attachmentId = Guid.Parse("247197AC-A206-4BAF-A670-DF636E7E6975");
        _repository.Setup(x => x.SingleOrDefaultAsync(It.IsAny<ISpecification<FormPermission>>())).ReturnsAsync(
            new FormPermission
            { 
                FormAttachments = new List<FormAttachment>
                    { new () { FileName = "existing.docx", ActiveRecord = true, Id = attachmentId} }
            });
        
        await _service.RemoveAttachmentAsync(5, attachmentId);
        
        _attachmentRepo.Verify(
            x => x.Update(It.Is<FormAttachment>(x => x.ActiveRecord == false && x.Id == attachmentId)), Times.Once);
    }

    [Theory]
    [MemberData(nameof(GetData), 7, 2)]
    public async Task AttachmentExistsAsync_ReturnsExpected(FormAttachment attachment, bool exists)
    {
        _attachmentRepo.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<FormAttachment>>()))
            .ReturnsAsync(attachment);

        var result = await _service.AttachmentExistsAsync(Guid.NewGuid());
        
        Assert.Equal(exists, result);
    }

    [Theory]
    [MemberData(nameof(GetData), 9, 4)]
    public async Task ActivateAttachmentRecordsAsync_ActivatesTheExpectedAttachments(FormPermission permission, List<AttachmentResult> attachments)
    {
        // Attachment exists
        _repository.Setup(x => x.SingleOrDefaultAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(permission);
        // Attachment is passed
        await _service.ActivateAttachmentRecordsAsync(3, attachments);
        // Attachment belongs to permission
        foreach (var attachment in attachments.Where(a => permission.FormAttachments.Any(f => a.Id == f.Id)))
        {
            _attachmentRepo.Verify(x => x.Update(It.Is<FormAttachment>(a => a.ActiveRecord && a.Id == attachment.Id)), Times.Once);
        }
        foreach (var attachment in permission.FormAttachments.Where(f => attachments.All(a => a.Id != f.Id)))
        {
            _attachmentRepo.Verify(x => x.Update(It.Is<FormAttachment>(a => a.Id == attachment.Id)), Times.Never);
        }
    }

    [Fact]
    public async Task DeactivateAllAttachmentsAsync_WhenUserCannotAction_DoesNothing()
    {
        _repository.Setup(x => x.SingleOrDefaultAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(new FormPermission{PermissionFlag = (int)PermissionFlag.View});
        await _service.DeactivateAllAttachmentsAsync(3);
        _attachmentRepo.Verify(x => x.Update(It.IsAny<FormAttachment>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateAllAttachmentsAsync_Succeeds()
    {
        // Attachment exists
        _repository.Setup(x => x.SingleOrDefaultAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(new FormPermission{PermissionFlag = (int)PermissionFlag.UserActionable, FormAttachments = new List<FormAttachment>
            {
                new () {ActiveRecord = true}, new () {ActiveRecord = false}
            }});
        // Attachment is passed
        await _service.DeactivateAllAttachmentsAsync(3);
        // Attachment belongs to permission
        _attachmentRepo.Verify(x => x.Update(It.Is<FormAttachment>(attachment => !attachment.ActiveRecord)), Times.Once);
    }

    [Fact]
    public async Task DeactivateAllAttachmentsAsync_WhenNoAttachments_DoesNothing()
    {
        _repository.Setup(x => x.SingleOrDefaultAsync(It.IsAny<ISpecification<FormPermission>>()))
            .ReturnsAsync(new FormPermission
            {
                PermissionFlag = (int)PermissionFlag.UserActionable, FormAttachments = new List<FormAttachment>()
            });
        await _service.DeactivateAllAttachmentsAsync(3);
        _attachmentRepo.Verify(x => x.Update(It.IsAny<FormAttachment>()), Times.Never);
    }

    public static IEnumerable<object[]> GetData(int skip, int take)
    {
        var attachmentId = Guid.Parse("247197AC-A206-4BAF-A670-DF636E7E6975");
        var file = new Mock<IFormFile>();
        file.SetupGet(x => x.FileName).Returns("filename.pdf");
        file.SetupGet(x => x.Length).Returns(9 * 1024 * 1024);
        var file2 = new Mock<IFormFile>();
        file2.SetupGet(x => x.FileName).Returns("existing.docx");
        file2.SetupGet(x => x.Length).Returns(8 * 1024 * 1024);
        var allData = new List<object[]>
        {
            new object[] { new FormFileCollection { file.Object }, true, 1, Times.Never(), Times.Once() },
            new object[] { new FormFileCollection { file.Object, file2.Object}, true, 2, Times.Once(), Times.Once() },
            new object[] { new FormFileCollection { file.Object, file2.Object}, false, 2, Times.Never(), Times.Once() },
            new object[] { new FormFileCollection { file2.Object}, true, 1, Times.Once(), Times.Never() },
            new object[] { new List<FormAttachment> { new () { FileName = "existing.docx", ActiveRecord = true, Id = attachmentId} }, Times.Once() },
            new object[] { new List<FormAttachment> { new () { FileName = "existing.docx", ActiveRecord = false, Id = attachmentId} }, Times.Never() },
            new object[] { new List<FormAttachment>(), Times.Never() },
            new object[] { new FormAttachment(), true },
            new object[] { null, false },
            new object[] { new FormPermission {FormAttachments = new List<FormAttachment>
            {
                new () {Id = Guid.Parse("88E156F7-62D4-4733-87AD-53346FF3ABDE"), ActiveRecord = false},
                new () {Id = Guid.Parse("07E14A64-4D9F-4BC1-8AF1-D5AF3E59BABE"), ActiveRecord = false}
            }}, new List<AttachmentResult>
            {
                new () {Id = Guid.Parse("88E156F7-62D4-4733-87AD-53346FF3ABDE")},
                new () {Id = Guid.Parse("07E14A64-4D9F-4BC1-8AF1-D5AF3E59BABE")}
            }},
            new object[] { new FormPermission {FormAttachments = new List<FormAttachment>
            {
                new () {Id = Guid.Parse("88E156F7-62D4-4733-87AD-53346FF3ABDE"), ActiveRecord = true},
                new () {Id = Guid.Parse("07E14A64-4D9F-4BC1-8AF1-D5AF3E59BABE"), ActiveRecord = false}
            }}, new List<AttachmentResult>
            {
                new () {Id = Guid.Parse("88E156F7-62D4-4733-87AD-53346FF3ABDE")},
                new () {Id = Guid.Parse("07E14A64-4D9F-4BC1-8AF1-D5AF3E59BABE")}
            }},
            new object[] { new FormPermission {FormAttachments = new List<FormAttachment>
            {
                new () {Id = Guid.Parse("88E156F7-62D4-4733-87AD-53346FF3ABDE"), ActiveRecord = false},
                new () {Id = Guid.Parse("07E14A64-4D9F-4BC1-8AF1-D5AF3E59BABE"), ActiveRecord = false}
            }}, new List<AttachmentResult>
            {
                new () {Id = Guid.Parse("07E14A64-4D9F-4BC1-8AF1-D5AF3E59BABE")}
            }},
            new object[] { new FormPermission {FormAttachments = new List<FormAttachment>
            {
                new () {Id = Guid.Parse("07E14A64-4D9F-4BC1-8AF1-D5AF3E59BABE"), ActiveRecord = false}
            }}, new List<AttachmentResult>
            {
                new () {Id = Guid.Parse("88E156F7-62D4-4733-87AD-53346FF3ABDE")},
                new () {Id = Guid.Parse("07E14A64-4D9F-4BC1-8AF1-D5AF3E59BABE")}
            }}
        };
        return allData.Skip(skip).Take(take);
    }
}