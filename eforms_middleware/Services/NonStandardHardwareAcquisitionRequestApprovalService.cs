using System.Linq;
using System.Threading.Tasks;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Settings;
using eforms_middleware.Workflows;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace eforms_middleware.Services;

public class NonStandardHardwareAcquisitionRequestApprovalService
{
    private readonly IAttachmentRecordService _attachmentRecordService;

    //Non-Standard Hardware Acquisition Request
    private readonly NonStandardHardwareAcquisitionRequestFormService _nonStandardHardwareAcquisitionRequestFormService;

    public NonStandardHardwareAcquisitionRequestApprovalService(
        NonStandardHardwareAcquisitionRequestFormService nonStandardHardwareAcquisitionRequestFormService
        , IAttachmentRecordService attachmentRecordService)
    {
        _nonStandardHardwareAcquisitionRequestFormService = nonStandardHardwareAcquisitionRequestFormService;
        _attachmentRecordService = attachmentRecordService;
    }

    public async Task<RequestResult> NonStandardHardwareAcquisitionRequestApprovalSystem(string requestBody)
    {
        var formInfoInsertModel = JsonConvert.DeserializeObject<HomeGaragingInfoInsertModel>(requestBody);
        var data = JsonConvert.DeserializeObject<NonStandardHardwareAcquisitionRequestModel>(formInfoInsertModel
            .FormDetails.Response);
        var formInfoId = formInfoInsertModel.FormDetails.FormInfoID;
        var formStatus = formInfoInsertModel.FormAction.GetParseEnum<FormStatus>();

        // Attachments must be checked in case it is a dynamic form element. If it is hidden after the fact
        // will need to Deactivate all the attachments.
        if (formInfoId.HasValue && data.Attachments is null &&
            formStatus is FormStatus.Unsubmitted or FormStatus.Submitted)
            await _attachmentRecordService.DeactivateAllAttachmentsAsync(formInfoId!.Value);
        else if (data.Attachments is not null && data.Attachments.Any() &&
                 formStatus is FormStatus.Unsubmitted or FormStatus.Submitted)
            await _attachmentRecordService.ActivateAttachmentRecordsAsync(formInfoId!.Value, data.Attachments);

        RequestResult requestResult = null;
        requestResult =
            await _nonStandardHardwareAcquisitionRequestFormService
                .NonStandardHardwareAcquisitionRequestApprovalProcess(formInfoInsertModel);
        return requestResult ?? RequestResult.FailedRequest(StatusCodes.Status500InternalServerError, "Unknown error");
    }
}