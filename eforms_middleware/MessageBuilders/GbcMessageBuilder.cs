using eforms_middleware.Constants;
using eforms_middleware.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;

namespace eforms_middleware.MessageBuilders;

public class GbcMessageBuilder : CoiMessageBuilder
{
    private readonly ILogger<GbcMessageBuilder> _logger;
    protected override string EditPath => "coi-board-committee";
    protected override string SummaryPath => "coi-board-committee/summary";
    protected override string FormTypeSubject => "Government board/committee";
    protected override string FormTypeBody => "Government board/committee";

    public GbcMessageBuilder(ILogger<GbcMessageBuilder> logger, IConfiguration configuration,
        IRequestingUserProvider requestingUserProvider, IPermissionManager permissionManager, IEmployeeService employeeService)
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
            messages = action switch
            {
                FormStatus.Submitted when DbModel.Modified.HasValue && DbModel.Modified.Value.Date <= past =>
                    await GetReminderMail(),
                FormStatus.Approved when DbModel.Modified.HasValue && DbModel.Modified.Value.Date <= past =>
                    await GetApprovedMail(true),
                FormStatus.Submitted when DbModel.FormStatusId == (int)FormStatus.Unsubmitted =>
                    await GetCancelledEmail(),
                FormStatus.Submitted when DbModel.FormStatusId == (int)FormStatus.Endorsed => await GetEndorsedMail(),
                FormStatus.Submitted when DbModel.FormStatusId == (int)FormStatus.Submitted => await GetSubmittedMail(),
                FormStatus.Approved when DbModel.FormStatusId == (int)FormStatus.Approved => await GetApprovedMail(),
                FormStatus.Approved when DbModel.FormStatusId == (int)FormStatus.Endorsed => await GetEndorsedMail(),
                FormStatus.Approved when DbModel.FormStatusId == (int)FormStatus.Completed => await GetCompletedMail(
                    ccManager: false),
                FormStatus.Endorsed when DbModel.FormStatusId == (int)FormStatus.Completed => await GetCompletedMail(),
                FormStatus.Endorsed => await GetEndorsedMail(),
                FormStatus.Completed => await GetCompletedMail(),
                FormStatus.Rejected => await GetRejectedMail(),
                FormStatus.Recall => await GetRecalledMail(),
                FormStatus.Escalated => await GetEscalatedMail(),
                _ => throw new ArgumentOutOfRangeException()
            };

            return messages;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Exception from {nameof(GetSubmittedMail)}");
            return new List<MailMessage>();
        }
    }
}