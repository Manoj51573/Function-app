using System;
using System.Linq;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Specifications;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace eforms_middleware.MasterData;

// TODO This must be removed from the source code once it has done it's task or at least disabled
// this will need to be done via the portal and we should check we are able to do it ourselves before the time.
public class MigrateBlobsTimer
{
    private readonly IMigrationService _migrationService;
    private readonly IBlobService _blobService;
    private readonly IRepository<FormInfo> _repository;
    private readonly ILogger<MigrateBlobsTimer> _logger;

    public MigrateBlobsTimer(IMigrationService migrationService, IBlobService blobService, IRepository<FormInfo> repository, ILogger<MigrateBlobsTimer> logger)
    {
        _migrationService = migrationService;
        _blobService = blobService;
        _repository = repository;
        _logger = logger;
    }
    
    [FunctionName("migrate-blobs-timer")]
    public async Task RunAsync([TimerTrigger("0 0 0 1 1 *")] TimerInfo myTimer)
    {
        _logger.LogInformation("migrate-blobs-timer function executed at: {Now}", DateTime.Now);
        try
        {
            // Find blob file ids
            var blobIds = _blobService.GetBlobIds();
            // Find CoI forms without attachment records
            var specification = new AttachmentsWithoutRecordsSpecification(blobIds.Keys);
            var actionItems = await _repository.ListAsync(specification);
            _logger.LogInformation("Found {FormCount} forms without attachment records", actionItems.Count);
            var formBody = actionItems.Where(x => blobIds.Any(b => x.FormInfoId == b.Key)).Select(x => new
            {
                FormId = x.FormInfoId, OwnerPermissionId = x.FormPermissions.FirstOrDefault(p => p.IsOwner),
                ResponseModel = JsonConvert.DeserializeObject<COIMain>(x.Response)
            }).Where(x => x.ResponseModel.Attachments != null && x.ResponseModel.Attachments.Any()).ToList();
            _logger.LogInformation("Limiting action to {MigrationItems} forms that need migrating", formBody.Count);
            // Have 2 collections. 1 with the blob name and form Id
            // another with the form Id and response model
            var result = blobIds.Join(formBody, b => new { Key = b.Key }, f => new { Key = f.FormId },
                (b, f) => new MigrationModel
                {
                    FormId = b.Key,
                    PermissionId = f.OwnerPermissionId.Id,
                    Attachments = b.Value,
                    Response = f.ResponseModel
                }).ToList();
            _logger.LogInformation("Prepared information for migration service");
            // Work through each item create db entry, clone blob item with guid as name, delete old entry
            await _migrationService.MigrateBlobsAsync(result);
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Error thrown in {nameof(MigrateBlobsTimer)}");
        }
        _logger.LogInformation("migrate-blobs-timer function finished at: {Now}", DateTime.Now);
    }
}