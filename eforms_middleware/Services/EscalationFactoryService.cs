using eforms_middleware.Constants;
using eforms_middleware.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace eforms_middleware.Services;

public class EscalationFactoryService : IEscalationFactoryService
{
    private readonly IServiceProvider _serviceProvider;

    public EscalationFactoryService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public EscalationManagerBase GetEscalationManager(FormType formType)
    {
        EscalationManagerBase manager = formType switch
        {
            FormType.CoI_CPR => _serviceProvider.GetRequiredService<CprEscalationManager>(),
            FormType.CoI_GBC => _serviceProvider.GetRequiredService<GbcEscalationManager>(),
            FormType.CoI_Other => _serviceProvider.GetRequiredService<CoiOtherEscalationService>(),
            FormType.LcR_LAC or
            FormType.LcR_MA or
            FormType.LcR_PLA or
            FormType.LcR_DSA or
            FormType.LcR_LWP or
            FormType.LcR_PLS or
            FormType.LcR_LCO => _serviceProvider.GetRequiredService<LeaveFormsEscalationManager>(),
            FormType.TPAC_SDTC or
            FormType.TPAC_MDTC or
            FormType.TPAC_CC or
            FormType.TPAC_RC => _serviceProvider.GetRequiredService<TravelEscalationService>(),            
            FormType.ACR_AHC or
            FormType.ACR_CTS or
            FormType.ACR_MVA or
            FormType.ACR_OAC or
            FormType.ACR_OHC or
            FormType.ACR_OTC or
            FormType.ACR_PSA or
            FormType.ACR_SGA => _serviceProvider.GetRequiredService<AllowanceClaimsFormsEscalationServiceManager>(),
            FormType.Hma => _serviceProvider.GetRequiredService<HomeGaragingFormsEscalationManager>(),
            FormType.PSYI or
            FormType.PHYI or
            FormType.HNM => _serviceProvider.GetRequiredService<WHSIREscalationService>(),
            _ => throw new ArgumentOutOfRangeException(nameof(formType), formType, null)
        };

        // Could return null should log but not stop processes
        return manager;
    }
}