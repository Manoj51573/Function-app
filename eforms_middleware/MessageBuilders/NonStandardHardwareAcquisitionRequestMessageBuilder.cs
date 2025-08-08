using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace eforms_middleware.MessageBuilders;

public class NonStandardHardwareAcquisitionRequestMessageBuilder : NSHAMessageBuilder
{
    private readonly ILogger<NonStandardHardwareAcquisitionRequestMessageBuilder> _logger;
    protected override string EditPath => "non-standard-hardware-acquisition-request";
    protected override string SummaryPath => $"{this.EditPath}/summary";
    protected override string FormTypeSubject => "Non-Standard Hardware Acquisition";
    protected override string FormTypeBody => "";


    public NonStandardHardwareAcquisitionRequestMessageBuilder(IEmployeeService employeeService, IRepository<TaskInfo> taskInfoRepo,
        ILogger<NonStandardHardwareAcquisitionRequestMessageBuilder> logger, IConfiguration configuration, IRepository<FormPermission> formPermissionRepo,
        IFormHistoryService formHistoryService, IRequestingUserProvider requestingUserProvider, IPermissionManager permissionManager)
        : base(configuration, requestingUserProvider, permissionManager, employeeService)
    {
        _logger = logger;
    }
    protected override async Task<List<MailMessage>> GetMessageInternalAsync()
    {
        try
        {
            _logger.LogInformation("Processing mail request for form {0}", DbModel.FormInfoId);
            var past = DateTime.Today.AddDays(-3);
            _logger.LogInformation("Date for reminders set as {0}", past);
            var messages = new List<MailMessage>();
            var action = Enum.Parse<FormStatus>(Request.FormAction);

            switch (action)
            {
                case FormStatus.Unsubmitted:
                    break;
                case FormStatus.Submitted when DbModel.FormStatusId == (int)FormStatus.Submitted:
                    messages = await GetSubmittedMail();
                    break;
                case FormStatus.Approved:
                    messages = await GetApprovedMail();
                    break;
                case FormStatus.Rejected:
                    messages = await GetRejectedMail();
                    break;
                case FormStatus.Completed:
                    messages = await GetCompletedEmail();
                    break;
                case FormStatus.Recalled or FormStatus.Recall:
                    break;
                default:
                   break;
            }

            return messages;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Exception from {nameof(GetMessageInternalAsync)}");
            return new List<MailMessage>();
        }
    }
}