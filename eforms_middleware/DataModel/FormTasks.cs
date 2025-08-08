using System;

namespace eforms_middleware.DataModel
{
    public class FormTaskInsertModel
    {
        public int? TaskInfoID { get; set; }
        public int AllFormsID { get; set; }
        public int FormInfoID { get; set; }
        public int? EmailInfoID { get; set; }
        public string FormOwnerEmail { get; set; }
        public string AssignedTo { get; set; }
        public string TaskStatus { get; set; }
        public DateTime TaskCreatedDate { get; set; }
        public string TaskCreatedBy { get; set; }
        public DateTime TaskCompletedDate { get; set; }
        public string TaskCompletedBy { get; set; }
        public int RemindersCount { get; set; }
        public int ReminderFrequency { get; set; }
        public string ReminderTo { get; set; }
        public bool SpecialReminder { get; set; }
        public string SpecialReminderTo { get; set; }
        public DateTime SpecialReminderDate { get; set; }
        public bool Escalation { get; set; }
        public DateTime EscalationDate { get; set; }
        public bool ActiveRecord { get; set; }
    }

    public class FormTaskGetModel
    {
        public string TaskInfoID { get; set; }
        public string AllFormsID { get; set; }
        public string FormInfoID { get; set; }
        public string EmailInfoID { get; set; }
        public string FormOwnerEmail { get; set; }
        public string AssignedTo { get; set; }
        public string TaskStatus { get; set; }
        public string TaskCreatedDate { get; set; }
        public string TaskCreatedBy { get; set; }
        public string TaskCompletedDate { get; set; }
        public string TaskCompletedBy { get; set; }
        public string RemindersCount { get; set; }
        public string ReminderFrequency { get; set; }
        public string ReminderTo { get; set; }
        public bool SpecialReminder { get; set; }
        public string SpecialReminderTo { get; set; }
        public string SpecialReminderDate { get; set; }
        public bool Escalation { get; set; }
        public string EscalationDate { get; set; }
        public bool ActiveRecord { get; set; }
    }

    public class AllFormTaskModel
    {
        public string TaskInfoID { get; set; }
        public string AllFormsID { get; set; }
        public string FormInfoID { get; set; }
        public string EmailInfoID { get; set; }
        public string FormOwnerEmail { get; set; }
        public string AssignedTo { get; set; }
        public string TaskStatus { get; set; }
        public string TaskCreatedDate { get; set; }
        public string TaskCreatedBy { get; set; }
        public string TaskCompletedDate { get; set; }
        public string TaskCompletedBy { get; set; }
        public string RemindersCount { get; set; }
        public string ReminderFrequency { get; set; }
        public string ReminderTo { get; set; }
        public bool SpecialReminder { get; set; }
        public string SpecialReminderTo { get; set; }
        public string SpecialReminderDate { get; set; }
        public bool Escalation { get; set; }
        public string EscalationDate { get; set; }
        public bool ActiveRecord { get; set; }
    }
}
