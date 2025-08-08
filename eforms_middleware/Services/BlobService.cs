using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using eforms_middleware.DataModel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using eforms_middleware.Interfaces;
using Microsoft.Extensions.Options;

namespace eforms_middleware.Services
{
    public class BlobService : IBlobService
    {
        private readonly ILogger<BlobService> _log;
        private readonly BlobOptions _blobOptions;

        public BlobService(ILogger<BlobService> log, IOptions<BlobOptions> blobOptions)
        {
            _log = log;
            _blobOptions = blobOptions.Value;
        }

        public async Task UploadAttachmentsAsync(IEnumerable<AttachmentRecordFile> attachments)
        {
            var blobContainerClient = new BlobContainerClient(_blobOptions.ConnectionString, _blobOptions.ContainerName);
            foreach (var attachment in attachments)
            {
                var fileName = $"{attachment.FormInfoId}/{attachment.Id}";
                var containerClient = blobContainerClient.GetBlobClient(fileName);
                var blobHttpHeader = new BlobHttpHeaders
                {
                    ContentType = attachment.File.ContentType
                };
                await containerClient.UploadAsync(attachment.File.OpenReadStream(), blobHttpHeader);
            }
        }

        public async Task<BlobRequestResult> DownloadDocumentAsync(int formId, Guid filename)
        {
            var blobContainerClient = new BlobContainerClient(_blobOptions.ConnectionString, _blobOptions.ContainerName);
            var containerClient = blobContainerClient.GetBlobClient($"{formId}/{filename}");
            var blobResponse = await containerClient.DownloadContentAsync();
            return BlobRequestResult.Succeeded(blobResponse.Value);
        }
        
        public Dictionary<int, List<string>> GetBlobIds()
        {
            _log.LogInformation("Getting a list of all blobs");
            var blobContainerClient = new BlobContainerClient(_blobOptions.ConnectionString, _blobOptions.ContainerName);
            var blobs = blobContainerClient.GetBlobs(BlobTraits.Metadata, BlobStates.All);

            var blobInformation = blobs.Select(x =>
            {
                var name = x.Name;
                var split = name.Split('/');
                return new { FormId = int.Parse(split[0]), AttachmentName = split[1] };
            }).GroupBy(x => x.FormId).ToDictionary(x => x.Key, x => x.Select(y => y.AttachmentName).ToList());
            _log.LogInformation("Returning {BlobCount} of blobs", blobInformation.Count);
            return blobInformation;
        }
    }
}
