using System;
using System.Collections.Generic;

namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class TaskInfo
    {
        public int TaskInfoId { get; set; }
        public int AllFormsId { get; set; }
        public int FormInfoId { get; set; }
        public string FormOwnerEmail { get; set; }
        public string AssignedTo { get; set; }
        public string TaskStatus { get; set; }
        public DateTime? TaskCreatedDate { get; set; }
        public string TaskCreatedBy { get; set; }
        public DateTime? TaskCompletedDate { get; set; }
        public string TaskCompletedBy { get; set; }
        public int? RemindersCount { get; set; }
        public int? ReminderFrequency { get; set; }
        public string ReminderTo { get; set; }
        public bool? SpecialReminder { get; set; }
        public string SpecialReminderTo { get; set; }
        public DateTime? SpecialReminderDate { get; set; }
        public bool? Escalation { get; set; }
        public bool? ActiveRecord { get; set; }
        public int? EmailInfoId { get; set; }
        public DateTime? EscalationDate { get; set; }

        public virtual AllForm AllForms { get; set; }
        public virtual FormInfo FormInfo { get; set; }
    }
}
