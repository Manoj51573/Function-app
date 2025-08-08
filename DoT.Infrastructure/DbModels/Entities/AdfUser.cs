using System;
using System.Collections.Generic;

namespace DoT.Infrastructure.DbModels.Entities
{
    public partial class AdfUser
    {
        public AdfUser()
        {
            AdfGroupMembers = new HashSet<AdfGroupMember>();
            FormPermissions = new HashSet<FormPermission>();
        }

        public int? Id { get; set; }
        public int? PositionId { get; set; }
        public bool IsSubstantive { get; set; }
        public string EmployeeNumber { get; set; }
        public string EmployeeFirstName { get; set; }
        public string EmployeeSecondName { get; set; }
        public string EmployeeSurname { get; set; }
        public string EmployeeNameNormalized { get; set; }
        public string EmployeePreferredName { get; set; }
        public string EmployeePreferredNameNormalized { get; set; }
        public string EmployeeTitle { get; set; }
        public string EmployeeGender { get; set; }
        public DateTime? EmployeeStartDate { get; set; }
        public DateTime? EmployeeTerminationDate { get; set; }
        public string EmployeeEmail { get; set; }
        public string EmployeeEmailNormalized { get; set; }
        public int? ReportsToPositionId { get; set; }
        public string UserId { get; set; }
        public string LeaveFlag { get; set; }
        public DateTime? IngestedAtUtc { get; set; }
        public bool? ActiveRecord { get; set; }
        public Guid ActiveDirectoryId { get; set; }

        public virtual AdfPosition Position { get; set; }
        public virtual AdfPosition ReportsToPosition { get; set; }
        public virtual ICollection<AdfGroupMember> AdfGroupMembers { get; set; }
        public virtual ICollection<FormPermission> FormPermissions { get; set; }
    }
}
