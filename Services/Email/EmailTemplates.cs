namespace erp.Services.Email;

/// <summary>
/// Templates HTML para emails do sistema
/// </summary>
public static class EmailTemplates
{
    private static string GetBaseTemplate(string title, string content)
    {
        return $@"
<!DOCTYPE html>
<html lang='pt-BR'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{title}</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background-color: #f4f4f4;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background-color: #ffffff;
            border-radius: 8px;
            box-shadow: 0 2px 8px rgba(0,0,0,0.1);
            overflow: hidden;
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: #ffffff;
            padding: 30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
            font-weight: 600;
        }}
        .content {{
            padding: 40px 30px;
            color: #333333;
            line-height: 1.6;
        }}
        .button {{
            display: inline-block;
            padding: 12px 30px;
            margin: 20px 0;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: #ffffff !important;
            text-decoration: none;
            border-radius: 5px;
            font-weight: 600;
            text-align: center;
        }}
        .button:hover {{
            opacity: 0.9;
        }}
        .code-box {{
            background-color: #f8f9fa;
            border: 2px dashed #667eea;
            border-radius: 5px;
            padding: 15px;
            margin: 20px 0;
            text-align: center;
            font-size: 24px;
            font-weight: bold;
            color: #667eea;
            letter-spacing: 3px;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 20px 30px;
            text-align: center;
            color: #666666;
            font-size: 12px;
        }}
        .divider {{
            height: 1px;
            background-color: #e0e0e0;
            margin: 20px 0;
        }}
        .warning {{
            background-color: #fff3cd;
            border-left: 4px solid #ffc107;
            padding: 15px;
            margin: 20px 0;
            border-radius: 4px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🏛️ Pillar ERP</h1>
        </div>
        <div class='content'>
            {content}
        </div>
        <div class='footer'>
            <p>Este é um email automático, por favor não responda.</p>
            <p>&copy; 2025 Pillar ERP. Todos os direitos reservados.</p>
        </div>
    </div>
</body>
</html>";
    }

    public static string GetPasswordResetTemplate(string userName, string resetToken, string resetUrl)
    {
        var content = $@"
            <h2>Olá, {userName}!</h2>
            <p>Recebemos uma solicitação para redefinir a senha da sua conta.</p>
            <p>Use o código abaixo para redefinir sua senha:</p>
            <div class='code-box'>{resetToken}</div>
            <p>Ou clique no botão abaixo para redefinir diretamente:</p>
            <div style='text-align: center;'>
                <a href='{resetUrl}' class='button'>Redefinir Senha</a>
            </div>
            <div class='warning'>
                <strong>⚠️ Atenção:</strong> Este link expira em 24 horas por questões de segurança.
            </div>
            <div class='divider'></div>
            <p style='font-size: 14px; color: #666;'>
                Se você não solicitou a redefinição de senha, ignore este email. 
                Sua senha permanecerá inalterada.
            </p>";

        return GetBaseTemplate("Recuperação de Senha", content);
    }

    public static string GetWelcomeTemplate(string userName, string tempPassword)
    {
        var content = $@"
            <h2>Bem-vindo ao Pillar ERP, {userName}!</h2>
            <p>Sua conta foi criada com sucesso! Estamos felizes em tê-lo(a) conosco.</p>
            <p>Sua senha temporária é:</p>
            <div class='code-box'>{tempPassword}</div>
            <div class='warning'>
                <strong>🔐 Importante:</strong> Por questões de segurança, recomendamos que você altere 
                sua senha temporária no primeiro acesso ao sistema.
            </div>
            <div style='text-align: center; margin: 30px 0;'>
                <a href='https://localhost:7051' class='button'>Acessar Sistema</a>
            </div>
            <div class='divider'></div>
            <h3>Próximos Passos:</h3>
            <ol>
                <li>Faça login com seu email e senha temporária</li>
                <li>Complete seu perfil nas configurações</li>
                <li>Explore as funcionalidades do sistema</li>
            </ol>
            <p>Se precisar de ajuda, nossa equipe está à disposição!</p>";

        return GetBaseTemplate("Bem-vindo", content);
    }

    public static string GetAccountConfirmationTemplate(string userName, string confirmationUrl)
    {
        var content = $@"
            <h2>Olá, {userName}!</h2>
            <p>Obrigado por se registrar no Pillar ERP!</p>
            <p>Para ativar sua conta, por favor confirme seu endereço de email clicando no botão abaixo:</p>
            <div style='text-align: center; margin: 30px 0;'>
                <a href='{confirmationUrl}' class='button'>Confirmar Email</a>
            </div>
            <div class='warning'>
                <strong>⏰ Atenção:</strong> Este link de confirmação expira em 48 horas.
            </div>
            <div class='divider'></div>
            <p style='font-size: 14px; color: #666;'>
                Se você não criou uma conta no Pillar ERP, ignore este email.
            </p>";

        return GetBaseTemplate("Confirmação de Conta", content);
    }

    public static string GetNotificationTemplate(string userName, string title, string message)
    {
        var content = $@"
            <h2>Olá, {userName}!</h2>
            <h3>{title}</h3>
            <div style='background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin: 20px 0;'>
                {message}
            </div>
            <div style='text-align: center; margin: 30px 0;'>
                <a href='https://localhost:7051' class='button'>Acessar Sistema</a>
            </div>";

        return GetBaseTemplate(title, content);
    }
}
