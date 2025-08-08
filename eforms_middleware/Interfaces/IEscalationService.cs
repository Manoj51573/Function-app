using System.Threading.Tasks;
using DoT.Infrastructure.DbModels.Entities;

namespace eforms_middleware.Interfaces;

public interface IEscalationService
{
    Task<FormInfo> EscalateFormAsync(TaskInfo task);
    Task NotifyFormAsync(FormInfo dbModel, int reminderDays = 2, int escalationDays = 5, bool allowEscalation = false);
}