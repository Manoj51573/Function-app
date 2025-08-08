using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using DoT.Infrastructure.DbModels.Entities;
using DoT.Infrastructure.Interfaces;
using eforms_middleware.Constants;
using eforms_middleware.DataModel;
using eforms_middleware.Interfaces;
using eforms_middleware.Services;
using eforms_middleware.Specifications;
using Microsoft.Extensions.Configuration;

namespace eforms_middleware.MessageBuilders;

public abstract class MessageBuilder
{
    protected IEmployeeService EmployeeService { get; }
    private readonly IRequestingUserProvider _requestingUserProvider;
    private readonly IPermissionManager _permissionManager;
    private readonly string _baseUrl;
    protected readonly string FromEmail;
    protected FormInfo DbModel { get; private set; }
    protected FormInfoUpdate Request { get; private set; }
    protected virtual string EditPath => "";
    protected virtual string SummaryPath => "";
    protected string EditHref => $"<a href={_baseUrl}/{EditPath}/{DbModel.FormInfoId}>click here</a>";
    protected string SummaryHref => $"<a href={_baseUrl}/{SummaryPath}/{DbModel.FormInfoId}>click here</a>";
    private List<MailMessage> Messages { get; set; }
    protected AdfGroup ActioningGroup { get; private set; }
    protected List<FormPermission> CurrentApprovers { get; private set; }
    protected FormPermission FormOwnerPermission { get; private set; }
    protected IList<FormPermission> Permissions { get; private set; }
    protected IUserInfo RequestingUser { get; private set; }
    protected string EditHrefHg => $"click <a href={_baseUrl}/{EditPath}/{DbModel.FormInfoId}>here</a>";
    protected string SummaryHrefHg => $"click <a href={_baseUrl}/{SummaryPath}/{DbModel.FormInfoId}>here</a>";

    protected MessageBuilder(IConfiguration configuration, IRequestingUserProvider requestingUserProvider, IPermissionManager permissionManager, IEmployeeService employeeService)
    {
        EmployeeService = employeeService;
        _requestingUserProvider = requestingUserProvider;
        _permissionManager = permissionManager;
        _baseUrl = configuration.GetValue<string>("BaseEformsUrl");
        FromEmail = configuration.GetValue<string>("FromEmail");
    }

    public void  Initialize(FormInfo dbModel, FormInfoUpdate request)
    {
        DbModel = dbModel;
        Request = request;
    }

    private static string WrapBody(string body)
    {
        var content = $"<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div>" +
                      $"<br/>" + body + $"<br/>";
        return content;
    }

    public async Task<List<MailMessage>> GetMessagesAsync()
    {
        RequestingUser = await _requestingUserProvider.GetRequestingUser();
        Permissions = new List<FormPermission>();
        var specification = new FormPermissionSpecification(DbModel.FormInfoId);
        Permissions = await _permissionManager.GetPermissionsBySpecificationAsync(specification);
        FormOwnerPermission = Permissions.Single(x => x.IsOwner);
        CurrentApprovers = Permissions.Where(x => x.PermissionFlag == (byte)PermissionFlag.UserActionable).ToList();
        ActioningGroup = CurrentApprovers.Any() ? CurrentApprovers.Single().Group : null;
        await BuildMessagesAsync();
        return Messages;
    }

    private async Task BuildMessagesAsync()
    {
        Messages = await GetMessageInternalAsync();

        foreach (var message in Messages)
        {
            message.Body = WrapBody(message.Body);
        }
    }
    
    protected abstract Task<List<MailMessage>> GetMessageInternalAsync();
}