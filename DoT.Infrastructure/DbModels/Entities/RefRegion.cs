using System;
using System.Collections.Generic;

namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class RefRegion
    {
        public RefRegion()
        {
            TravelLocations = new HashSet<TravelLocation>();
        }

        public int Id { get; set; }
        public string ParallelType { get; set; }
        public string RegionalArea { get; set; }
        public string RegionalOpsPositionNumber { get; set; }
        public string RegionalOpsPositionTitle { get; set; }

        public virtual ICollection<TravelLocation> TravelLocations { get; set; }
    }
}
