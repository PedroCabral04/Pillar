using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Google;
using erp.DTOs.Chatbot;
using erp.Services.Chatbot.ChatbotPlugins;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace erp.Services.Chatbot;

public class ChatbotService : IChatbotService
{
    private readonly Kernel _kernel;
    private readonly ILogger<ChatbotService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IChatbotCacheService _cacheService;
    private readonly IChatbotConfirmationService _confirmationService;
    private readonly IChatbotUserContext _userContext;
    private readonly IChatbotAuditService _auditService;
    private readonly string _aiProvider;
    private readonly bool _aiConfigured;

    private static readonly string[] MutatingActionKeywords =
    {
        "criar", "cadastrar", "adicionar", "inserir", "gerar", "emitir",
        "editar", "atualizar", "alterar", "ajustar", "mudar",
        "excluir", "deletar", "remover", "cancelar", "finalizar", "aprovar",
        "reabrir", "transferir", "baixar", "liquidar", "pagar"
    };

    private static readonly HashSet<string> MutatingActionKeywordSet =
        new(MutatingActionKeywords, StringComparer.Ordinal);

    private static readonly HashSet<string> IntentPrefixKeywords =
        new(StringComparer.Ordinal)
        {
            "quero", "preciso", "pode", "poderia", "vamos", "favor", "faca", "faça", "execute", "executar"
        };

    private static readonly HashSet<string> BridgeTokens =
        new(StringComparer.Ordinal)
        {
            "me", "nos", "isso", "isto", "o", "a"
        };

