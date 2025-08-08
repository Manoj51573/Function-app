using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace eforms_middleware.Services;

public class MigrationService : IMigrationService
{
    private readonly IRepository<FormAttachment> _attachmentsRepo;
    private readonly BlobOptions _blobOptions;
    private readonly ILogger<MigrationService> _logger;
    private readonly IRepository<FormInfo> _formRepo;

    public MigrationService(IRepository<FormAttachment> attachmentsRepo, IOptions<BlobOptions> blobOptions, ILogger<MigrationService> logger, IRepository<FormInfo> formRepo)
    {
        _attachmentsRepo = attachmentsRepo;
        _blobOptions = blobOptions.Value;
        _logger = logger;
        _formRepo = formRepo;
    }
    
    public async Task MigrateBlobsAsync(List<MigrationModel> models)
    {
        var blobContainerClient = new BlobContainerClient(_blobOptions.ConnectionString, _blobOptions.ContainerName);
        foreach (var model in models)
        {
            _logger.LogInformation("Updating the attachments for Form: {FormId}", model.FormId);
            var attachmentRecords = new List<AttachmentResult>();
            foreach (var blobItem in model.Attachments)
            {
                try
                {
                    var active = model.Response.Attachments.Any(a => a.Name == blobItem);
                    var filetype = blobItem.Split('.')[1];
                    _logger.LogInformation("Updating Document {BlobName} as activeState: {Active}",blobItem, active);
                    var attachment = await _attachmentsRepo.AddAsync(new FormAttachment
                    {
                        FormId = model.FormId, PermissionId = model.PermissionId, Created = DateTime.Now, CreatedBy = "System", Modified = DateTime.Now,
                        FileName = blobItem, ModifiedBy = "System", ActiveRecord = active, FileType = filetype
                    });
                    var fileName = $"{model.FormId}/{attachment.Id}";
                    var sourceBlob = blobContainerClient.GetBlobClient($"{model.FormId}/{blobItem}");
                    var originalDoc = await sourceBlob.DownloadAsync();
                    var destination = blobContainerClient.GetBlobClient(fileName);
                    var blobHttpHeader = new BlobHttpHeaders
                    {
                        ContentType = originalDoc.Value.ContentType
                    };
                    await destination.UploadAsync(originalDoc.Value.Content, blobHttpHeader);
                    await sourceBlob.DeleteAsync();
                    if (attachment.ActiveRecord)
                    {
                        attachmentRecords.Add(new AttachmentResult { Id = attachment.Id, Name = attachment.FileName});
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error thrown in {Name} for Form: {FormId} Blob: {BlobName}", nameof(MigrateBlobsAsync), model.FormId, blobItem);
                }
            }
            
            if (!attachmentRecords.Any()) continue;
            _logger.LogInformation("Updating the Form Response for: {FormId}", model.FormId);
            var form = await _formRepo.FirstOrDefaultAsync(x => x.FormInfoId == model.FormId);
            var newResponse = HandleResponse(form, attachmentRecords);
            form.Response = newResponse;
            _formRepo.Update(form);
        }
    }

    private static string HandleResponse(FormInfo form, IList<AttachmentResult> attachmentRecords)
    {
        var response = form.Response;
        var formType = (FormType)form.AllFormsId;
        switch (formType)
        {
            case FormType.CoI_REC:
                var recModel = JsonConvert.DeserializeObject<RecruitmentModel>(response);
                recModel.Attachments = attachmentRecords;
                response = JsonConvert.SerializeObject(recModel);
                break;
            case FormType.CoI_SE:
                var seModel = JsonConvert.DeserializeObject<SecondaryEmploymentModel>(response);
                seModel.Attachments = attachmentRecords;
                response = JsonConvert.SerializeObject(seModel);
                break;
            case FormType.CoI_GBH:
                var gbhModel = JsonConvert.DeserializeObject<GBHModel>(response);
                gbhModel.Attachments = attachmentRecords;
                response = JsonConvert.SerializeObject(gbhModel);
                break;
        }

        return response;
    }
}