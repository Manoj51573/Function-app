using System.Net;
using Azure.Storage.Blobs.Models;

namespace eforms_middleware.DataModel;

public class BlobRequestResult
{
    public bool Success { get; private set; }
    public int StatusCode { get; private set; }
    public string MoreInfo { get; private set; }
    public BlobDownloadResult Value { get; private set; }

    private BlobRequestResult(bool succeeded, int statusCode, string moreInfo, BlobDownloadResult blobResult)
    {
        Success = succeeded;
        StatusCode = statusCode;
        MoreInfo = moreInfo;
        Value = blobResult;
    }

    public static BlobRequestResult Succeeded(BlobDownloadResult blobResult)
    {
        return new BlobRequestResult(true, (int)HttpStatusCode.OK, null, blobResult);
    }

    public static BlobRequestResult Failed(int statusCode, string moreInfo)
    {
        return new BlobRequestResult(false, statusCode, moreInfo, null);
    }
}