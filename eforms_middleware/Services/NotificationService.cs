using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Specifications;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace eforms_middleware.Services;

public class NotificationService : INotificationService
{
    private readonly ITaskManager _taskManager;
    private readonly IMessageFactoryService _messageFactoryService;
    private readonly IEscalationService _escalationService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ITaskManager taskManager, IMessageFactoryService messageFactoryService,
        IEscalationService escalationService, ILogger<NotificationService> logger)
    {
        _taskManager = taskManager;
        _messageFactoryService = messageFactoryService;
        _escalationService = escalationService;
        _logger = logger;
    }

    public async Task SendNotificationsAsync()
    {
        // Do not allow the forms that won't work through here
        var excludedTypes = new[] { (int)FormType.CoI, (int)FormType.CoI_REC, (int)FormType.CoI_GBH, (int)FormType.CoI_SE, (int)FormType.e29 };
        var tasks =
            await _taskManager.GetTaskInfoAsync(new TaskInfoForDateSpecification(taskDate: DateTime.Today, excludeTypes: excludedTypes,
                activeOnly: true));
        foreach (var task in tasks)
        {
            try
            {

                if (task.IsReminder)
                {
                    var formAction = Enum.GetName(typeof(FormStatus), task.FormInfo.FormStatusId);
                    await _messageFactoryService.SendEmailAsync(task.FormInfo,
                        new FormInfoUpdate { FormAction = formAction });

                }

                if (task.IsEscalation)
                {
                    var savedForm = await _escalationService.EscalateFormAsync(task);
                    await _messageFactoryService.SendEmailAsync(savedForm,
                        new FormInfoUpdate { FormAction = Enum.GetName(FormStatus.Escalated) });

                }

                if (!task.IsReminder && !task.IsEscalation)
                {
                    _logger.LogInformation($"Task {task.TaskInfoId} for Form {task.FormInfoId} is neither reminder or escalation", task);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error thrown in {nameof(NotificationService)} for Task {task.TaskInfoId}", task);
            }
        }
    }
}