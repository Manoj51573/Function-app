using System;
using System.Collections.Generic;

namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class SalaryLevel
    {
        public int Id { get; set; }
        public string PositionLevel { get; set; }
        public string PositionStep { get; set; }
        public decimal GrossSalary { get; set; }
    }
}
