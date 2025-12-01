using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Google;
using erp.DTOs.Chatbot;
using erp.Services.Chatbot.ChatbotPlugins;

namespace erp.Services.Chatbot;

public class ChatbotService : IChatbotService
{
    private readonly Kernel _kernel;
    private readonly ILogger<ChatbotService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _aiProvider;
    private readonly bool _aiConfigured;

    public ChatbotService(
        ILogger<ChatbotService> logger,
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _configuration = configuration;

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
        
        // Novos plugins
        builder.Plugins.AddFromObject(
            ActivatorUtilities.CreateInstance<AssetsPlugin>(serviceProvider), 
            "AssetsPlugin");
        
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
        List<ChatMessageDto>? conversationHistory = null)
    {
        try
        {
            // Se não tem IA configurada, usar modo fallback
            if (!_aiConfigured)
            {
                return ProcessFallbackMode(message);
            }

            // Criar histórico de conversa
            var chatHistory = new ChatHistory();
            
            // System prompt
            chatHistory.AddSystemMessage(@"Você é um assistente virtual do Pillar ERP, um sistema de gestão empresarial.
                Você tem acesso a funções para gerenciar produtos, vendas, finanças e recursos humanos.
                Seja prestativo, profissional e objetivo nas respostas.
                Quando o usuário pedir para cadastrar algo ou realizar uma ação, use as funções disponíveis.
                Sempre confirme o sucesso ou falha das operações realizadas.
                Responda em português brasileiro de forma clara e amigável.");

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
            chatHistory.AddUserMessage(message);

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

            // Gerar sugestões de ações
            var suggestions = GenerateSuggestions(message);

            return new ChatResponseDto
            {
                Response = result.Content ?? "Desculpe, não consegui processar sua mensagem.",
                Success = true,
                SuggestedActions = suggestions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar mensagem do chatbot");
            return new ChatResponseDto
            {
                Response = "Desculpe, ocorreu um erro ao processar sua mensagem. Por favor, tente novamente.",
                Success = false,
                Error = ex.Message
            };
        }
    }

    private ChatResponseDto ProcessFallbackMode(string message)
    {
        // Modo básico sem IA - detecta palavras-chave
        var lowerMessage = message.ToLower();

        if (lowerMessage.Contains("ajuda") || lowerMessage.Contains("help") || lowerMessage.Contains("o que você faz"))
        {
            return new ChatResponseDto
            {
                Response = new SystemPlugin().GetHelp(),
                Success = true
            };
        }

        if (lowerMessage.Contains("sistema") || lowerMessage.Contains("sobre"))
        {
            return new ChatResponseDto
            {
                Response = new SystemPlugin().GetSystemInfo(),
                Success = true
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
            SuggestedActions = new List<string> { "Ajuda", "Sobre o sistema" }
        };
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
