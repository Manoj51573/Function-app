using System;
using System.Collections.Generic;

namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class RefFormStatus
    {
        public int RefStatusesId { get; set; }
        public string Status { get; set; }
        public bool? ActiveRecord { get; set; }
    }
}
