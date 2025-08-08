using System;
using System.Collections.Generic;

namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class RefDirectorate
    {
        public int RefDirectoratesId { get; set; }
        public string Directorate { get; set; }
        public bool? ActiveRecord { get; set; }
    }
}
