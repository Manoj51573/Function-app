using System;
using System.Collections.Generic;

namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class FormInfo
    {
        public FormInfo()
        {
            FormAttachments = new HashSet<FormAttachment>();
            FormPermissions = new HashSet<FormPermission>();
            TaskInfos = new HashSet<TaskInfo>();
            WorkflowBtns = new HashSet<WorkflowBtn>();
        }

        public int FormInfoId { get; set; }
        public int AllFormsId { get; set; }
        public string ChildFormType { get; set; }
        public int FormItemId { get; set; }
        public string FormOwnerName { get; set; }
        public string FormOwnerEmail { get; set; }
        public string FormOwnerDirectorate { get; set; }
        public string FormOwnerPositionNo { get; set; }
        public string FormOwnerEmployeeNo { get; set; }
        public string FormOwnerPositionTitle { get; set; }
        public int FormStatusId { get; set; }
        public string FormSubStatus { get; set; }
        public string InitiatedForName { get; set; }
        public string InitiatedForEmail { get; set; }
        public string InitiatedForDirectorate { get; set; }
        public string Response { get; set; }
        public string FormApprovers { get; set; }
        public string FormReaders { get; set; }
        public DateTime? Created { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? Modified { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string Pdfguid { get; set; }
        public bool? ActiveRecord { get; set; }
        public string NextApprovalLevel { get; set; }
        public string NextApprover { get; set; }

        public virtual AllForm AllForms { get; set; }
        public virtual ICollection<FormAttachment> FormAttachments { get; set; }
        public virtual ICollection<FormPermission> FormPermissions { get; set; }
        public virtual ICollection<TaskInfo> TaskInfos { get; set; }
        public virtual ICollection<WorkflowBtn> WorkflowBtns { get; set; }
    }
}
