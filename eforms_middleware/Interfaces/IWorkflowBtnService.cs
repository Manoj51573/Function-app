using eforms_middleware.DataModel;
using System.Threading.Tasks;

namespace eforms_middleware.Interfaces
{
    public interface IWorkflowBtnService
    {
        Task SetNextApproverBtn(int formId, StatusBtnData btnStatus, int? permissionId);
    }
}
