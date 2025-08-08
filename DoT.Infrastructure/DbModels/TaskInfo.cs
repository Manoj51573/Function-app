using System;

namespace DoT.Infrastructure.DbModels.Entities;

public partial class TaskInfo
{
    public bool IsReminder => SpecialReminderDate.HasValue && SpecialReminderDate == DateTime.Today;
    public bool IsEscalation => EscalationDate.HasValue && EscalationDate == DateTime.Today;
}