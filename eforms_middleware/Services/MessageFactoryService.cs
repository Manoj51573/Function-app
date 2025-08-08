using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.MessageBuilders;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace eforms_middleware.Services;

public class MessageFactoryService : IMessageFactoryService
{
    private readonly IFormInfoService _formInfoService;
    private readonly IFormEmailService _formEmailService;
    private readonly IServiceProvider _serviceProvider;

    public MessageFactoryService(IFormInfoService formInfoService, IFormEmailService formEmailService, IServiceProvider serviceProvider)
    {
        _formInfoService = formInfoService;
        _formEmailService = formEmailService;
        _serviceProvider = serviceProvider;
    }

    public async Task SendEmailAsync(FormInfo dbRecord, FormInfoUpdate request)
    {
        var messageBuilder = GetMessageBuilder(dbRecord);
        messageBuilder.Initialize(dbRecord, request);
        var messages = await messageBuilder.GetMessagesAsync();
        _formEmailService.SendEmail(messages);
    }

    private MessageBuilder GetMessageBuilder(FormInfo dbRecord)
    {
        var formType = (FormType)dbRecord.AllFormsId;
        switch (formType)
        {
            case FormType.CoI_CPR:
                return _serviceProvider.GetRequiredService<CprMessageBuilder>();
            case FormType.e29:
                return _serviceProvider.GetRequiredService<E29MessageBuilder>();
            case FormType.CoI_GBC:
                return _serviceProvider.GetRequiredService<GbcMessageBuilder>();
            case FormType.CoI_Other:
                return _serviceProvider.GetRequiredService<CoIOtherMessageBuilder>();
            case FormType.LcR_LAC:
                return _serviceProvider.GetRequiredService<LeaveAmendmentMessageBuilder>();
            case FormType.LcR_MA:
                return _serviceProvider.GetRequiredService<MaternityLeaveMessageBuilder>();
            case FormType.LcR_DSA:
            case FormType.LcR_PLA:
                return _serviceProvider.GetRequiredService<PurchasedLeaveMessageBuilder>();
            case FormType.LcR_LCO:
                return _serviceProvider.GetRequiredService<LeaveCashOutMessageBuilder>();
            case FormType.LcR_LWP:
                return _serviceProvider.GetRequiredService<LeaveWithoutPayMessageBuilder>();
            case FormType.LcR_PLS:
                return _serviceProvider.GetRequiredService<ProRataLeaveMessageBuilder>();
            case FormType.TPAC_SDTC or
                FormType.TPAC_MDTC or
                FormType.TPAC_CC or
                FormType.TPAC_RC:
                return _serviceProvider.GetRequiredService<TravelMessageBuilder>();
            case FormType.ACR_AHC:
                return _serviceProvider.GetRequiredService<AdditionalHoursClaimsMessageBuilder>();
            case FormType.ACR_CTS:
                return _serviceProvider.GetRequiredService<CasualTimesheetsMessageBuilder>();
            case FormType.ACR_MVA:
                return _serviceProvider.GetRequiredService<MotorVehicleAllowanceClaimsMessageBuilder>();
            case FormType.ACR_OAC:
                return _serviceProvider.GetRequiredService<OtherAllowanceClaimsMessageBuilder>();
            case FormType.ACR_OHC:
                return _serviceProvider.GetRequiredService<OutofHoursContactClaimsMessageBuilder>();
            case FormType.ACR_OTC:
                return _serviceProvider.GetRequiredService<OvertimeClaimsMessageBuilder>();
            case FormType.ACR_PSA:
                return _serviceProvider.GetRequiredService<PenaltyShiftAllowanceClaimsMessageBuilder>();
            case FormType.ACR_SGA:
                return _serviceProvider.GetRequiredService<SeaGoingAllowanceClaimsMessageBuilder>();
            case FormType.CoI:
            case FormType.Rcr:
                return _serviceProvider.GetRequiredService<RosterChangeMessageBuilder>();
            case FormType.Hma:
                return _serviceProvider.GetRequiredService<HomeGaragingMessageBuilder>();
            case FormType.PHYI or
                FormType.PSYI or
                FormType.HNM:
                return _serviceProvider.GetRequiredService<WHSIRMessageBuilder>();
            case FormType.NSHAR:
                return _serviceProvider.GetRequiredService<NonStandardHardwareAcquisitionRequestMessageBuilder>();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}