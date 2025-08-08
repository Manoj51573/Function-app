using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;

namespace eforms_middleware.MessageBuilders;
public class RosterChangeMessageBuilder : RcrMessageBuilder
{
    private readonly ILogger<RcrMessageBuilder> _logger;
    protected override string EditPath => "roster-change-request";
    protected override string SummaryPath => $"{this.EditPath}/summary";
    protected override string FormTypeSubject => "Roster Change";
    protected override string FormTypeBody => "Close personal relationship in the workplace";


    public RosterChangeMessageBuilder(IEmployeeService employeeService, IRepository<TaskInfo> taskInfoRepo,
        ILogger<RcrMessageBuilder> logger, IConfiguration configuration, IRepository<FormPermission> formPermissionRepo,
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

            messages = action switch
            {
                FormStatus.Approved when DbModel.Modified.HasValue && DbModel.Modified.Value.Date <= past =>
                    GetApprovedMail(true),
                FormStatus.Submitted when DbModel.Modified.HasValue && DbModel.Modified.Value.Date <= past =>
                    await GetSubmittedMail(true),
                FormStatus.Escalated when DbModel.FormStatusId == (int)FormStatus.Unsubmitted =>
                    await GetCancelledEmail(),
                FormStatus.Submitted when DbModel.FormStatusId == (int)FormStatus.Unsubmitted =>
                    await GetCancelledEmail(),
                FormStatus.Submitted when DbModel.FormStatusId == (int)FormStatus.Submitted => await GetSubmittedMail(),
                FormStatus.Delegated when DbModel.FormStatusId == (int)FormStatus.Delegated => await GetDelegatedMail(),
                FormStatus.Submitted when DbModel.FormStatusId == (int)FormStatus.Endorsed => await GetEndorsedMail(),
                FormStatus.Approved when DbModel.FormStatusId == (int)FormStatus.Endorsed => await GetEndorsedMail(),
                FormStatus.Endorsed when DbModel.FormStatusId == (int)FormStatus.Completed => await GetCompletedEmail(),
                FormStatus.Endorsed => await GetEndorsedMail(),
                FormStatus.Approved when DbModel.FormStatusId == (int)FormStatus.Completed => await GetCompletedEmail(
                    ccManager: false),
                FormStatus.Approved => GetApprovedMail(),
                FormStatus.Rejected => await GetRejectedMail(),
                FormStatus.Completed when DbModel.Modified.Value.Date == DateTime.Today => await GetCompletedEmail(),
                FormStatus.Recall => await GetRecalledMail(),
                FormStatus.Escalated => await GetEscalatedMail(),
                FormStatus.Unsubmitted => new List<MailMessage>(),
                _ => throw new ArgumentOutOfRangeException()
            };

            return messages;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"Exception from {nameof(GetMessageInternalAsync)}");
            return new List<MailMessage>();
        }
    }
}

