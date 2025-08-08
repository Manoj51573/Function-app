using System;
using System.Collections.Generic;

namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class AdfGroupMember
    {
        public Guid MemberId { get; set; }
        public Guid GroupId { get; set; }
        public bool? ActiveRecord { get; set; }
        public DateTime? IngestedAtUtc { get; set; }

        public virtual AdfGroup Group { get; set; }
        public virtual AdfUser Member { get; set; }
    }
}
