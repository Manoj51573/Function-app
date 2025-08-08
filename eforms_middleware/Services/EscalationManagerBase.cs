using System.Threading.Tasks;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;

namespace eforms_middleware.Services;

public abstract class EscalationManagerBase : IEscalationManager
{
    public abstract Task<EscalationResult> EscalateFormAsync(FormInfo originalForm);
}