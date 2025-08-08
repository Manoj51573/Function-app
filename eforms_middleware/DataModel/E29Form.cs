using System;
using System.Collections.Generic;

namespace eforms_middleware.DataModel
{
    public class E29Form
    {
        public int BranchId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string BranchName { get; set; }
        public string RejectionReason { get; set; }
        public IEnumerable<TeamMember> Users { get; set; } = new List<TeamMember>();
        public int? PreviousMonthFormId { get; set; }
    }

    public class TeamMember
    {
        public string Email { get; set; }
        public string Name { get; set; }
        public string PositionTitle { get; set; }
        public int? RemovedReason { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string TransferBranch { get; set; }
    }
}