    private static readonly Regex WordTokenRegex =
        new(@"\p{L}+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly string[] ConfirmationPrefixes =
    {
        "confirmar:", "confirmo:", "confirmar ", "confirmo ",
        "sim, confirmar", "sim confirmar"
    };

    public ChatbotService(
        ILogger<ChatbotService> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider,
        IChatbotCacheService cacheService,
        IChatbotConfirmationService confirmationService,
        IChatbotUserContext userContext,
        IChatbotAuditService auditService)
    {
        _logger = logger;
        _configuration = configuration;
        _cacheService = cacheService;
        _confirmationService = confirmationService;
        _userContext = userContext;
        _auditService = auditService;

        // Criar o Kernel do Semantic Kernel
        var builder = Kernel.CreateBuilder();

        // Determinar qual provedor de IA usar
        var aiProvider = configuration["AI:Provider"]?.ToLower() ?? "openai";
        _aiProvider = aiProvider;

        bool aiConfigured = false;

        // Configurar OpenAI
        if (aiProvider == "openai")
        {
            var openAiKey = configuration["OpenAI:ApiKey"];
            var openAiModel = configuration["OpenAI:Model"] ?? "gpt-4o-mini";

            if (!string.IsNullOrEmpty(openAiKey))
            {
                builder.AddOpenAIChatCompletion(
                    modelId: openAiModel,
                    apiKey: openAiKey);
                
                aiConfigured = true;
                _logger.LogInformation("Chatbot configurado com OpenAI (modelo: {Model})", openAiModel);
            }
        }
        // Configurar Google AI (Gemini)
        else if (aiProvider == "google" || aiProvider == "gemini")
        {
            var googleApiKey = configuration["GoogleAI:ApiKey"];
            var googleModel = configuration["GoogleAI:Model"] ?? "gemini-1.5-flash";

            if (!string.IsNullOrEmpty(googleApiKey))
            {
                builder.AddGoogleAIGeminiChatCompletion(
                    modelId: googleModel,
                    apiKey: googleApiKey);
                
                aiConfigured = true;
                _aiProvider = "google";
                _logger.LogInformation("Chatbot configurado com Google AI (modelo: {Model})", googleModel);
            }
        }
        // Configurar LM Studio
        else if (aiProvider == "lmstudio")
        {
            var lmStudioEndpoint = configuration["LMStudio:Endpoint"];
            var lmStudioModel = configuration["LMStudio:Model"] ?? "local-model";

            if (!string.IsNullOrEmpty(lmStudioEndpoint))
            {
                if (Uri.TryCreate(lmStudioEndpoint, UriKind.Absolute, out var endpointUri))
                {
                    builder.AddOpenAIChatCompletion(
                        modelId: lmStudioModel,
                        apiKey: "not-needed",
                        endpoint: endpointUri);

                    aiConfigured = true;
                    _aiProvider = "lmstudio";
                    _logger.LogInformation("Chatbot configurado com LM Studio: {Endpoint} (modelo: {Model})", lmStudioEndpoint, lmStudioModel);
                }
                else
                {
                    _logger.LogError("LM Studio Endpoint inválido: {Endpoint}", lmStudioEndpoint);
                }
            }
        }
        // Configurar Custom OpenAI-compatible endpoint (LMStudio, Ollama, LocalAI, etc.)
        else if (aiProvider == "custom" || aiProvider == "openai-compatible")
        {
            var customEndpoint = configuration["CustomAI:Endpoint"];
            var customApiKey = configuration["CustomAI:ApiKey"];
            var customModel = configuration["CustomAI:Model"] ?? "local-model";

            if (!string.IsNullOrEmpty(customEndpoint))
            {
                // Validar URL do endpoint
                if (!Uri.TryCreate(customEndpoint, UriKind.Absolute, out var endpointUri) ||
                    (endpointUri.Scheme != "http" && endpointUri.Scheme != "https"))
                {
                    _logger.LogError("Custom AI Endpoint inválido: {Endpoint}. Deve ser uma URL HTTP/HTTPS válida.", customEndpoint);
                }
                else
                {
                    // Usar chave de API se fornecida, caso contrário usar placeholder (para endpoints locais)
                    var apiKey = string.IsNullOrEmpty(customApiKey) ? "not-needed" : customApiKey;
                    
                    builder.AddOpenAIChatCompletion(
                        modelId: customModel,
                        apiKey: apiKey,
                        endpoint: endpointUri);
                    
                    aiConfigured = true;
                    _aiProvider = "custom";
                    _logger.LogInformation("Chatbot configurado com Custom OpenAI-compatible endpoint: {Endpoint} (modelo: {Model})", 
                        customEndpoint, customModel);
                }
            }
            else
            {
                _logger.LogWarning("Custom AI Provider selecionado mas endpoint não configurado em CustomAI:Endpoint");
            }
        }

        if (!aiConfigured)
        {
            // Modo fallback sem IA - apenas responde com templates
            _logger.LogWarning("Nenhuma API Key ou endpoint configurado (OpenAI, Google AI ou Custom). Chatbot funcionará em modo limitado.");
        }

        _aiConfigured = aiConfigured;

        // Registrar plugins com DI
        builder.Services.AddSingleton(serviceProvider);
        
        builder.Plugins.AddFromObject(
            ActivatorUtilities.CreateInstance<ProductsPlugin>(serviceProvider), 
            "ProductsPlugin");
        
        builder.Plugins.AddFromObject(
            ActivatorUtilities.CreateInstance<SalesPlugin>(serviceProvider), 
            "SalesPlugin");
        
        builder.Plugins.AddFromObject(
            ActivatorUtilities.CreateInstance<FinancialPlugin>(serviceProvider), 
            "FinancialPlugin");
        
        builder.Plugins.AddFromObject(
            ActivatorUtilities.CreateInstance<HRPlugin>(serviceProvider), 
            "HRPlugin");
        
        builder.Plugins.AddFromObject(
            new SystemPlugin(), 
            "SystemPlugin");
        
        // builder.Plugins.AddFromObject(
        //     ActivatorUtilities.CreateInstance<AssetsPlugin>(serviceProvider), 
        //     "AssetsPlugin");
        
        builder.Plugins.AddFromObject(
            ActivatorUtilities.CreateInstance<CustomersPlugin>(serviceProvider), 
            "CustomersPlugin");
        
        builder.Plugins.AddFromObject(
            ActivatorUtilities.CreateInstance<SuppliersPlugin>(serviceProvider), 
            "SuppliersPlugin");
        
        builder.Plugins.AddFromObject(
            ActivatorUtilities.CreateInstance<PayrollPlugin>(serviceProvider), 
            "PayrollPlugin");

        _kernel = builder.Build();
    }

    public async Task<ChatResponseDto> ProcessMessageAsync(
        string message,
        List<ChatMessageDto>? conversationHistory = null,
        int? userId = null,
        ChatOperationMode operationMode = ChatOperationMode.ProposeAction,
        ChatResponseStyle responseStyle = ChatResponseStyle.Executive,
        bool isConfirmedAction = false,
        int? conversationId = null,
        string source = "quick")
    {
        var stopwatch = Stopwatch.StartNew();
        var normalizedSource = NormalizeSource(source);
        string effectiveMessage = message;
        bool confirmationGranted = isConfirmedAction;

        try
        {
            // Set user context for this request if userId is provided
            if (userId.HasValue)
            {
                _userContext.SetCurrentUser(userId.Value);
            }

            var parsedMessage = ExtractEffectiveMessage(message);
            var isConfirmationMessage = parsedMessage.IsConfirmation;
            effectiveMessage = parsedMessage.EffectiveMessage;
            confirmationGranted = isConfirmedAction;

            string? pendingConfirmation = null;
            if (userId.HasValue)
            {
                pendingConfirmation = _confirmationService.GetPendingAction(userId.Value, conversationId, normalizedSource);
            }

            if (!confirmationGranted && isConfirmationMessage)
            {
                if (!string.IsNullOrWhiteSpace(pendingConfirmation)
                    && IsSameConfirmationTarget(pendingConfirmation, effectiveMessage))
                {
                    confirmationGranted = true;
                    if (userId.HasValue)
                    {
                        _confirmationService.ClearPendingAction(userId.Value, conversationId, normalizedSource);
                    }
                }
                else
                {
                    var invalidConfirmationResponse = BuildInvalidConfirmationResponse(operationMode, pendingConfirmation);
                    return await AuditAndReturnAsync(
                        invalidConfirmationResponse,
                        "confirmation_invalid",
                        userId,
                        conversationId,
                        normalizedSource,
                        message,
                        effectiveMessage,
                        operationMode,
                        responseStyle,
                        false,
                        stopwatch);
                }
            }

            string outcome;

            var guardrailResponse = EvaluateSafetyResponse(effectiveMessage, operationMode, confirmationGranted);
            if (guardrailResponse != null)
            {
                if (guardrailResponse.RequiresConfirmation && userId.HasValue)
                {
                    _confirmationService.SetPendingAction(userId.Value, conversationId, normalizedSource, effectiveMessage);
                }

                outcome = guardrailResponse.RequiresConfirmation
                    ? "confirmation_requested"
                    : "guardrail_blocked";

                return await AuditAndReturnAsync(
                    guardrailResponse,
                    outcome,
                    userId,
                    conversationId,
                    normalizedSource,
                    message,
                    effectiveMessage,
                    operationMode,
                    responseStyle,
                    confirmationGranted,
                    stopwatch);
            }

            var cacheMessageKey = $"{operationMode}:{responseStyle}:{effectiveMessage}";

            // Verificar cache primeiro
            if (_cacheService.IsEnabled)
            {
                var contextHash = _cacheService.GenerateContextHash(conversationHistory);
                var cachedResponse = _cacheService.GetCachedResponse(cacheMessageKey, contextHash);
                
                if (cachedResponse != null)
                {
                    _logger.LogDebug("Resposta obtida do cache para: {MessagePreview}...", 
                        effectiveMessage.Length > 30 ? effectiveMessage[..30] : effectiveMessage);
                    cachedResponse.OperationMode = operationMode;
                    return await AuditAndReturnAsync(
                        cachedResponse,
                        "cache_hit",
                        userId,
                        conversationId,
                        normalizedSource,
                        message,
                        effectiveMessage,
                        operationMode,
                        responseStyle,
                        confirmationGranted,
                        stopwatch);
                }
            }

            // Se não tem IA configurada, usar modo fallback
            if (!_aiConfigured)
            {
                var fallbackResponse = ProcessFallbackMode(effectiveMessage, operationMode);
                return await AuditAndReturnAsync(
                    fallbackResponse,
                    "fallback_mode",
                    userId,
                    conversationId,
                    normalizedSource,
                    message,
                    effectiveMessage,
                    operationMode,
                    responseStyle,
                    confirmationGranted,
                    stopwatch);
            }

            // Criar histórico de conversa
            var chatHistory = new ChatHistory();
            
            // System prompt
            chatHistory.AddSystemMessage(BuildSystemPrompt(operationMode, responseStyle));

            // Adicionar histórico anterior se existir
            if (conversationHistory != null)
            {
                foreach (var msg in conversationHistory.TakeLast(10)) // Limitar para evitar contexto muito grande
                {
                    if (msg.Role == "user")
                        chatHistory.AddUserMessage(msg.Content);
                    else if (msg.Role == "assistant")
                        chatHistory.AddAssistantMessage(msg.Content);
                }
            }

            // Adicionar mensagem atual
            chatHistory.AddUserMessage(effectiveMessage);

            // Configurar execução com auto function calling
            PromptExecutionSettings executionSettings;
            
            if (_aiProvider == "google")
            {
                executionSettings = new GeminiPromptExecutionSettings
                {
                    ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions,
                    Temperature = 0.7,
                    MaxTokens = 1000
                };
            }
            else // openai or custom (both use OpenAI-compatible format)
            {
                executionSettings = new OpenAIPromptExecutionSettings
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                    Temperature = 0.7,
                    MaxTokens = 1000
                };
            }

            // Obter resposta
            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
            var result = await chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                _kernel);

            var responseContent = result.Content ?? "Desculpe, não consegui processar sua mensagem.";
            var suggestions = GenerateSuggestions(effectiveMessage);

            var response = new ChatResponseDto
            {
                Response = responseContent,
                Success = true,
                SuggestedActions = suggestions,
                OperationMode = operationMode,
                EvidenceSources = ExtractEvidenceSources(responseContent)
            };

            // Armazenar no cache para futuras requisições
            if (_cacheService.IsEnabled)
            {
                var contextHash = _cacheService.GenerateContextHash(conversationHistory);
                _cacheService.SetCachedResponse(cacheMessageKey, response, contextHash);
            }

            return await AuditAndReturnAsync(
                response,
                "processed",
                userId,
                conversationId,
                normalizedSource,
                message,
                effectiveMessage,
                operationMode,
                responseStyle,
                confirmationGranted,
                stopwatch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem do chatbot");
            var errorResponse = new ChatResponseDto
            {
                Response = "Desculpe, ocorreu um erro ao processar sua mensagem. Por favor, tente novamente.",
                Success = false,
                Error = ex.Message,
                OperationMode = operationMode
            };

            return await AuditAndReturnAsync(
                errorResponse,
                "error",
                userId,
                conversationId,
                normalizedSource,
                message,
                effectiveMessage,
                operationMode,
                responseStyle,
                confirmationGranted,
                stopwatch);
        }
        finally
        {
            // Always clear user context after processing
            _userContext.Clear();
        }
    }

