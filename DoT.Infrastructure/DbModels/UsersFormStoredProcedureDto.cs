using System;

namespace DoT.Infrastructure.DbModels
{
    public class UsersFormStoredProcedureDto
    {
        public int AllFormsID { get; set; }
        public int FormInfoID { get; set; }
        public string FormOwnerName { get; set; }
        public string ChildFormType { get; set; }
        public int FormStatusID { get; set; }
        public string NextApprover { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public string Status { get; set; }
        public bool CanAction { get; set; }
        public bool MyForm { get; set; }
        public bool MyGroup { get; set; }
        public string FormOwnerEmail { get; set; }
        public string NextApproverEmail { get; set; }
        public byte PermissionFlag { get; set; }
        public bool CanRecall { get; set; }
    }
}