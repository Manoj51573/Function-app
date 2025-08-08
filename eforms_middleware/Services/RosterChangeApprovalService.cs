using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Settings;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;

namespace eforms_middleware.Services
{

    public class RosterChangeApprovalService
    {
        private readonly IAttachmentRecordService _attachmentRecordService;
        private readonly RosterChangeRequestService _rosterChangeRequestService;
        public RosterChangeApprovalService(IAttachmentRecordService attachmentRecordService,
            RosterChangeRequestService rosterChangeRequestService)
        {
            _attachmentRecordService = attachmentRecordService;
            _rosterChangeRequestService = rosterChangeRequestService;
        }

        public async Task<RequestResult> RosterChangeApprovalSystem(string requestBody)
        {
            var formInfoInsertModel = JsonConvert.DeserializeObject<RosterChangeInsertModel>(requestBody);
            var data = JsonConvert.DeserializeObject<RosterChangeModel>(formInfoInsertModel.FormDetails.Response);
            var formInfoId = formInfoInsertModel.FormDetails.FormInfoID;
            var formStatus = formInfoInsertModel.FormAction.GetParseEnum<FormStatus>();


            if (formInfoId.HasValue && data.SupportingInfoGroup?.Attachments is null
                    && formStatus is FormStatus.Unsubmitted or FormStatus.Submitted)
            {
                await _attachmentRecordService.DeactivateAllAttachmentsAsync(formInfoId!.Value);
            }
            else if (data.SupportingInfoGroup?.Attachments is not null
                && data.SupportingInfoGroup.Attachments.Any() && formStatus is FormStatus.Unsubmitted or FormStatus.Submitted)
            {
                await _attachmentRecordService.ActivateAttachmentRecordsAsync(formInfoId!.Value,
                            data.SupportingInfoGroup.Attachments);
            }
            RequestResult requestResult = null;
            switch (formInfoInsertModel.FormDetails.AllFormsID)
            {
                case (int)FormType.Rcr:
                    requestResult = await _rosterChangeRequestService.RosterChangeRequestApprovalProcess(formInfoInsertModel);
                    break;
            }

            return requestResult ??
                RequestResult.FailedRequest(StatusCodes.Status500InternalServerError, "Unknown error");

        }
    }
}
