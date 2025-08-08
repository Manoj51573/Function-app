using System;
using System.Collections.Generic;

namespace eforms_middleware.DataModel
{
    public class RosterChangeInsertModel
    {
        public FormDetails FormDetails { get; set; }
        public string FormAction { get; set; }
        public string RosterChangeRequestType { get; set; }
        public string AdditionalInfo { get; set; }
        public FormDetailsRequest ToFormDetailRequest()
        {
            return new FormDetailsRequest
            {
                AllFormsId = FormDetails.AllFormsID,
                FormInfoId = FormDetails.FormInfoID,
                NextApprover = FormDetails.NextApprover,
                Response = FormDetails.Response
            };
        }
    }

    public class RosterChangeModel
    {
        public RosterChange RosterChangeRequestGroup { get; set; }
        public CurrentRoster CurrentRosterGroup { get; set; }
        public NewRoster NewRosterGroup { get; set; }
        public SupportingInfo SupportingInfoGroup { get; set; }
        public string ReasonForDecision { get; set; }
    }

    public class RosterChange
    {
        public string OtherRequestorFlag { get; set; }
        public IList<UserIdentifier> otherRequestor { get; set; } = new List<UserIdentifier>();
    }
    public class CurrentRoster
    {
        public string IsThisNineDaysRoster { get; set; }
        public CurrentRosterFirstWeek CurrentRosterGroupFirstWeek { get; set; }
        public CurrentRosterSecondWeek CurrentRosterGroupSecondWeek { get; set; }
        public decimal TotalHoursPerFornight { get; set; }
    }
    public class NewRoster
    {
        public string IsThisNineDaysRoster { get; set; }
        public string EmployeeActingDiffPosition { get; set; }
        public string RosterApplyToActingRole { get; set; }
        public string IsThisPermanentChanged { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public NewRosterFirstWeek NewRosterGroupFirstWeek { get; set; }
        public NewRosterSecondWeek NewRosterGroupSecondWeek { get; set; }
        public decimal TotalHoursPerFornight { get; set; }
        public string ReasonForChange { get; set; }
    }

    public class SupportingInfo
    {
        public IList<UserIdentifier> AdditionalNotification { get; set; } = new List<UserIdentifier>();
        public IList<AttachmentResult> Attachments { get; set; } = new List<AttachmentResult>();
    }
    public class CurrentRosterFirstWeek
    {
        public decimal? CurrentRosterGroupFirstWeekSunday { get; set; }
        public decimal? CurrentRosterGroupFirstWeekMonday { get; set; }
        public decimal? CurrentRosterGroupFirstWeekTuesday { get; set; }
        public decimal? CurrentRosterGroupFirstWeekWednesday { get; set; }
        public decimal? CurrentRosterGroupFirstWeekThursday { get; set; }
        public decimal? CurrentRosterGroupFirstWeekFriday { get; set; }
        public decimal? CurrentRosterGroupFirstWeekSaturday { get; set; }

    }
    public class CurrentRosterSecondWeek
    {
        public decimal? CurrentRosterGroupSecondWeekSunday { get; set; }
        public decimal? CurrentRosterGroupSecondWeekMonday { get; set; }
        public decimal? CurrentRosterGroupSecondWeekTuesday { get; set; }
        public decimal? CurrentRosterGroupSecondWeekWednesday { get; set; }
        public decimal? CurrentRosterGroupSecondWeekThursday { get; set; }
        public decimal? CurrentRosterGroupSecondWeekFriday { get; set; }
        public decimal? CurrentRosterGroupSecondWeekSaturday { get; set; }
    }
    public class NewRosterFirstWeek
    {
        public decimal? NewRosterGroupFirstWeekSunday { get; set; }
        public decimal? NewRosterGroupFirstWeekMonday { get; set; }
        public decimal? NewRosterGroupFirstWeekTuesday { get; set; }
        public decimal? NewRosterGroupFirstWeekWednesday { get; set; }
        public decimal? NewRosterGroupFirstWeekThursday { get; set; }
        public decimal? NewRosterGroupFirstWeekFriday { get; set; }
        public decimal? NewRosterGroupFirstWeekSaturday { get; set; }
    }
    public class NewRosterSecondWeek
    {
        public decimal? NewRosterGroupSecondWeekSunday { get; set; }
        public decimal? NewRosterGroupSecondWeekMonday { get; set; }
        public decimal? NewRosterGroupSecondWeekTuesday { get; set; }
        public decimal? NewRosterGroupSecondWeekWednesday { get; set; }
        public decimal? NewRosterGroupSecondWeekThursday { get; set; }
        public decimal? NewRosterGroupSecondWeekFriday { get; set; }
        public decimal? NewRosterGroupSecondWeekSaturday { get; set; }

    }
}
