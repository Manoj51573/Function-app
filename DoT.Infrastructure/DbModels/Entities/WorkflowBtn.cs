using System;
using System.Collections.Generic;

namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class WorkflowBtn
    {
        public long Id { get; set; }
        public int PermisionId { get; set; }
        public int StatusId { get; set; }
        public bool? IsActive { get; set; }
        public int FormId { get; set; }
        public string BtnText { get; set; }
        public string FormSubStatus { get; set; }
        public bool? HasDeclaration { get; set; }

        public virtual FormInfo Form { get; set; }
        public virtual FormPermission Permision { get; set; }
    }
}
