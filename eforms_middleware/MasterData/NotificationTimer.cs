using eforms_middleware.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace eforms_middleware.MasterData;

public class NotificationTimer
{
    private readonly INotificationService _notificationService;

    public NotificationTimer(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [FunctionName("func-notification-timer")]
    public async Task RunAsync([TimerTrigger("0 0 5 * * *")] TimerInfo myTimer, ILogger log)
    {
        log.LogInformation($"C# Timer trigger function executed at: {DateTime.UtcNow}");
        // In the service get all notifications for the day using specification to exclude standard COI
        // Have is escalation or reminder already worked out
        // Have the mail builder send the message
        await _notificationService.SendNotificationsAsync();

        log.LogInformation($"C# Timer trigger function finished at: {DateTime.UtcNow}");
    }


    [FunctionName("func-notification-timer-test")]
    public async Task TestAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
    {
        log.LogInformation($"C# Timer trigger function executed at: {DateTime.UtcNow}");
        // In the service get all notifications for the day using specification to exclude standard COI
        // Have is escalation or reminder already worked out
        // Have the mail builder send the message
        await _notificationService.SendNotificationsAsync();

        log.LogInformation($"C# Timer trigger function finished at: {DateTime.UtcNow}");
    }
}
