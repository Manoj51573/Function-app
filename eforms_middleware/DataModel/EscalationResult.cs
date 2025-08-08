using DoT.Infrastructure.DbModels.Entities;

namespace eforms_middleware.DataModel;

public class EscalationResult
{
    public FormInfo UpdatedForm { get; set; }
    public bool DoesEscalate { get; set; }
    public int NotifyDays { get; set; } = 3;
    public int EscalationDays { get; set; } = 5;
    public FormPermission PermissionUpdate { get; set; }
    public StatusBtnData StatusBtnData { get; set; }
}