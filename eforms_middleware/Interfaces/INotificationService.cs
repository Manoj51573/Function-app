using System.Threading.Tasks;

namespace eforms_middleware.Interfaces;

public interface INotificationService
{
    Task SendNotificationsAsync();
}