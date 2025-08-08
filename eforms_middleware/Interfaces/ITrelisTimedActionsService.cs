using System.Threading.Tasks;

namespace eforms_middleware.Services;

public interface ITrelisTimedActionsService
{
    Task SendReminderEmailsAsync();
    Task CreateTrelisForms(int? branchId = null);
}