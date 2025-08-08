using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Constants;
using eforms_middleware.Constants.E29;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Services;
using eforms_middleware.Settings;
using eforms_middleware.Specifications;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace eforms_middleware.MessageBuilders;

public class E29MessageBuilder : MessageBuilder
{
    private readonly IFormHistoryService _formHistoryService;
    private readonly IRepository<FormPermission> _formPermissionRepo;

    public E29MessageBuilder(IFormHistoryService formHistoryService, IEmployeeService employeeService,
        IConfiguration configuration, IRepository<FormPermission> formPermissionRepo, IRequestingUserProvider requestingUserProvider
        , IPermissionManager permissionManager) : base(configuration, requestingUserProvider, permissionManager, employeeService)
    {
        _formHistoryService = formHistoryService;
        _formPermissionRepo = formPermissionRepo;
    }

    private async Task<List<MailMessage>> GetUnsubmittedEmailAsync()
    {
        MailMessage reminder;
        var specification = new FormPermissionSpecification(formId: DbModel.FormInfoId, addGroupMemberInfo: true, addPositionInfo: true, addUserInfo: true);
        var permissions = await _formPermissionRepo.ListAsync(specification);
        var actioningPermission = permissions.SingleOrDefault(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable);
        if (DbModel.FormOwnerEmail == null)
        {
            var response = JsonConvert.DeserializeObject<E29Form>(DbModel.Response);
            var subject = $"{response.BranchName} - TRELIS user monthly return created without an owner";
            var url = $"<a href={Helper.BaseEformsURL}/trelis-access/{DbModel.FormInfoId}>click here</a>";
            var body = string.Format(E29Templates.UNSUBMITTED_NO_OWNER_TEMPLATE, response.BranchName, url);
            reminder = new MailMessage(Helper.FromEmail, actioningPermission?.Group?.GroupEmail, subject, body);
        }
        else if (Request.FormDetails.NextApprover == E29Constants.TrelisAccessManagementGroupMail)
        {
            var groupSpecification = new FormPermissionSpecification(formId: DbModel.FormInfoId, addGroupMemberInfo: true);
            var groupPermission = await _formPermissionRepo.FirstOrDefaultAsync(groupSpecification);
            var response = JsonConvert.DeserializeObject<E29Form>(DbModel.Response);
            var subject = $"{response.BranchName} - TRELIS user monthly return is now due";
            var currentOwner = permissions.SingleOrDefault(x => x.IsOwner)?.Name;
            var body = string.Format(E29Templates.UnsubmittedAccessManagementTemplate, response.BranchName, currentOwner);
            reminder = new MailMessage(Helper.FromEmail, groupPermission.Group.GroupEmail, subject, body);
        }
        else
        {
            var subject = "Your TRELIS user monthly return is now due";
            if (DbModel.Created.Value.Date != DateTime.Today)
            {
                subject = "Reminder: " + subject;
            }
            var url = $"<a href={Helper.BaseEformsURL}/trelis-access/{DbModel.FormInfoId}>click here</a>";
            var body = string.Format(E29Templates.UnsubmittedOwnerTemplate, actioningPermission.Name, url);
            reminder = new MailMessage(Helper.FromEmail, actioningPermission.Email, subject, body);
        }

        return new List<MailMessage> { reminder };
    }
    
    private async Task<List<MailMessage>> GetDelegatedEmailAsync()
    {
        var subject = "Your TRELIS user monthly return is now due - Delegated";
        var specification = new FormPermissionSpecification(formId: DbModel.FormInfoId, permissionFlag: (byte) PermissionFlag.UserActionable, addUserInfo: true);
        var permission = await _formPermissionRepo.SingleOrDefaultAsync(specification);
        if (DbModel.Created.Value.Date == DateTime.Today.AddDays(-2))
        {
            subject = "Reminder: " + subject;
        }
        var url = $"<a href={Helper.BaseEformsURL}/trelis-access/{DbModel.FormInfoId}>click here</a>";
        var body = string.Format(E29Templates.DelegatedTemplate, permission.User.EmployeeFullName, url);
        var delegateMessage = new MailMessage(Helper.FromEmail, permission.User.EmployeeEmail, subject, body);
        return new List<MailMessage> { delegateMessage };
    }

