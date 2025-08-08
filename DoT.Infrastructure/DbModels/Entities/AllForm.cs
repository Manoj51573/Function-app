using System;
using System.Collections.Generic;

namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class AllForm
    {
        public AllForm()
        {
            FormInfos = new HashSet<FormInfo>();
            InverseParentAllForms = new HashSet<AllForm>();
            TaskInfos = new HashSet<TaskInfo>();
        }

        public int AllFormsId { get; set; }
        public string FormCode { get; set; }
        public string FormTitle { get; set; }
        public string FormId { get; set; }
        public string BusinessOwner { get; set; }
        public int BusinessDirectorateId { get; set; }
        public string FormType { get; set; }
        public string FormCategory { get; set; }
        public string VisibleTo { get; set; }
        public DateTime? Created { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? Modified { get; set; }
        public string ModifiedBy { get; set; }
        public string FormUrl { get; set; }
        public string ArchiveUrl { get; set; }
        public bool? ActiveRecord { get; set; }
        public int? ParentAllFormsId { get; set; }
        public bool? IsEmployeeOnly { get; set; }
        public bool IsAvailable { get; set; }
        public bool? HasDeclaration { get; set; }
        public int TotalPage { get; set; }
        public string TooltipValue { get; set; }
        public string RequestType { get; set; }
        public string PageTitle { get; set; }
        public string IconValue { get; set; }
        public string FormGroupName { get; set; }

        public virtual AllForm ParentAllForms { get; set; }
        public virtual ICollection<FormInfo> FormInfos { get; set; }
        public virtual ICollection<AllForm> InverseParentAllForms { get; set; }
        public virtual ICollection<TaskInfo> TaskInfos { get; set; }
        public virtual ICollection<EmailSentInfo> EmailSentInfos { get; set; }        
    }
}
