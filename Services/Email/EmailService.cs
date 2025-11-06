using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace erp.Services.Email;

/// <summary>
/// Serviço de envio de emails via SMTP
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        return await SendEmailAsync(new[] { to }, subject, body, isHtml);
    }

    public async Task<bool> SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true)
    {
        try
        {
            if (!_settings.Enabled)
            {
                _logger.LogWarning("Email service is disabled. Email not sent to: {Recipients}", string.Join(", ", to));
                return false;
            }

            using var message = new MailMessage();
            message.From = new MailAddress(_settings.FromEmail, _settings.FromName);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = isHtml;

            foreach (var recipient in to)
            {
                message.To.Add(recipient);
            }

            using var smtpClient = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword),
                EnableSsl = _settings.EnableSsl
            };

            await smtpClient.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to: {Recipients}", string.Join(", ", to));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to: {Recipients}", string.Join(", ", to));
            return false;
        }
    }

    public async Task<bool> SendPasswordResetEmailAsync(string to, string userName, string resetToken, string resetUrl)
    {
        var subject = "Recuperação de Senha - Pillar ERP";
        var body = EmailTemplates.GetPasswordResetTemplate(userName, resetToken, resetUrl);
        return await SendEmailAsync(to, subject, body);
    }

    public async Task<bool> SendWelcomeEmailAsync(string to, string userName, string tempPassword)
    {
        var subject = "Bem-vindo ao Pillar ERP";
        var body = EmailTemplates.GetWelcomeTemplate(userName, tempPassword);
        return await SendEmailAsync(to, subject, body);
    }

    public async Task<bool> SendAccountConfirmationEmailAsync(string to, string userName, string confirmationUrl)
    {
        var subject = "Confirme sua Conta - Pillar ERP";
        var body = EmailTemplates.GetAccountConfirmationTemplate(userName, confirmationUrl);
        return await SendEmailAsync(to, subject, body);
    }

    public async Task<bool> SendNotificationEmailAsync(string to, string userName, string title, string message)
    {
        var subject = $"Notificação - {title}";
        var body = EmailTemplates.GetNotificationTemplate(userName, title, message);
        return await SendEmailAsync(to, subject, body);
    }
}

/// <summary>
/// Configurações de email do appsettings.json
/// </summary>
public class EmailSettings
{
    public bool Enabled { get; set; }
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool EnableSsl { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}
