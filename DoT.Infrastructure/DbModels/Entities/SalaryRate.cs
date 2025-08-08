using System;
using System.Collections.Generic;

namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class SalaryRate
    {
        public SalaryRate()
        {
            TravelRates = new HashSet<TravelRate>();
        }

        public int Id { get; set; }
        public string Rate { get; set; }
        public decimal? From { get; set; }
        public decimal? To { get; set; }

        public virtual ICollection<TravelRate> TravelRates { get; set; }
    }
}
