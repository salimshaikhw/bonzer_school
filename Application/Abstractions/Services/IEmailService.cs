using Application.ViewModels.EmailSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendEmailAsync(string to, string subject, string body, IEnumerable<EmailAttachment>? attachments = null);
    }
}