    private async Task<ChatResponseDto> AuditAndReturnAsync(
        ChatResponseDto response,
        string outcome,
        int? userId,
        int? conversationId,
        string source,
        string requestMessage,
        string effectiveMessage,
        ChatOperationMode operationMode,
        ChatResponseStyle responseStyle,
        bool isConfirmedAction,
        Stopwatch stopwatch)
    {
        stopwatch.Stop();

        await _auditService.LogAsync(new ChatbotAuditRequest
        {
            UserId = userId,
            ConversationId = conversationId,
            Source = source,
            Outcome = outcome,
            RequestMessage = requestMessage,
            EffectiveMessage = effectiveMessage,
            Response = response,
            OperationMode = operationMode,
            ResponseStyle = responseStyle,
            IsConfirmedAction = isConfirmedAction,
            AiProvider = _aiProvider,
            AiConfigured = _aiConfigured,
            DurationMs = (int)stopwatch.ElapsedMilliseconds
        });

        return response;
    }

    private ChatResponseDto ProcessFallbackMode(string message, ChatOperationMode operationMode)
    {
        // Modo básico sem IA - detecta palavras-chave
        var lowerMessage = message.ToLower();

        if (lowerMessage.Contains("ajuda") || lowerMessage.Contains("help") || lowerMessage.Contains("o que você faz"))
        {
            return new ChatResponseDto
            {
                Response = new SystemPlugin().GetHelp(),
                Success = true,
                OperationMode = operationMode
            };
        }

        if (lowerMessage.Contains("sistema") || lowerMessage.Contains("sobre"))
        {
            return new ChatResponseDto
            {
                Response = new SystemPlugin().GetSystemInfo(),
                Success = true,
                OperationMode = operationMode
            };
        }

        return new ChatResponseDto
        {
            Response = @"⚠️ **Modo Limitado**

Para habilitar todas as funcionalidades do chatbot, configure uma chave de API:

**Opção 1 - OpenAI (GPT):**
```json
{
  ""AI"": { ""Provider"": ""openai"" },
  ""OpenAI"": {
    ""ApiKey"": ""sua-chave-aqui"",
    ""Model"": ""gpt-4o-mini""
  }
}
```

**Opção 2 - Google AI (Gemini - GRÁTIS):**
```json
{
  ""AI"": { ""Provider"": ""google"" },
  ""GoogleAI"": {
    ""ApiKey"": ""sua-chave-aqui"",
    ""Model"": ""gemini-1.5-flash""
  }
}
```

**Opção 3 - LM Studio (Local):**
```json
{
  ""AI"": { ""Provider"": ""lmstudio"" },
  ""LMStudio"": {
    ""Endpoint"": ""http://localhost:1234/v1"",
    ""Model"": ""gemma-2b-it""
  }
}
```

**Opção 4 - Custom OpenAI-Compatible (Ollama, LocalAI):**
```json
{
  ""AI"": { ""Provider"": ""custom"" },
  ""CustomAI"": {
    ""Endpoint"": ""http://localhost:11434/v1"",
    ""ApiKey"": """",
    ""Model"": ""llama3""
  }
}
```

🎁 **Recomendado:** Google AI oferece tier gratuito generoso!
Obtenha sua chave em: https://ai.google.dev/

💻 **Local:** Use LMStudio para rodar modelos localmente sem custo!
Download: https://lmstudio.ai/

            No momento, posso apenas fornecer informações básicas.
Digite 'ajuda' para ver o que posso fazer quando configurado corretamente.",
            Success = true,
            SuggestedActions = new List<string> { "Ajuda", "Sobre o sistema" },
            OperationMode = operationMode
        };
    }

