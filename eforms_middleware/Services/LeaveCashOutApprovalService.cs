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
    public class LeaveCashOutApprovalService
    {
        private readonly LeaveAmendmentFormService _leaveAmendmentFormService;
        private readonly MaternityLeaveFormService _maternityLeaveFormService;
        private readonly PurchaseLeaveFormService _purchaseLeaveFormService;
        private readonly LeaveCashOutFormService _leaveCashOutFormService;
        private readonly LeaveWithoutPayFormService _leaveWithoutPayFormService;
        private readonly ProRataLeaveFormService _proRataLeaveFormService;
        private readonly GBHService _gbhService;
        private readonly SecondaryEmploymentService _secondaryEmployment;
        private readonly IAttachmentRecordService _attachmentRecordService;    
        
        public LeaveCashOutApprovalService(GBHService gbhService
            , LeaveAmendmentFormService leaveAmendmentFormService
            , MaternityLeaveFormService maternityLeaveFormService
            , PurchaseLeaveFormService  purchaseLeaveFormService
            , LeaveCashOutFormService   leaveCashOutFormService
            , LeaveWithoutPayFormService leaveWithoutPayFormService
            , ProRataLeaveFormService proRataLeaveFormService
            , IAttachmentRecordService attachmentRecordService)
        {
            _gbhService = gbhService;
            _leaveAmendmentFormService = leaveAmendmentFormService;
            _maternityLeaveFormService = maternityLeaveFormService;
            _purchaseLeaveFormService= purchaseLeaveFormService;
            _leaveCashOutFormService = leaveCashOutFormService;
            _leaveWithoutPayFormService = leaveWithoutPayFormService;
            _proRataLeaveFormService = proRataLeaveFormService;
            _attachmentRecordService = attachmentRecordService;
        }

        public async Task<RequestResult> LeaveCashOutApprovalSystem(string requestBody)
        {            
            var formInfoInsertModel = JsonConvert.DeserializeObject<LeaveInfoInsertModel>(requestBody);
            var data = JsonConvert.DeserializeObject<LCOMain>(formInfoInsertModel.FormDetails.Response);
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
            var formType = (FormType)formInfoInsertModel.FormDetails.AllFormsID;
            switch (formType)
            {
                case FormType.LcR_LAC:
                    requestResult = await _leaveAmendmentFormService.LeaveAmendmentApprovalProcess(formInfoInsertModel);
                    break;
                case FormType.LcR_MA:
                    requestResult = await _maternityLeaveFormService.MaternityLeaveApprovalProcess(formInfoInsertModel);
                    break;
                case FormType.LcR_DSA:
                case FormType.LcR_PLA:
                    requestResult = await _purchaseLeaveFormService.PurchasedLeaveApprovalProcess(formInfoInsertModel);
                    break;
                case FormType.LcR_LCO:
                    requestResult = await _leaveCashOutFormService.LeaveCashOutApprovalProcess(formInfoInsertModel);
                    break;
                case FormType.LcR_LWP:
                    requestResult = await _leaveWithoutPayFormService.LeaveWithoutPayApprovalProcess(formInfoInsertModel);
                    break;
                case FormType.LcR_PLS:
                    requestResult = await _proRataLeaveFormService.ProRataLeaveApprovalProcess(formInfoInsertModel);
                    break;
            }

            return requestResult ?? RequestResult.FailedRequest(StatusCodes.Status500InternalServerError, "Unknown error");
        }
    }
}
