using System;
using System.Collections.Generic;

namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class TravelRate
    {
        public long Id { get; set; }
        public long? TravelLocationId { get; set; }
        public decimal? Accomodation { get; set; }
        public decimal? Food { get; set; }
        public decimal? Incidentals { get; set; }
        public decimal? Total { get; set; }
        public string RateType { get; set; }
        public decimal? Breakfast { get; set; }
        public decimal? Lunch { get; set; }
        public decimal? Dinner { get; set; }
        public int? SalaryRateId { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? ActiveDate { get; set; }

        public virtual SalaryRate SalaryRate { get; set; }
        public virtual TravelLocation TravelLocation { get; set; }
    }
}
