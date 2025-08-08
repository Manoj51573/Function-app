namespace eforms_middleware.Constants.E29;

public static partial class E29Templates
{
    public static string UnsubmittedOwnerTemplate = 
        "<div>Dear {0}</div><br/>" +
        "<br/>" +
        "<div>A monthly return is to be completed and submitted for the following month, please ensure all current " +
        "users are recorded including any staff currently on or commencing leave.</div><br/>" +
        "<br/>" +
        "<div>Failure to do so may result in TRELIS access being removed.</div><br/>" +
        "<br/>" +
        "<div>Please {1} to access the form.</div><br/>" +
        "<br/>" +
        "<div>Kind Regards</div><br/>" +
        "<br/>" +
        "<div>Access Management</div>";
    
    public static string UnsubmittedAccessManagementTemplate =
        "<div>The monthly return for {0} assigned to {1} is overdue.</div><br/>" +
        "<br/>" +
        "<div>Please remind them to submit the form as failure to do so may result in TRELIS access being removed.</div><br/>";
    
    public const string UNSUBMITTED_NO_OWNER_TEMPLATE =
        "<div>The monthly return for {0} could not find a user to auto set as the owner.</div><br/>" +
        "<br/>" +
        "<div>Please {1} to access the form and delegate to a user.</div><br/>";
}