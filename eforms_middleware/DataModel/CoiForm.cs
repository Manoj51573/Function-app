using eforms_middleware.Constants;

namespace eforms_middleware.DataModel;

interface ICoiForm<T> where T : CoiEndorsementForm
{
    T FinalApprovalForm { get; set; }
}

public class CoIForm : ICoiForm<CoiEndorsementForm>
{
    public CoiManagerForm ManagerForm { get; set; }
    public CoiEndorsementForm EndorsementForm { get; set; }
    public CoiEndorsementForm FinalApprovalForm { get; set; }
}

public class CoiManagerForm
{
    public ConflictOfInterest.ConflictType? ConflictType { get; set; }
    public string ActionsTaken { get; set; }
    public bool? IsDeclarationAcknowledged { get; set; }
    public string AdditionalComments { get; set; }
    public string RejectionReason { get; set; }
}

public class CoiEndorsementForm
{
    public string AdditionalComments { get; set; }
    public string RejectionReason { get; set; }
}

public class CoiRejection
{
    public string RejectionReason { get; set; }
}