using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Specifications;
using Microsoft.AspNetCore.Http;

namespace eforms_middleware.Services;

public class AttachmentRecordService : IAttachmentRecordService
{
    private readonly IRepository<FormPermission> _repository;
    private readonly IRepository<FormAttachment> _attachmentRepo;
    private readonly IRequestingUserProvider _requestingUserProvider;

    public AttachmentRecordService(IRepository<FormPermission> repository, IRepository<FormAttachment> attachmentRepo, IRequestingUserProvider requestingUserProvider)
    {
        _repository = repository;
        _attachmentRepo = attachmentRepo;
        _requestingUserProvider = requestingUserProvider;
    }

    public async Task<List<AttachmentRecordFile>> CreateOrUpdateAttachmentRecordAsync(int formId,
        IFormFileCollection files)
    {
        var requestingUser = await _requestingUserProvider.GetRequestingUser();
        var activePermission =
            await _repository.SingleOrDefaultAsync(new FormPermissionSpecification(formId,
                (byte)PermissionFlag.UserActionable, addAttachments: true));
        var existingAttachments = activePermission.FormAttachments.Where(a => files.Any(f => a.FileName == f.FileName))
            .ToList();
        var newFiles = files.Where(x => activePermission.FormAttachments.All(a => a.FileName != x.FileName)).Select(x =>
            new FormAttachment
            {
                FileName = x.FileName, FileType = Path.GetExtension(x.FileName), CreatedBy = requestingUser.EmployeeEmail,
                ActiveRecord = false, FormId = formId, PermissionId = activePermission.Id, Created = DateTime.Now,
                Modified = DateTime.Now, ModifiedBy = requestingUser.EmployeeEmail
            });

        var createdFiles = new List<FormAttachment>();
        foreach (var file in newFiles)
        {
            var result = await _attachmentRepo.AddAsync(file);
            createdFiles.Add(result);
        }
        
        foreach (var existingAttachment in existingAttachments.Where(existingAttachment => existingAttachment.ActiveRecord))
        {
            existingAttachment.ActiveRecord = false;
            existingAttachment.Modified = DateTime.Now;
            existingAttachment.ModifiedBy = requestingUser.EmployeeEmail;
            _attachmentRepo.Update(existingAttachment);
        }

        return createdFiles.Union(existingAttachments).Select(x => new AttachmentRecordFile
        {
            FormPermissionId = x.PermissionId, Id = x.Id, FormInfoId = x.FormId, File = files.GetFile(x.FileName)
        }).ToList();
    }

    public async Task RemoveAttachmentAsync(int formId, Guid attachmentId)
    {
        var permission = await _repository.SingleOrDefaultAsync(new FormPermissionSpecification(formId,
            (byte)PermissionFlag.UserActionable, addAttachments: true));
        var attachment = permission.FormAttachments.SingleOrDefault(x => x.Id == attachmentId);
        if (attachment?.ActiveRecord ?? false)
        {
            attachment.ActiveRecord = false;
            _attachmentRepo.Update(attachment);
        }
    }

    public async Task<bool> AttachmentExistsAsync(Guid id)
    {
        var specification = new AttachmentSpecification(id);
        var attachment = await _attachmentRepo.FirstOrDefaultAsync(specification);
        return attachment != null;
    }

    public async Task ActivateAttachmentRecordsAsync(int formId, IList<AttachmentResult> attachments)
    {
        var activePermission =
            await _repository.SingleOrDefaultAsync(new FormPermissionSpecification(formId,
                (byte)PermissionFlag.UserActionable, addAttachments: true));
        if (activePermission.FormAttachments.Any())
        {
            var updatedAttachments = activePermission.FormAttachments.Where(x => attachments.Any(a => a.Id == x.Id));
            foreach (var attachment in updatedAttachments)
            {
                attachment.ActiveRecord = true;
                _attachmentRepo.Update(attachment);
            }
        }
    }

    public async Task DeactivateAllAttachmentsAsync(int formInfoId)
    {
        var activePermission =
            await _repository.SingleOrDefaultAsync(new FormPermissionSpecification(formInfoId,
                (byte)PermissionFlag.UserActionable, addAttachments: true));
        if (activePermission.FormAttachments.Any())
        {
            var updatedAttachments = activePermission.FormAttachments.Where(x => x.ActiveRecord);
            foreach (var attachment in updatedAttachments)
            {
                attachment.ActiveRecord = false;
                _attachmentRepo.Update(attachment);
            }
        }
    }
}