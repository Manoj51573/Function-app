using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.Constants.COI;
using eforms_middleware.Interfaces;
using eforms_middleware.Services;
using eforms_middleware.Specifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace eforms_middleware.MessageBuilders;

public class LeaveAmendmentMessageBuilder : LCOMessageBuilder
{
    private readonly ILogger<LeaveAmendmentMessageBuilder> _logger;
    protected override string EditPath => "leave-cash-out";
    protected override string SummaryPath => "leave-cash-out/summary";
    protected override string FormTypeSubject => "Leave Amendment and Cancellation";
    protected override string FormTypeBody => "Close personal relationship in the workplace";

    public LeaveAmendmentMessageBuilder(IEmployeeService employeeService, IRepository<TaskInfo> taskInfoRepo,   
        ILogger<LeaveAmendmentMessageBuilder> logger, IConfiguration configuration, IRepository<FormPermission> formPermissionRepo,
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
                    await GetReminderMail(),
                FormStatus.Submitted when DbModel.Modified.HasValue && DbModel.Modified.Value.Date <= past =>
                    await GetReminderMail(),
                FormStatus.Escalated when DbModel.FormStatusId == (int)FormStatus.Unsubmitted =>
                    await GetCancelledEmail(),
                FormStatus.Submitted when DbModel.FormStatusId == (int)FormStatus.Unsubmitted =>
                    await GetCancelledEmail(),
                FormStatus.Submitted when DbModel.FormStatusId == (int)FormStatus.Submitted => await GetSubmittedMail(),
                FormStatus.Delegated when DbModel.FormStatusId == (int)FormStatus.Delegated => await GetDelegatedMail(),
                FormStatus.Approved => await GetApprovedMail(),
                FormStatus.Rejected => await GetRejectedMail(),
                FormStatus.Completed when DbModel.Modified.Value.Date == DateTime.Today => await GetCompletedEmail(),
                FormStatus.Recall => await GetRecalledMail(),
                FormStatus.Escalated => await GetEscalatedMail(),   
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