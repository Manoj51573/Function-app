using System;
using DoT.Infrastructure.DbModels.Entities;

namespace eforms_middleware.Specifications;

public class TaskInfoSpecification : BaseQuerySpecification<TaskInfo>
{
    public TaskInfoSpecification(int? formInfoId = null, DateTime? reminderDate = null, DateTime? escalationDate = null, bool activeOnly = false)
    : base(
        x => (!formInfoId.HasValue || x.FormInfoId == formInfoId) &&
             (!reminderDate.HasValue || reminderDate == x.SpecialReminderDate) &&
             (!escalationDate.HasValue || escalationDate == x.EscalationDate) &&
             (!activeOnly || x.ActiveRecord == true),
        x => x.TaskCreatedDate, orderDescending: true)
    {
        AddInclude(t => t.FormInfo);
    }
}