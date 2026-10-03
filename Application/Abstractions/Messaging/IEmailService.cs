using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Abstractions.Messaging
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(IEnumerable<string> to, string subject, string content,
            IEnumerable<string> attachmentFiles = null, bool isHtml = true);
    }
}
