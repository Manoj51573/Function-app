using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using System.Threading.Tasks;
using eforms_middleware.Constants;

namespace eforms_middleware.Services
{
    public class WorkflowBtnService : IWorkflowBtnService
    {
        protected IRepository<WorkflowBtn> WorkflowBtnRepository;

        public WorkflowBtnService(IRepository<WorkflowBtn> workflowBtnRepository)
        {
            WorkflowBtnRepository = workflowBtnRepository;
        }

        public async Task SetNextApproverBtn(int formId
            , StatusBtnData btnStatus
            , int? permissionId)
        {
            if (btnStatus != null)
            {
                var existingWorkFlow =
                    await WorkflowBtnRepository.FindByAsync(x => x.FormId == formId
                     && x.FormSubStatus != FormStatus.Delegated.ToString());

                existingWorkFlow.ForEach(x =>
                {
                    x.IsActive = false;
                    WorkflowBtnRepository.Update(x);
                });

                btnStatus.StatusBtnModel?.ForEach(x =>
                {
                    WorkflowBtnRepository.Create(new WorkflowBtn()
                    {
                        FormId = formId,
                        IsActive = true,
                        PermisionId = permissionId ?? 0,
                        StatusId = x.StatusId,
                        FormSubStatus = x.FormSubStatus,
                        BtnText = x.BtnText
                    });
                });
            }
        }

    }
}
