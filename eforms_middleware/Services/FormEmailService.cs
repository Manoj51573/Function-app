using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using eforms_middleware.Settings;
using DoT.Infrastructure;
using DoT.Infrastructure.DbModels.Entities;
using eforms_middleware.Interfaces;
using System.Threading.Tasks;


namespace eforms_middleware.GetMasterData
{
    public class FormEmailService : IFormEmailService
    {
        private readonly ILogger<FormEmailService> _log;
        private readonly IRepository<EmailSentInfo> _emailSentInfo;
        private readonly IRepository<FormInfo> _formInfo;
        private readonly SmtpClient _smtp;

        public FormEmailService(ILogger<FormEmailService> log
            , SmtpClient smtpClient
            , IRepository<EmailSentInfo> emailSentInfo
            , IRepository<FormInfo> formInfo)
        {
            _log = log;
            _emailSentInfo = emailSentInfo;
            _smtp = smtpClient;
            _formInfo = formInfo;
            _smtp.UseDefaultCredentials = false;
            _smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
        }

        public async Task SendEmail(int formInfoId
            , string toEmail
            , string subject
            , string body
            , string ccEmail = "")
        {
            MailMessage message = new MailMessage();
            var result = new JsonResult(null);
            try
            {
                if (!string.IsNullOrEmpty(toEmail))
                {
                    var content = $"<div style=\"background-color:#5393CF;text-align:center;color:white;\">eForms Notification</div>" +
                            $"<br/>" + body + $"<br/>";

                    if (!string.IsNullOrEmpty(ccEmail))
                    {
                        var ccInfo = ccEmail;
                        foreach (var cc in ccInfo.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            message.CC.Add(cc);
                        }
                    }

                    message.From = new MailAddress(Helper.FromEmail);
                    foreach (var email in toEmail.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        message.To.Add(new MailAddress(email));
                    }

                    message.Subject = subject;
                    message.IsBodyHtml = true;
                    message.Body = content;
                    _smtp.Send(message);
                    await SaveEmailSentInfo(formInfoId, body, subject, Helper.FromEmail, toEmail, ccEmail, "");
                }
            }
            catch (Exception e)
            {
                _log.LogError(e, e.Message);
                result.Value = new
                {
                    error = e.Message
                };
            }
        }

        public void SendEmail(IList<MailMessage> messages)
        {
            foreach (var message in messages)
            {
                message.IsBodyHtml = true;
                _smtp.Send(message);
            }
        }

        private async Task SaveEmailSentInfo(int formInfoId
            , string body
            , string subject
            , string from
            , string to
            , string cc
            , string bcc)
        {
            var formInfo = await _formInfo.FirstOrDefaultAsync(x => x.FormInfoId == formInfoId);
            try
            {
                var emailSentInfo = new EmailSentInfo();
                emailSentInfo.AllFormsId = formInfo.AllFormsId;
                emailSentInfo.ActiveRecord = true;
                emailSentInfo.EmailBcc = bcc;
                emailSentInfo.EmailSubject = subject;
                emailSentInfo.EmailContent = body;
                emailSentInfo.EmailFrom = from;
                emailSentInfo.FormInfoId = formInfoId;
                emailSentInfo.EmailSentFlag = true;
                emailSentInfo.SentOn = DateTime.Now;
                emailSentInfo.EmailTo = to;
                emailSentInfo.EmailCc = cc;
                _emailSentInfo.Create(emailSentInfo);
            }
            catch (Exception e)
            {
                var result = new JsonResult(null);
                _log.LogError(e, e.Message);
                result.Value = new
                {
                    error = e.Message
                };
            }
        }
    }
}
