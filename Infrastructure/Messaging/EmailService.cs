using Application.Abstractions.Messaging;
using Application.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Messaging
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string? _host;
        private readonly int _port;
        private readonly bool _enableSSL;
        private readonly string? _userName;
        private readonly string? _password;
        private readonly string? _from;

        public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            this._host = configuration["EmailSender:Host"];
            this._port = configuration.GetValue<int>("EmailSender:Port");
            this._enableSSL = configuration.GetValue<bool>("EmailSender:EnableSSL");
            this._userName = configuration["EmailSender:UserName"] ?? string.Empty;
            this._password = configuration["EmailSender:Password"] ?? string.Empty;
            this._from = configuration["EmailSender:From"] ?? string.Empty;
        }

        public Task<bool> SendEmailAsync(IEnumerable<string> to, string subject, string content, IEnumerable<string>? attachmentFiles = null, bool isHtml = true)
        {
            bool success;
            try
            {
                var isSendEmail = Convert.ToBoolean(_configuration.GetSection("appSettings")["SendEmail"]);
                if (isSendEmail)
                {
                    var client = new SmtpClient(_host, _port)
                    {
                        Credentials = new NetworkCredential(_userName, _password),
                        EnableSsl = _enableSSL,
                        Timeout = 120
                    };

                    _logger.LogInformation($"Sending email to: {string.Join(',', to)}, subject: {subject}");

                    var mailMessage = new MailMessage() { IsBodyHtml = isHtml };
                    mailMessage.From = new MailAddress(_from);
                    mailMessage.Subject = subject;
                    mailMessage.Body = content;

                    if (to.IsAny())
                    {
                        foreach (var address in to)
                            mailMessage.Bcc.Add(new MailAddress(address));

                        if (attachmentFiles.IsAny())
                        {
                            foreach (var filePath in attachmentFiles)
                            {
                                mailMessage.Attachments.Add(new Attachment(filePath));
                            }
                        }

                        client.SendMailAsync(mailMessage).Wait();
                        _logger.LogInformation($"Email Sent to: {string.Join(',', to)}");
                        success = true;
                    }
                    else
                    {
                        success = false;
                        _logger.LogInformation("No email recipient found.");
                    }
                }
                else
                {
                    success = false;
                    _logger.LogInformation("Send email is not active.");
                }
            }
            catch (Exception ex)
            {
                success = false;
                _logger.LogError(ex, "Error in sending email");
            }

            return Task.FromResult(success);
        }
    }
}
