using System;
using System.Collections.Generic;
using CoIConflictType = eforms_middleware.Constants.ConflictOfInterest.ConflictType;

namespace eforms_middleware.DataModel
{
    public class ClosePersonalRelationship : CoIForm
    {
        public CprEmployeeForm EmployeeForm { get; set; } = new();
    }

    public class CprEmployeeForm
    {
        public DateTime? From { get; set; }
        public CoIConflictType? ConflictType { get; set; }
        public string Description { get; set; }
        public string ProposedPlan { get; set; }
        public bool? IsDeclarationAcknowledged { get; set; }
        public string AdditionalComments { get; set; }
        public bool IsRequestOnBehalf { get; set; }
        public IList<UserIdentifier> requestOnBehalf { get; set; } = new List<UserIdentifier>();
    }
}