namespace erp.Services.Email;

/// <summary>
/// Interface para serviço de envio de emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envia um email simples
    /// </summary>
    Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    
    /// <summary>
    /// Envia um email para múltiplos destinatários
    /// </summary>
    Task<bool> SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true);
    
    /// <summary>
    /// Envia email de recuperação de senha
    /// </summary>
    Task<bool> SendPasswordResetEmailAsync(string to, string userName, string resetToken, string resetUrl);
    
    /// <summary>
    /// Envia email de boas-vindas para novo usuário
    /// </summary>
    Task<bool> SendWelcomeEmailAsync(string to, string userName, string tempPassword);
    
    /// <summary>
    /// Envia email de confirmação de conta
    /// </summary>
    Task<bool> SendAccountConfirmationEmailAsync(string to, string userName, string confirmationUrl);
    
    /// <summary>
    /// Envia email de notificação genérica
    /// </summary>
    Task<bool> SendNotificationEmailAsync(string to, string userName, string title, string message);
}
