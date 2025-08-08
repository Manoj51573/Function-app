using System;
using System.Threading.Tasks;
using DoT.Eforms.Test.Shared;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DoT.Eforms.Test;

public class TaskManagerTest
{
    private Mock<IRepository<TaskInfo>> _repository;
    private Mock<ILogger<TaskManager>> _logger;
    private readonly TaskManager _manager;

    public TaskManagerTest()
    {
        _repository = new Mock<IRepository<TaskInfo>>();
        _logger = new Mock<ILogger<TaskManager>>();
        _manager = new TaskManager(_repository.Object, _logger.Object);

        _repository.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<TaskInfo>>())).ReturnsAsync(new TaskInfo()
            { ActiveRecord = true, TaskInfoId = 1, TaskStatus = "Submitted" });
    }
    
    [Fact]
    public async Task AddFormTaskAsync_should_add_new_task_and_deactivate_all_prior_tasks()
    {
        var newTask = new TaskInfo
        {
            ActiveRecord = true, SpecialReminder = true, SpecialReminderDate = DateTime.Today.AddDays(3), TaskStatus = "Submitted"
        };

        await _manager.AddFormTaskAsync(3, newTask);
        
        _logger.VerifyLogging("", LogLevel.Error, Times.Never(), true);
        _repository.Verify(
            x => x.Update(It.Is<TaskInfo>(t =>
                t.TaskInfoId == 1 && t.TaskStatus == "Submitted" && t.ActiveRecord == false)), Times.Once);
        _repository.Verify(x => x.AddAsync(It.Is<TaskInfo>(t =>
            t.SpecialReminder == true && t.ActiveRecord == true &&
            t.SpecialReminderDate == DateTime.Today.AddDays(3))), Times.Once);
    }

    [Fact]
    public async Task AddFormTaskAsync_should_just_add_when_no_prior_tasks()
    {
        _repository.Setup(x => x.FirstOrDefaultAsync(It.IsAny<ISpecification<TaskInfo>>()))
            .ReturnsAsync(default(TaskInfo));
        
        var newTask = new TaskInfo
        {
            ActiveRecord = true, SpecialReminder = true, SpecialReminderDate = DateTime.Today.AddDays(3), TaskStatus = "Submitted"
        };

        await _manager.AddFormTaskAsync(3, newTask);
        
        _logger.VerifyLogging("", LogLevel.Error, Times.Never(), true);
        _repository.Verify(
            x => x.Update(It.IsAny<TaskInfo>()), Times.Never);
        _repository.Verify(x => x.AddAsync(It.Is<TaskInfo>(t =>
            t.SpecialReminder == true && t.ActiveRecord == true &&
            t.SpecialReminderDate == DateTime.Today.AddDays(3))), Times.Once);
    }

    [Fact]
    public async Task AddFormTaskAsync_should_clear_the_last_task_when_no_new_task()
    {
        await _manager.AddFormTaskAsync(3);
        
        _logger.VerifyLogging("", LogLevel.Error, Times.Never(), true);
        _repository.Verify(
            x => x.Update(It.Is<TaskInfo>(t =>
                t.TaskInfoId == 1 && t.TaskStatus == "Submitted" && t.ActiveRecord == false)), Times.Once);
        _repository.Verify(x => x.AddAsync(It.IsAny<TaskInfo>()), Times.Never);
    }
}