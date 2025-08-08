using DoT.Infrastructure.Interfaces;
using eforms_middleware.Constants;
using System;

namespace eforms_middleware.DataModel;

public class NotificationInfo
{
    public FormStatus EmailType { get; }
    public string Type { get; }
    public string ToEmail { get; }
    public Guid? ToGuid { get; }
    public IUserInfo ToUser { get; }

    public NotificationInfo(string type, string toEmail = null, Guid? toGuid = null)
    {
        Type = type;
        ToEmail = toEmail;
        ToGuid = toGuid;
    }

    public NotificationInfo(FormStatus type, string toEmail = null, Guid? toGuid = null, IUserInfo toUser = null)
    {
        EmailType = type;
        ToEmail = toEmail;
        ToGuid = toGuid;
        ToUser = toUser;
    }
    public NotificationInfo(string type, IUserInfo toUser = null)
    {
        Type = type;
        ToUser = toUser;
    }

}