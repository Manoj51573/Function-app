using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;

namespace eforms_middleware.Interfaces
{
    public interface IFormEmailService
    {
        Task SendEmail(int formInfoId, string toEmail, string subject, string body, string ccEmail = "");
        void SendEmail(IList<MailMessage> messages);
    }
}