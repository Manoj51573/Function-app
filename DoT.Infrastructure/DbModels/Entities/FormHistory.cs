using System;
using System.Collections.Generic;

namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class FormHistory
    {
        public int FormHistoryId { get; set; }
        public int AllFormsId { get; set; }
        public int FormInfoId { get; set; }
        public DateTime? Created { get; set; }
        public string ActionType { get; set; }
        public string ActionBy { get; set; }
        public string ActionByPosition { get; set; }
        public string GroupName { get; set; }
        public int FormStatusId { get; set; }
        public string AditionalComments { get; set; }
        public bool? ActiveRecord { get; set; }
        public string RejectedReason { get; set; }
    }
}
