using System;
using System.Collections.Generic;

namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class EmailSentInfo
    {
        public int EmailSentInfoId { get; set; }
        public int AllFormsId { get; set; }
        public int FormInfoId { get; set; }
        public int EmailInfoId { get; set; }
        public string EmailSubject { get; set; }
        public string EmailFrom { get; set; }
        public string EmailTo { get; set; }
        public string EmailCc { get; set; }
        public string EmailBcc { get; set; }
        public string EmailContent { get; set; }
        public DateTime SentOn { get; set; }
        public bool? EmailSentFlag { get; set; }
        public bool? ActiveRecord { get; set; }

        public virtual AllForm AllForms { get; set; }
    }
}
