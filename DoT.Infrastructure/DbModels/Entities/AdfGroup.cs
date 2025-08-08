using System;
using System.Collections.Generic;

namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class AdfGroup
    {
        public AdfGroup()
        {
            AdfGroupMembers = new HashSet<AdfGroupMember>();
            FormPermissions = new HashSet<FormPermission>();
        }

        public Guid Id { get; set; }
        public string GroupName { get; set; }
        public string GroupNameNormalized { get; set; }
        public string GroupEmail { get; set; }
        public string GroupEmailNormalized { get; set; }
        public DateTime? IngestedAtUtc { get; set; }
        public bool? ActiveRecord { get; set; }

        public virtual ICollection<AdfGroupMember> AdfGroupMembers { get; set; }
        public virtual ICollection<FormPermission> FormPermissions { get; set; }
    }
}
