using System;
using System.Collections.Generic;
using System.Linq;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Specifications;
using Xunit;

namespace DoT.Eforms.Test.Specifications;

public class TaskInfoSpecificationTest
{
    [Fact]
    public void Matches_all_items_matching_form_info_id()
    {
        var specification = new TaskInfoSpecification(1);
        var result = GetTestCollection()
            .AsQueryable()
            .Where(specification.Criteria);

        Assert.Collection(result, task =>
        {
            Assert.Equal(1, task.FormInfoId);
            Assert.Equal(3, task.TaskInfoId);
            Assert.True(task.ActiveRecord);
        }, task =>
        {
            Assert.Equal(1, task.FormInfoId);
            Assert.Equal(2, task.TaskInfoId);
            Assert.False(task.ActiveRecord);
        }, task =>
        {
            Assert.Equal(1, task.FormInfoId);
            Assert.Equal(1, task.TaskInfoId);
            Assert.False(task.ActiveRecord);
        });
    }
    
    [Fact]
    public void Matches_only_active_item_for_form_info_id()
    {
        var specification = new TaskInfoSpecification(1, activeOnly: true);
        var result = GetTestCollection()
            .AsQueryable()
            .Where(specification.Criteria);

        var taskInfo = Assert.Single(result);
        Assert.Equal(1, taskInfo.FormInfoId);
        Assert.True(taskInfo.ActiveRecord);
    }

    [Fact]
    public void Matches_all_reminders_for_the_day()
    {
        var today = DateTime.Today;
        var specification = new TaskInfoSpecification(reminderDate: today, activeOnly: true);
        var result = GetTestCollection()
            .AsQueryable()
            .Where(specification.Criteria);
        
        var info = Assert.Single(result);

        Assert.Equal(10, info.FormInfoId);
        Assert.Equal(7, info.TaskInfoId);
        Assert.Equal(today, info.SpecialReminderDate);
        Assert.NotEqual(today, info.EscalationDate);
        Assert.True(info.ActiveRecord);
    }
    
    [Fact]
    public void Matches_all_escalations_for_the_day()
    {
        var today = DateTime.Today;
        var specification = new TaskInfoSpecification(escalationDate: today, activeOnly: true);
        var result = GetTestCollection()
            .AsQueryable()
            .Where(specification.Criteria);
        
        var info = Assert.Single(result);

        Assert.Equal(13, info.FormInfoId);
        Assert.Equal(9, info.TaskInfoId);
        Assert.NotEqual(today, info.SpecialReminderDate);
        Assert.Equal(today, info.EscalationDate);
        Assert.True(info.ActiveRecord);
    }

    private static IEnumerable<TaskInfo> GetTestCollection()
    {
        var today = DateTime.Today;
        return new List<TaskInfo>
        {
            new ()
            {
                FormInfoId = 1, TaskInfoId = 3, TaskCreatedDate = today, ActiveRecord = true
            },
            new ()
            {
                FormInfoId = 1, TaskInfoId = 2, TaskCreatedDate = today.AddDays(-1), ActiveRecord = false
            },
            new ()
            {
                FormInfoId = 1, TaskInfoId = 1, TaskCreatedDate = today.AddDays(-2), ActiveRecord = false
            },
            new ()
            {
                FormInfoId = 2, TaskInfoId = 5, TaskCreatedDate = today, ActiveRecord = true
            },
            new ()
            {
                FormInfoId = 2, TaskInfoId = 4, TaskCreatedDate = today, ActiveRecord = false
            },
            new ()
            {
                FormInfoId = 10, TaskInfoId = 6, TaskCreatedDate = today.AddDays(-3), ActiveRecord = false,
                SpecialReminderDate = today, EscalationDate = today.AddDays(2)
            },
            new ()
            {
                FormInfoId = 10, TaskInfoId = 7, TaskCreatedDate = today.AddDays(-3), ActiveRecord = true,
                SpecialReminderDate = today, EscalationDate = today.AddDays(2)
            },
            new ()
            {
                FormInfoId = 13, TaskInfoId = 8, TaskCreatedDate = today.AddDays(-5), ActiveRecord = false,
                SpecialReminderDate = today.AddDays(-2), EscalationDate = today
            },
            new ()
            {
                FormInfoId = 13, TaskInfoId = 9, TaskCreatedDate = today.AddDays(-5), ActiveRecord = true,
                SpecialReminderDate = today.AddDays(-2), EscalationDate = today
            }
        };
    }
}