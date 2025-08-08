using System;
using System.Collections.Generic;

namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class RefLocation
    {
        public int RefLocationId { get; set; }
        public string LocationTitle { get; set; }
        public int? LocationNumber { get; set; }
        public string OfficeLocationName { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostCode { get; set; }
        public bool? ActiveRecord { get; set; }
    }
}
