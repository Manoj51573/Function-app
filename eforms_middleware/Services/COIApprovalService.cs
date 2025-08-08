using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Settings;
using eforms_middleware.Workflows;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using eforms_middleware.Interfaces;
using Microsoft.AspNetCore.Http;
using static eforms_middleware.Settings.Helper;

namespace eforms_middleware.Services
{
    public class COIApprovalService
    {
        private readonly RecruitmentFormService _recruitmentFormService;
        private readonly GBHService _gbhService;
        private readonly SecondaryEmploymentService _secondaryEmployment;
        private readonly IAttachmentRecordService _attachmentRecordService;

        public COIApprovalService(GBHService gbhService
            , RecruitmentFormService recruitmentFormService
            , SecondaryEmploymentService secondaryEmployment
            , IAttachmentRecordService attachmentRecordService)
        {
            _gbhService = gbhService;
            _recruitmentFormService = recruitmentFormService;
            _secondaryEmployment = secondaryEmployment;
            _attachmentRecordService = attachmentRecordService;
        }

        public async Task<RequestResult> COIApprovalSystem(string requestBody)
        {
            var formInfoInsertModel = JsonConvert.DeserializeObject<FormInfoInsertModel>(requestBody);
            var data = JsonConvert.DeserializeObject<COIMain>(formInfoInsertModel.FormDetails.Response);
            var formInfoId = formInfoInsertModel.FormDetails.FormInfoID;
            var formStatus = formInfoInsertModel.FormAction.GetParseEnum<FormStatus>();

            // Attachments must be checked in case it is a dynamic form element. If it is hidden after the fact
            // will need to Deactivate all the attachments.
            if (formInfoId.HasValue && data.Attachments is null && formStatus is FormStatus.Unsubmitted or FormStatus.Submitted)
            {
                await _attachmentRecordService.DeactivateAllAttachmentsAsync(formInfoId!.Value);
            }
            else if (data.Attachments is not null && data.Attachments.Any() && formStatus is FormStatus.Unsubmitted or FormStatus.Submitted)
            {
                await _attachmentRecordService.ActivateAttachmentRecordsAsync(formInfoId!.Value, data.Attachments);
            }

            RequestResult requestResult = null;

            switch (formInfoInsertModel.FormDetails.AllFormsID)
            {
                case (int)FormType.CoI_REC:
                    requestResult = await _recruitmentFormService.RecruitmentApprovalProcess(formInfoInsertModel);
                    break;
                case (int)FormType.CoI_GBH:
                    requestResult = await _gbhService.GBHApprovalProcess(formInfoInsertModel);
                    break;
                case (int)FormType.CoI_SE:
                    requestResult = await _secondaryEmployment.SEApprovalProcess(formInfoInsertModel);
                    break;
            }

            return requestResult ?? RequestResult.FailedRequest(StatusCodes.Status500InternalServerError, "Unknown error");
        }
    }
}
