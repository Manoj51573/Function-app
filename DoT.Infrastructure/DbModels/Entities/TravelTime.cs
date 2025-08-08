using System;
using System.Collections.Generic;

namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class TravelTime
    {
        public int Id { get; set; }
        public string TimeInterval { get; set; }
        public decimal? Rate { get; set; }
        public bool? IsStartTime { get; set; }
    }
}
