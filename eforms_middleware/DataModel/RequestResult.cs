using System.Collections.Generic;
using System.Linq;
using DoT.Infrastructure.DbModels.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace eforms_middleware.DataModel
{
    public class RequestResult
    {
        public bool Success { get; private set; }
        public int StatusCode { get; private set; }
        public string MoreInfo { get; private set; }
        public object Value { get; private set; }

        private RequestResult(bool success, int statusCode, object value, string moreInfo = null)
        {
            Success = success;
            StatusCode = statusCode;
            MoreInfo = moreInfo;
            Value = value;
        }

        public static RequestResult SuccessfulFormInfoRequest(FormInfo formInfo)
        {
            return new RequestResult(true, StatusCodes.Status200OK, new {outcome = "Success",
                outcomeValues = new
                {
                    allFormsID = formInfo.AllFormsId,
                    formInfoID = formInfo.FormInfoId,
                    formItemId = formInfo.FormItemId,
                    formAction = formInfo.FormSubStatus,
                }});
        }

        public static RequestResult FailedRequest(int statusCode, object errorInfo)
        {
            return new RequestResult(false, statusCode, errorInfo);
        }

        public static RequestResult SuccessfulAttachmentRequest(List<AttachmentRecordFile> formAttachments)
        {
            return new RequestResult(true, StatusCodes.Status200OK, new
            {
                outcome = "Success",
                outcomeValues = formAttachments.Select(x => new
                {
                    x.Id, Name = x.File.FileName
                })
            });
        }

        public static RequestResult SuccessfulRequest()
        {
            return new RequestResult(true, StatusCodes.Status204NoContent, null);
        }

        public static IActionResult ErrorActionResult(int errorCode = 500, object value = null)
        {
            var overrideToUnexpected = errorCode < 300;
            var actionResult = new JsonResult(null);
            actionResult.StatusCode = overrideToUnexpected ? 500 : errorCode;
            actionResult.Value = overrideToUnexpected ? "An unexpected error occurred. Please try again later." : value;
            return actionResult;
        }

        public IActionResult ToActionResult()
        {
            var actionResult = new JsonResult(null);
            actionResult.StatusCode = StatusCode;
            actionResult.Value = Value;
            return actionResult;
        }

        public static RequestResult SuccessRequestWithErrorMessage(object errorInfo)
        {
            return new RequestResult(true, StatusCodes.Status200OK, errorInfo);
        }

    }
}