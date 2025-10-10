using System.Net;

namespace erp.Services;

public static class ErrorMessageHelper
{
    public static string GetFriendlyErrorMessage(HttpStatusCode statusCode, string context = "operação")
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => $"Os dados fornecidos são inválidos. Por favor, verifique e tente novamente.",
            HttpStatusCode.Unauthorized => $"Você precisa estar autenticado para realizar esta {context}.",
            HttpStatusCode.Forbidden => $"Você não tem permissão para realizar esta {context}.",
            HttpStatusCode.NotFound => $"O recurso solicitado não foi encontrado.",
            HttpStatusCode.Conflict => $"Já existe um registro com estes dados.",
            HttpStatusCode.UnprocessableEntity => $"Não foi possível processar os dados fornecidos.",
            HttpStatusCode.InternalServerError => $"Ocorreu um erro no servidor. Tente novamente mais tarde.",
            HttpStatusCode.ServiceUnavailable => $"O serviço está temporariamente indisponível. Tente novamente em alguns instantes.",
            HttpStatusCode.RequestTimeout => $"A requisição demorou muito para ser processada. Verifique sua conexão e tente novamente.",
            _ => $"Ocorreu um erro inesperado ({(int)statusCode}). Tente novamente."
        };
    }

    public static string GetLoginErrorMessage(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => "E-mail ou senha inválidos. Verifique seus dados e tente novamente.",
            HttpStatusCode.Unauthorized => "E-mail ou senha incorretos. Tente novamente.",
            HttpStatusCode.Forbidden => "Sua conta está desativada. Entre em contato com o administrador.",
            HttpStatusCode.TooManyRequests => "Muitas tentativas de login. Aguarde alguns minutos e tente novamente.",
            HttpStatusCode.InternalServerError => "Erro ao processar o login. Tente novamente mais tarde.",
            _ => $"Não foi possível realizar o login. {GetFriendlyErrorMessage(statusCode, "login")}"
        };
    }

    public static string GetUserOperationErrorMessage(HttpStatusCode statusCode, string operation)
    {
        var baseMessage = operation switch
        {
            "create" => "criar o usuário",
            "update" => "atualizar o usuário",
            "delete" => "excluir o usuário",
            "load" => "carregar os dados do usuário",
            _ => "realizar a operação"
        };

        return statusCode switch
        {
            HttpStatusCode.BadRequest => $"Não foi possível {baseMessage}. Verifique se todos os campos estão preenchidos corretamente.",
            HttpStatusCode.Conflict => operation == "create" 
                ? "Este e-mail já está cadastrado no sistema." 
                : "Conflito ao atualizar. O registro pode ter sido modificado por outro usuário.",
            HttpStatusCode.NotFound => "Usuário não encontrado. Ele pode ter sido removido.",
            HttpStatusCode.UnprocessableEntity => $"Dados inválidos. Verifique:\n• E-mail deve ser válido\n• Telefone deve ter 10 ou 11 dígitos\n• Senha deve ter no mínimo 6 caracteres",
            _ => $"Não foi possível {baseMessage}. {GetFriendlyErrorMessage(statusCode)}"
        };
    }

    public static string GetValidationErrorMessage(string field, string? errorType = null)
    {
        return (field.ToLower(), errorType?.ToLower()) switch
        {
            ("email", "required") => "O e-mail é obrigatório.",
            ("email", "invalid") => "Informe um e-mail válido (exemplo: usuario@empresa.com).",
            ("password", "required") => "A senha é obrigatória.",
            ("password", "minlength") => "A senha deve ter no mínimo 6 caracteres.",
            ("username", "required") => "O nome é obrigatório.",
            ("username", "minlength") => "O nome deve ter pelo menos 3 caracteres.",
            ("username", "maxlength") => "O nome não pode ter mais de 50 caracteres.",
            ("phone", "required") => "O telefone é obrigatório.",
            ("phone", "invalid") => "Informe um telefone válido com DDD (10 ou 11 dígitos).",
            ("roles", "required") => "Selecione pelo menos uma função/permissão.",
            _ => $"Campo inválido: {field}"
        };
    }
}
