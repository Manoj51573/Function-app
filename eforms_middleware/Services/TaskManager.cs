using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Interfaces;
using eforms_middleware.Specifications;
using Microsoft.Extensions.Logging;

namespace eforms_middleware.Services;

public class TaskManager : ITaskManager
{
    private readonly IRepository<TaskInfo> _repository;
    private readonly ILogger<TaskManager> _logger;

    public TaskManager(IRepository<TaskInfo> repository, ILogger<TaskManager> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IList<TaskInfo>> GetTaskInfoAsync(ISpecification<TaskInfo> specification)
    {
        return await _repository.ListAsync(specification);
    }

    public async Task AddFormTaskAsync(int formInfoId, TaskInfo taskInfo = null)
    {
        try
        {
            var specification = new TaskInfoSpecification(formInfoId, activeOnly: true);
            var previousTask = await _repository.FirstOrDefaultAsync(specification);
            if (previousTask != null)
            {
                previousTask.ActiveRecord = false;
                previousTask.Escalation = false;
                previousTask.SpecialReminder= false;
                previousTask.EscalationDate = null;
                previousTask.SpecialReminderDate = null;
                _repository.Update(previousTask);
            }

            if (taskInfo != null)
            {
                await _repository.AddAsync(taskInfo);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error thrown in {nameof(TaskManager)}");
            throw;
        }
    }

    public async Task AddFormTaskAsync(TaskInfo originalTask, TaskInfo taskInfo = null)
    {
        try
        {

            originalTask.ActiveRecord = false;
            _repository.Update(originalTask);

            if (taskInfo != null)
            {
                await _repository.AddAsync(taskInfo);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error thrown in {nameof(TaskManager)}");
            throw;
        }
    }
}