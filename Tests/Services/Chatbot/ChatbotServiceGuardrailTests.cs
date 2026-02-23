using erp.Data;
using erp.DTOs.Chatbot;
using erp.Services.Chatbot;
using erp.Services.Financial;
using erp.Services.Inventory;
using erp.Services.Payroll;
using erp.Services.Sales;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace erp.Tests.Services.Chatbot;

public class ChatbotServiceGuardrailTests
{
    [Fact]
    public async Task ExecuteWithConfirmation_WithoutPendingChallenge_DoesNotGrantConfirmation()
    {
        var setup = BuildService();

        var response = await setup.Service.ProcessMessageAsync(
            "Confirmar: excluir cliente ACME",
            null,
            10,
            ChatOperationMode.ExecuteWithConfirmation,
            ChatResponseStyle.Executive,
            false,
            1,
            "conversation");

        response.RequiresConfirmation.Should().BeFalse();
        response.Response.Should().Contain("Não há confirmação pendente");
    }

    [Fact]
    public async Task ExecuteWithConfirmation_WithPendingChallenge_AllowsMatchingConfirmation()
    {
        var setup = BuildService();

        var challenge = await setup.Service.ProcessMessageAsync(
            "excluir cliente ACME",
            null,
            20,
            ChatOperationMode.ExecuteWithConfirmation,
            ChatResponseStyle.Executive,
            false,
            2,
            "conversation");

        challenge.RequiresConfirmation.Should().BeTrue();

        var confirmed = await setup.Service.ProcessMessageAsync(
            "Confirmar: excluir cliente ACME",
            null,
            20,
            ChatOperationMode.ExecuteWithConfirmation,
            ChatResponseStyle.Executive,
            false,
            2,
            "conversation");

        confirmed.RequiresConfirmation.Should().BeFalse();
        confirmed.Response.Should().NotContain("Não há confirmação pendente");
        confirmed.Response.Should().NotContain("Confirmação necessária");
    }

    [Fact]
    public async Task ExecuteWithConfirmation_ReadOnlyQuestionAboutContasAPagar_DoesNotTriggerConfirmation()
    {
        var setup = BuildService();

        var response = await setup.Service.ProcessMessageAsync(
            "quais contas a pagar vencem hoje?",
            null,
            30,
            ChatOperationMode.ExecuteWithConfirmation,
            ChatResponseStyle.Executive,
            false,
            3,
            "conversation");

        response.RequiresConfirmation.Should().BeFalse();
        response.Response.Should().NotContain("Confirmação necessária");
    }

    private static (ChatbotService Service, ApplicationDbContext Context) BuildService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AI:Provider"] = "openai"
            })
            .Build();

        var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbContext = new ApplicationDbContext(dbOptions);

        var logger = new Mock<ILogger<ChatbotService>>();
        var cacheService = new Mock<IChatbotCacheService>();
        cacheService.SetupGet(x => x.IsEnabled).Returns(false);

        var userContext = new Mock<IChatbotUserContext>();
        var auditService = new Mock<IChatbotAuditService>();
        auditService.Setup(x => x.LogAsync(It.IsAny<ChatbotAuditRequest>())).Returns(Task.CompletedTask);

        var confirmationService = new ChatbotConfirmationService(new MemoryCache(new MemoryCacheOptions()));

        var services = new ServiceCollection();
        services.AddSingleton(cacheService.Object);
        services.AddSingleton(userContext.Object);
        services.AddSingleton(dbContext);
        services.AddSingleton(new Mock<IInventoryService>().Object);
        services.AddSingleton(new Mock<ISalesService>().Object);
        services.AddSingleton(new Mock<IAccountPayableService>().Object);
        services.AddSingleton(new Mock<IAccountReceivableService>().Object);
        services.AddSingleton(new Mock<IFinancialCategoryService>().Object);
        services.AddSingleton(new Mock<ISupplierService>().Object);
        services.AddSingleton(new Mock<ICustomerService>().Object);
        services.AddSingleton(new Mock<IPayrollService>().Object);

        var serviceProvider = services.BuildServiceProvider();

        var chatbotService = new ChatbotService(
            logger.Object,
            configuration,
            serviceProvider,
            cacheService.Object,
            confirmationService,
            userContext.Object,
            auditService.Object);

        return (chatbotService, dbContext);
    }
}
