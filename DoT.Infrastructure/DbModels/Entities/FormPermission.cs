using System;
using System.Collections.Generic;

namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class FormPermission
    {
        public FormPermission()
        {
            FormAttachments = new HashSet<FormAttachment>();
            WorkflowBtns = new HashSet<WorkflowBtn>();
        }

        public int Id { get; set; }
        public int FormId { get; set; }
        public byte PermissionFlag { get; set; }
        public Guid? UserId { get; set; }
        public int? PositionId { get; set; }
        public Guid? GroupId { get; set; }
        public bool IsOwner { get; set; }

        public virtual FormInfo Form { get; set; }
        public virtual AdfGroup Group { get; set; }
        public virtual AdfPosition Position { get; set; }
        public virtual AdfUser User { get; set; }
        public virtual ICollection<FormAttachment> FormAttachments { get; set; }
        public virtual ICollection<WorkflowBtn> WorkflowBtns { get; set; }
    }
}
