using System;
using System.Collections.Generic;

namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class FormAttachment
    {
        public Guid Id { get; set; }
        public int FormId { get; set; }
        public int PermissionId { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public bool ActiveRecord { get; set; }
        public DateTime Created { get; set; }
        public string CreatedBy { get; set; }
        public DateTime Modified { get; set; }
        public string ModifiedBy { get; set; }

        public virtual FormInfo Form { get; set; }
        public virtual FormPermission Permission { get; set; }
    }
}
