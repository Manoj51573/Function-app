using System;
using System.Collections.Generic;

namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class TravelLocation
    {
        public TravelLocation()
        {
            TravelRates = new HashSet<TravelRate>();
        }

        public long Id { get; set; }
        public string Location { get; set; }
        public int? RefRegionalId { get; set; }

        public virtual RefRegion RefRegional { get; set; }
        public virtual ICollection<TravelRate> TravelRates { get; set; }
    }
}
