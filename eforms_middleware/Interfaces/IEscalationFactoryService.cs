using eforms_middleware.Constants;
using eforms_middleware.Services;

namespace eforms_middleware.Interfaces;

public interface IEscalationFactoryService
{
    EscalationManagerBase GetEscalationManager(FormType formType);
}