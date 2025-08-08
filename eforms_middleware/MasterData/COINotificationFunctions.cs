using eforms_middleware.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace eforms_middleware.MasterData
{
    public class COINotificationFunctions
    {
        private readonly COINotificationService _COINotificationService;
        public COINotificationFunctions(COINotificationService COINotificationService)
        {
            _COINotificationService = COINotificationService;
        }

        [FunctionName("func-create-coi-notification-timer")]
        public async Task Run([TimerTrigger("0 0 4 * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation("COI Notification trigger function executed at: {Now}", DateTime.Now);
            try
            {
                await _COINotificationService.RunReminder(log);
            }
            catch (Exception e)
            {
                log.LogError(e, $"Error in {nameof(COINotificationFunctions)}");
            }
            log.LogInformation("COI Notification trigger function finished at: {Now}", DateTime.Now);
        }
    }
}
