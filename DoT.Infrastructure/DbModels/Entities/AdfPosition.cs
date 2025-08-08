using System;
using System.Collections.Generic;

namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class AdfPosition
    {
        public AdfPosition()
        {
            AdfUserPositions = new HashSet<AdfUser>();
            AdfUserReportsToPositions = new HashSet<AdfUser>();
            FormPermissions = new HashSet<FormPermission>();
            InverseReportsPosition = new HashSet<AdfPosition>();
        }

        public int Id { get; set; }
        public string PositionNumber { get; set; }
        public string PositionTitle { get; set; }
        public int? ReportsPositionId { get; set; }
        public string ReportsPositionNumber { get; set; }
        public string Directorate { get; set; }
        public string OccupancyType { get; set; }
        public int? ManagementTier { get; set; }
        public string PositionStatus { get; set; }
        public DateTime? PositionCreatedDate { get; set; }
        public DateTime? IngestedAtUtc { get; set; }
        public bool? ActiveRecord { get; set; }
        public string Classification { get; set; }
        public string Department { get; set; }
        public string Branch { get; set; }
        public string Section { get; set; }
        public string Unit { get; set; }
        public virtual AdfPosition ReportsPosition { get; set; }
        public virtual ICollection<AdfUser> AdfUserPositions { get; set; }
        public virtual ICollection<AdfUser> AdfUserReportsToPositions { get; set; }
        public virtual ICollection<FormPermission> FormPermissions { get; set; }
        public virtual ICollection<AdfPosition> InverseReportsPosition { get; set; }
    }
}