    private static (bool IsConfirmation, string EffectiveMessage) ExtractEffectiveMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return (false, message);
        }

        var normalized = message.Trim();
        var normalizedLower = normalized.ToLowerInvariant();

        foreach (var prefix in ConfirmationPrefixes)
        {
            if (!normalizedLower.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var effective = normalized[prefix.Length..].Trim();
            if (string.IsNullOrWhiteSpace(effective))
            {
                effective = normalized;
            }

            return (true, effective);
        }

        return (false, normalized);
    }

    private static string NormalizeSource(string source)
    {
        return string.IsNullOrWhiteSpace(source)
            ? "quick"
            : source.Trim().ToLowerInvariant();
    }

    private static bool IsSameConfirmationTarget(string expectedMessage, string providedMessage)
    {
        return string.Equals(
            NormalizeForComparison(expectedMessage),
            NormalizeForComparison(providedMessage),
            StringComparison.Ordinal);
    }

    private static string NormalizeForComparison(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return string.Empty;
        }

        return Regex.Replace(message.Trim().ToLowerInvariant(), @"\s+", " ");
    }

    private static string BuildConfirmationCommand(string message)
    {
        return $"Confirmar: {message}";
    }

    private static ChatResponseDto BuildInvalidConfirmationResponse(
        ChatOperationMode operationMode,
        string? pendingConfirmation)
    {
        if (string.IsNullOrWhiteSpace(pendingConfirmation))
        {
            return new ChatResponseDto
            {
                Success = true,
                OperationMode = operationMode,
                Response = "⚠️ Não há confirmação pendente. Envie primeiro a ação que deseja executar.",
                SuggestedActions = new List<string> { "Descrever ação a executar" }
            };
        }

        var confirmationCommand = BuildConfirmationCommand(pendingConfirmation);
        return new ChatResponseDto
        {
            Success = true,
            OperationMode = operationMode,
            RequiresConfirmation = true,
            ConfirmationPrompt = confirmationCommand,
            Response = $"⚠️ A confirmação não corresponde à ação pendente. Para continuar, envie exatamente: `{confirmationCommand}`",
            SuggestedActions = new List<string> { confirmationCommand }
        };
    }

    private static ChatResponseDto? EvaluateSafetyResponse(
        string message,
        ChatOperationMode operationMode,
        bool confirmationGranted)
    {
        if (!IsMutatingRequest(message))
        {
            return null;
        }

        if (operationMode == ChatOperationMode.ReadOnly)
        {
            return new ChatResponseDto
            {
                Success = true,
                OperationMode = operationMode,
                Response = """
                    🔒 **Modo Somente Leitura Ativo**

                    Não posso executar ações que alteram dados neste modo.

                    **Próximo passo:** mude para **Propor ação** ou **Executar com confirmação** para continuar.
                    """,
                SuggestedActions = new List<string>
                {
                    "Mudar para Propor ação",
                    "Mudar para Executar com confirmação"
                }
            };
        }

        if (operationMode == ChatOperationMode.ProposeAction)
        {
            return new ChatResponseDto
            {
                Success = true,
                OperationMode = operationMode,
                Response = """
                    📝 **Modo Propor Ação Ativo**

                    Posso montar o plano e validar os dados, mas não executo alterações neste modo.

                    **Próximo passo:** altere para **Executar com confirmação** e envie novamente.
                    """,
                SuggestedActions = new List<string>
                {
                    "Mudar para Executar com confirmação"
                }
            };
        }

        if (operationMode == ChatOperationMode.ExecuteWithConfirmation && !confirmationGranted)
        {
            return new ChatResponseDto
            {
                Success = true,
                OperationMode = operationMode,
                RequiresConfirmation = true,
                ConfirmationPrompt = BuildConfirmationCommand(message),
                Response = $"""
                    ⚠️ **Confirmação necessária**

                    Identifiquei uma ação que pode alterar dados.

                    Se quiser continuar, envie exatamente:
                    `{BuildConfirmationCommand(message)}`
                    """,
                SuggestedActions = new List<string>
                {
                    BuildConfirmationCommand(message)
                }
            };
        }

        return null;
    }

    private static bool IsMutatingRequest(string message)
    {
        var normalized = NormalizeForComparison(message);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        if (Regex.IsMatch(normalized, @"\bcontas?\s+a\s+(pagar|receber)\b", RegexOptions.CultureInvariant)
            && (normalized.EndsWith("?", StringComparison.Ordinal)
                || normalized.StartsWith("quais ", StringComparison.Ordinal)
                || normalized.StartsWith("qual ", StringComparison.Ordinal)
                || normalized.StartsWith("listar ", StringComparison.Ordinal)
                || normalized.StartsWith("mostr", StringComparison.Ordinal)))
        {
            return false;
        }

        var tokens = WordTokenRegex.Matches(normalized)
            .Select(match => match.Value)
            .ToList();

        if (tokens.Count == 0)
        {
            return false;
        }

        if (MutatingActionKeywordSet.Contains(tokens[0]))
        {
            return true;
        }

        for (var i = 1; i < tokens.Count; i++)
        {
            if (!MutatingActionKeywordSet.Contains(tokens[i]))
            {
                continue;
            }

            if (IntentPrefixKeywords.Contains(tokens[i - 1]))
            {
                return true;
            }

            if (i > 1
                && BridgeTokens.Contains(tokens[i - 1])
                && IntentPrefixKeywords.Contains(tokens[i - 2]))
            {
                return true;
            }
        }

        return false;
    }

    private static string BuildSystemPrompt(ChatOperationMode operationMode, ChatResponseStyle responseStyle)
    {
        var modeInstructions = operationMode switch
        {
            ChatOperationMode.ReadOnly => """
                - Você está em MODO SOMENTE LEITURA: nunca execute ações que criem, alterem ou excluam dados.
                - Se o usuário pedir ação transacional, explique que o modo atual bloqueia execução.
                """,
            ChatOperationMode.ProposeAction => """
                - Você está em MODO PROPOR AÇÃO: não execute ações transacionais.
                - Você deve responder com o plano de ação, impactos e validações necessárias.
                """,
            _ => """
                - Você está em MODO EXECUTAR COM CONFIRMAÇÃO.
                - Só execute ações transacionais quando a mensagem do usuário vier confirmada.
                """
        };

        var styleInstructions = responseStyle switch
        {
            ChatResponseStyle.Specialist => "Forneça detalhes técnicos, critérios e riscos quando relevante.",
            _ => "Prefira respostas curtas e executivas, sem perder precisão."
        };

        return $"""
            Você é o assistente virtual do Pillar ERP, um sistema de gestão empresarial brasileiro.

            **Suas capacidades:**
            - Gerenciar produtos, vendas, clientes e fornecedores
            - Consultar informações financeiras (contas a pagar/receber)
            - Gerenciar ativos da empresa e manutenções
            - Consultar folha de pagamento e recursos humanos

            **Regras de formatação e resposta:**
            - Responda sempre em português brasileiro.
            - Use Markdown.
            - Estruture em três blocos quando possível: **Resumo**, **Evidências**, **Próximo passo**.
            - No bloco **Evidências**, inclua ao menos uma linha com `Fonte: <origem> | Período: <intervalo>` quando houver dados.
            - Seja objetivo e profissional.
            - {styleInstructions}

            **Guardrails de operação:**
            {modeInstructions}
            """;
    }

    private static List<ChatEvidenceSourceDto> ExtractEvidenceSources(string responseContent)
    {
        var evidence = new List<ChatEvidenceSourceDto>();
        if (string.IsNullOrWhiteSpace(responseContent))
        {
            return evidence;
        }

        var matches = Regex.Matches(
            responseContent,
            @"(?im)^\s*(?:-\s*)?(?:\*\*)?(?:fonte|origem)(?:\*\*)?\s*:\s*(.+)$");

        foreach (Match match in matches)
        {
            var rawValue = match.Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                continue;
            }

            var source = rawValue;
            string? period = null;

            var parts = rawValue.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                source = parts[0].Replace("Período:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            }

            if (parts.Length > 1)
            {
                period = parts
                    .FirstOrDefault(p => p.Contains("período", StringComparison.OrdinalIgnoreCase))
                    ?.Replace("Período:", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Trim();
            }

            evidence.Add(new ChatEvidenceSourceDto
            {
                Source = source,
                Period = string.IsNullOrWhiteSpace(period) ? null : period
            });
        }

        if (evidence.Count > 0)
        {
            return evidence;
        }

        var normalized = responseContent.ToLowerInvariant();
        if (normalized.Contains("produto") || normalized.Contains("estoque"))
        {
            evidence.Add(new ChatEvidenceSourceDto { Source = "Módulo de Produtos/Estoque" });
        }
        else if (normalized.Contains("financeiro") || normalized.Contains("conta"))
        {
            evidence.Add(new ChatEvidenceSourceDto { Source = "Módulo Financeiro" });
        }
        else if (normalized.Contains("funcionário") || normalized.Contains("folha") || normalized.Contains("rh"))
        {
            evidence.Add(new ChatEvidenceSourceDto { Source = "Módulo RH/Folha" });
        }

        return evidence;
    }

    private List<string> GenerateSuggestions(string message)
    {
        var lowerMessage = message.ToLower();

        if (lowerMessage.Contains("produto") || lowerMessage.Contains("estoque"))
        {
            return new List<string>
            {
                "Listar todos os produtos",
                "Verificar estoque",
                "Cadastrar produto"
            };
        }

        if (lowerMessage.Contains("venda") || lowerMessage.Contains("pedido"))
        {
            return new List<string>
            {
                "Ver vendas recentes",
                "Criar nova venda",
                "Calcular total de vendas"
            };
        }

        if (lowerMessage.Contains("financeiro") || lowerMessage.Contains("conta") || lowerMessage.Contains("pagar") || lowerMessage.Contains("receber"))
        {
            return new List<string>
            {
                "Resumo financeiro",
                "Contas a pagar vencidas",
                "Contas a receber em atraso"
            };
        }

        if (lowerMessage.Contains("funcionário") || lowerMessage.Contains("rh") || lowerMessage.Contains("departamento"))
        {
            return new List<string>
            {
                "Buscar funcionário",
                "Listar departamento",
                "Quem trabalha no TI?"
            };
        }

        return new List<string>
        {
            "O que você pode fazer?",
            "Mostrar produtos",
            "Ver vendas recentes",
            "Resumo financeiro"
        };
    }
}
