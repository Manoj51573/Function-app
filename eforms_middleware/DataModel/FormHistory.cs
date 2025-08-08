using System;

namespace eforms_middleware.DataModel
{
    public class FormHistoryInsertModel
    {
        public int? FormHistoryID { get; set; }
        public int? AllFormsID { get; set; }
        public int? FormInfoID { get; set; }
        public DateTime Created { get; set; }
        public string ActionType { get; set; }
        public string ActionBy { get; set; }
        public string FormStatusID { get; set; }
        public string AditionalComments { get; set; }
        public bool ActiveRecord { get; set; }
    }

    public class FormHistoryDto
    {
        public int FormHistoryID { get; set; }
        public int AllFormsID { get; set; }
        public int FormInfoID { get; set; }
        public DateTime? Created { get; set; }
        public string ActionType { get; set; }
        public string ActionBy { get; set; }
        public string FormStatus { get; set; }
        public string AdditionalComments { get; set; }
        public bool ActiveRecord { get; set; }
        public string RejectedReason { get; set; }
        public bool HasRejectedReason
        {
            get
            {
                return !string.IsNullOrEmpty(RejectedReason);
            }
        }
        public string FormOwner { get; set; }
        public string FormOwnerEmail { get; set; }
        public string ActionByPosition { get; set; }
    }

    public class FormHistoryGetModel
    {
        public string FormHistoryID { get; set; }
        public string AllFormsID { get; set; }
        public string FormInfoID { get; set; }
        public string Created { get; set; }
        public string ActionType { get; set; }
        public string ActionBy { get; set; }
        public string FormStatusID { get; set; }
        public string AdditionalComments { get; set; }
        public bool ActiveRecord { get; set; }
    }

    public class AllFormHistoryModel
    {
        public int? FormHistoryID { get; set; }
        public int? AllFormsID { get; set; }
        public int? FormInfoID { get; set; }
        public DateTime Created { get; set; }
        public string ActionType { get; set; }
        public string ActionBy { get; set; }
        public string FormStatusID { get; set; }
        public string AditionalComments { get; set; }
        public bool ActiveRecord { get; set; }
    }
}
