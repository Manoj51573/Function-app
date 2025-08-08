using System;
using System.Collections.Generic;

namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class CampRate
    {
        public int Id { get; set; }
        public string CampId { get; set; }
        public string Type { get; set; }
        public decimal? Rate { get; set; }
        public bool? IsCookProvided { get; set; }
        public string ParallelType { get; set; }
    }
}