    private async Task<List<MailMessage>> GetSubmittedEmailAsync()
    {
        var specification = new FormPermissionSpecification(formId: DbModel.FormInfoId, permissionFlag: (byte) PermissionFlag.UserActionable, addGroupMemberInfo: true);
        var permission = await _formPermissionRepo.SingleOrDefaultAsync(specification);
        var url = $"<a href={Helper.BaseEformsURL}/trelis-access/summary/{DbModel.FormInfoId}>click here</a>";
        var body = string.Format(E29Templates.SubmittedTemplate, url);
        var delegateMessage = new MailMessage(Helper.FromEmail, permission.Group.GroupEmail,
            "A completed monthly return has been received and ready for review", body);
        return new List<MailMessage> { delegateMessage };
    }

    private async Task<List<MailMessage>> GetRejectedEmailAsync()
    {
        var specification = new FormPermissionSpecification(formId: DbModel.FormInfoId, permissionFlag: (byte) PermissionFlag.UserActionable, addPositionInfo: true, addUserInfo: true);
        var permission = await _formPermissionRepo.SingleOrDefaultAsync(specification);
        var recipients = permission.PositionId.HasValue
            ? permission.Position.AdfUserPositions.ToList()
            : new List<AdfUser> { permission.User };
        var url = $"<a href={Helper.BaseEformsURL}/trelis-access/{DbModel.FormInfoId}>click here</a>";
        var rejectedMessages = new List<MailMessage>();
        foreach (var recipient in recipients)
        {
            var body = string.Format(E29Templates.RejectedTemplate, recipient.EmployeeFullName, url);
            var rejectedMessage = new MailMessage(Helper.FromEmail, recipient.EmployeeEmail,
                "Your previously submitted TRELIS user monthly return has been rejected", body);
            rejectedMessages.Add(rejectedMessage);
        }
        return rejectedMessages;
    }

    private async Task<List<MailMessage>> GetCompletedEmailAsync()
    {
        var specification =
            new GetHistoryByActionAndFormIdDescending(DbModel.FormInfoId, Enum.GetName(FormStatus.Submitted));
        var history = await _formHistoryService.GetFirstOrDefaultHistoryBySpecification(specification);
        var url = $"<a href={Helper.BaseEformsURL}/trelis-access/summary/{DbModel.FormInfoId}>click here</a>";
        var body = E29Templates.CompletedTemplate;
        var to = history.ActionBy != DbModel.FormOwnerEmail
            ? $"{history.ActionBy},{DbModel.FormOwnerEmail}"
            : DbModel.FormOwnerEmail;
        var completedMessage = new MailMessage(Helper.FromEmail, to,
            "Your TRELIS user monthly return has now been completed", body);
        return new List<MailMessage> { completedMessage };
    }

    protected override async Task<List<MailMessage>> GetMessageInternalAsync()
    {
        var messages = new List<MailMessage>();
                var action = Enum.Parse<FormStatus>(Request.FormAction);
                switch (action)
                {
                    case FormStatus.Delegate:
                        messages = await GetDelegatedEmailAsync();
                        break;
                    case FormStatus.Submitted:
                        messages = await GetSubmittedEmailAsync();
                        break;
                    case FormStatus.Rejected:
                        messages = await GetRejectedEmailAsync();
                        break;
                    case FormStatus.Completed:
                        messages = await GetCompletedEmailAsync();
                        break;
                    case FormStatus.Unsubmitted:
                        messages = await GetUnsubmittedEmailAsync();
                        break;
                    case FormStatus.Endorsed:
                    case FormStatus.Approved:
                    case FormStatus.Recall:
                    case FormStatus.IndependentReview:
                    case FormStatus.IndependentReviewCompleted:
                    case FormStatus.Requestor:
                    case FormStatus.Withdraw:
                    default:
                        break;
                }
            
                return messages;
    }
}