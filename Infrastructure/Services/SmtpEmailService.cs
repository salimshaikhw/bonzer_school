using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Options;
using Application.Abstractions.Services;
using Application.ViewModels.EmailSettings;

public class SmtpEmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public SmtpEmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    // Keep compatibility: simple send without attachments
    public Task SendEmailAsync(string to, string subject, string body)
        => SendEmailAsync(to, subject, body, null);

    // New method with attachments
    public async Task SendEmailAsync(string to, string subject, string body, IEnumerable<EmailAttachment>? attachments = null)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = body };

        if (attachments != null)
        {
            foreach (var att in attachments)
            {
                if (att.Content.CanSeek) att.Content.Position = 0;
                // Use MimeKit's ContentType parser
                var contentType = MimeKit.ContentType.Parse(att.ContentType ?? "application/octet-stream");
                builder.Attachments.Add(att.FileName, att.Content, contentType);
            }
        }

        email.Body = builder.ToMessageBody();

        using var smtp = new SmtpClient();
        try
        {
            // Choose appropriate SecureSocketOptions for your SMTP port
            await smtp.ConnectAsync(_settings.SmtpServer, _settings.Port, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtp.SendAsync(email);
        }
        finally
        {
            await smtp.DisconnectAsync(true);
        }
    }
}

