using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace DemoMVC.Services;

public sealed class EmailSender : IEmailSender
{
    private readonly string _smtpServer;
    private readonly int _smtpPort;
    private readonly string _senderName;
    private readonly string _senderEmail;
    private readonly string _password;

    public EmailSender(IConfiguration configuration)
    {
        var emailSettings = configuration.GetRequiredSection("EmailSettings");
        _smtpServer = GetRequiredSetting(emailSettings, "SmtpServer");
        _smtpPort = emailSettings.GetValue<int?>("SmtpPort")
            ?? throw new InvalidOperationException("EmailSettings:SmtpPort is required.");
        _senderName = GetRequiredSetting(emailSettings, "SenderName");
        _senderEmail = GetRequiredSetting(emailSettings, "SenderEmail");
        _password = GetRequiredSetting(emailSettings, "Password");

        if (_smtpPort is < 1 or > 65535)
        {
            throw new InvalidOperationException("EmailSettings:SmtpPort must be between 1 and 65535.");
        }
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_senderEmail, _senderName),
            Subject = subject,
            Body = htmlMessage,
            IsBodyHtml = true
        };
        message.To.Add(email);

        using var client = new SmtpClient(_smtpServer, _smtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_senderEmail, _password)
        };

        await client.SendMailAsync(message);
    }

    private static string GetRequiredSetting(IConfigurationSection settings, string key)
    {
        var value = settings[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"EmailSettings:{key} is required.");
        }

        return value;
    }
}
