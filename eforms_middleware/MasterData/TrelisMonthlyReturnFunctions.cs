using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using eforms_middleware.Services;

namespace eforms_middleware.MasterData
{
    public class TrelisMonthlyReturnFunctions
    {
        private readonly ITrelisTimedActionsService _trelisTimedActionsService;

        public TrelisMonthlyReturnFunctions(ITrelisTimedActionsService trelisTimedActionsService)
        {
            _trelisTimedActionsService = trelisTimedActionsService;
        }

        // Run every 4th Monday of the month at 1 a.m.
        [FunctionName("func-create-trelis-monthly-return-timer")]
        public async Task RunAsync([TimerTrigger("0 0 1 22-28 * MON")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation("C# Timer trigger function executed at: {Now}", DateTime.Now);

            await _trelisTimedActionsService.CreateTrelisForms();
            
            log.LogInformation("C# Timer trigger function finished at: {Now}", DateTime.Now);
        }
    }

    public class TrelisNotificationFunctions
    {
        private readonly ITrelisTimedActionsService _trelisTimedActionsService;

        public TrelisNotificationFunctions(ITrelisTimedActionsService trelisTimedActionsService)
        {
            _trelisTimedActionsService = trelisTimedActionsService;
        }
        
        [FunctionName("func-create-trelis-notification-timer")]
        public async Task Run([TimerTrigger("0 0 8 * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation("Notification trigger function executed at: {Now}", DateTime.Now);
            try
            {
                await _trelisTimedActionsService.SendReminderEmailsAsync();
            }
            catch (Exception e)
            {
                log.LogError(e, $"Error in {nameof(TrelisNotificationFunctions)}");
            }
            log.LogInformation("Notification trigger function finished at: {Now}", DateTime.Now);
        }
    }
}