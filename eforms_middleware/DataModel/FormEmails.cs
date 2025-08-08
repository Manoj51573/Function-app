using System;

namespace eforms_middleware.DataModel
{
    public class FormEmailInsertModel
    {
        public int? EmailInfoID { get; set; }
        public int AllFormsID { get; set; }
        public string EmailReferenceTitle { get; set; }
        public string EmailSubject { get; set; }
        public int EmailSequence { get; set; }
        public string EmailFrom { get; set; }
        public string EmailTo { get; set; }
        public string EmailCC { get; set; }
        public string EmailBCC { get; set; }
        public string EmailContent { get; set; }
        public DateTime Modified { get; set; }
        public string ModifiedBy { get; set; }
        public string EmailHeader { get; set; }
        public string EmailFooter { get; set; }
        public string TaskTo { get; set; }
        public bool ActiveRecord { get; set; }
    }

    public class FormEmailGetModel
    {
        public int? EmailInfoID { get; set; }
        public int AllFormsID { get; set; }
        public string EmailReferenceTitle { get; set; }
        public string EmailSubject { get; set; }
        public int EmailSequence { get; set; }
        public string EmailFrom { get; set; }
        public string EmailTo { get; set; }
        public string EmailCC { get; set; }
        public string EmailBCC { get; set; }
        public string EmailContent { get; set; }
        public DateTime Modified { get; set; }
        public string ModifiedBy { get; set; }
        public string EmailHeader { get; set; }
        public string EmailFooter { get; set; }
        public string TaskTo { get; set; }
        public bool ActiveRecord { get; set; }
    }

    public class AllFormEmailModel
    {
        public int? EmailInfoID { get; set; }
        public int AllFormsID { get; set; }
        public string EmailReferenceTitle { get; set; }
        public string EmailSubject { get; set; }
        public int EmailSequence { get; set; }
        public string EmailFrom { get; set; }
        public string EmailTo { get; set; }
        public string EmailCC { get; set; }
        public string EmailBCC { get; set; }
        public string EmailContent { get; set; }
        public DateTime Modified { get; set; }
        public string ModifiedBy { get; set; }
        public string EmailHeader { get; set; }
        public string EmailFooter { get; set; }
        public string TaskTo { get; set; }
        public bool ActiveRecord { get; set; }
    }
}
