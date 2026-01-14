using AutoBogus;
using Bogus;
using erp.Data;
using erp.Models;
using erp.Models.Financial;
using erp.Models.Identity;
using erp.Models.Inventory;
using erp.Models.Sales;
using erp.Models.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace erp.Services.Seeding;

public sealed class DemoSeedOptions
{
    public bool Enabled { get; set; }
    public bool Force { get; set; }
    public int Products { get; set; } = 40;
    public int Customers { get; set; } = 25;
    public int Suppliers { get; set; } = 10;
    public int Sales { get; set; } = 30;
    public int AccountsPayable { get; set; } = 15;
    public int AccountsReceivable { get; set; } = 20;
    public string TenantSlug { get; set; } = "demo";
}

public sealed class DemoDataSeeder
{
    private static readonly string[] PaymentMethods = ["Pix", "Boleto", "Cartão de Crédito", "Cartão de Débito"];
    private static readonly string[] SaleStatuses = ["Pendente", "Finalizada"];

    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DemoDataSeeder> _logger;
    private readonly DemoSeedOptions _options;

    public DemoDataSeeder(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IOptions<DemoSeedOptions> options,
        ILogger<DemoDataSeeder> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
        _options = options.Value;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Demo data seeding is disabled.");
            return;
        }

        var tenant = await EnsureTenantAsync(cancellationToken);
        
        // Seed organizational structure first (departments, positions)
        var departments = await SeedDepartmentsAsync(cancellationToken);
        var positions = await SeedPositionsAsync(departments, cancellationToken);
        
        // Now create/update demo user with department and position
        var demoUser = await EnsureDemoUserAsync(tenant, departments, positions, cancellationToken);

        // Ensure admin user is also linked to this tenant and has department/position
        await UpdateAdminUserAsync(tenant, departments, positions, cancellationToken);

        if (!_options.Force && await _db.Products.AnyAsync(p => p.TenantId == tenant.Id, cancellationToken))
        {
            _logger.LogInformation("Demo data already exists for tenant {Tenant}. Set DemoSeed:Force=true to regenerate.", tenant.Slug);
            return;
        }

        if (_options.Force)
        {
            await PurgeDemoDataAsync(tenant.Id, cancellationToken);
        }

        await SeedLookupsAsync(cancellationToken);
        
        // Seed financial structure
        var financialCategories = await SeedFinancialCategoriesAsync(cancellationToken);
        var costCenters = await SeedCostCentersAsync(departments, demoUser.Id, cancellationToken);
        
        // Seed asset categories
        await SeedAssetCategoriesAsync(cancellationToken);
        await SeedAssetsAsync(cancellationToken);

        var customers = await SeedCustomersAsync(tenant.Id, demoUser.Id, cancellationToken);
        var suppliers = await SeedSuppliersAsync(tenant.Id, demoUser.Id, cancellationToken);
        var products = await SeedProductsAsync(tenant.Id, demoUser.Id, cancellationToken);
        await SeedSalesAsync(demoUser.Id, customers, products, cancellationToken);
        
        // Seed financial transactions
        await SeedAccountsPayableAsync(suppliers, financialCategories, costCenters, demoUser.Id, cancellationToken);
        await SeedAccountsReceivableAsync(customers, financialCategories, costCenters, demoUser.Id, cancellationToken);

        _logger.LogInformation(
            "Demo data seeded: {Departments} departments, {Positions} positions, {Customers} customers, {Suppliers} suppliers, {Products} products, {Sales} sales, {AP} accounts payable, {AR} accounts receivable.",
            departments.Count,
            positions.Count,
            customers.Count,
            suppliers.Count,
            products.Count,
            _options.Sales,
            _options.AccountsPayable,
            _options.AccountsReceivable);
    }

    private async Task<Tenant> EnsureTenantAsync(CancellationToken cancellationToken)
    {
        var slug = string.IsNullOrWhiteSpace(_options.TenantSlug)
            ? "demo"
            : _options.TenantSlug.Trim().ToLowerInvariant();

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);
        if (tenant is not null)
        {
            return tenant;
        }

        tenant = new Tenant
        {
            Name = "Pillar Demo",
            Slug = slug,
            DocumentNumber = "00000000000191",
            PrimaryContactName = "Equipe Demo",
            PrimaryContactEmail = $"demo@{slug}.local",
            Status = TenantStatus.Active,
            IsDemo = true,
            CreatedAt = DateTime.UtcNow,
            ActivatedAt = DateTime.UtcNow
        };

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Demo tenant {Tenant} created.", slug);

        return tenant;
    }

    private async Task<List<Department>> SeedDepartmentsAsync(CancellationToken cancellationToken)
    {
        if (await _db.Departments.AnyAsync(cancellationToken))
        {
            return await _db.Departments.ToListAsync(cancellationToken);
        }

        var departments = new List<Department>
        {
            new() { Name = "Diretoria", Code = "DIR", Description = "Diretoria Executiva", IsActive = true, CostCenter = "CC-001" },
            new() { Name = "Tecnologia da Informação", Code = "TI", Description = "Departamento de TI e Infraestrutura", IsActive = true, CostCenter = "CC-002" },
            new() { Name = "Recursos Humanos", Code = "RH", Description = "Gestão de Pessoas e Benefícios", IsActive = true, CostCenter = "CC-003" },
            new() { Name = "Financeiro", Code = "FIN", Description = "Controladoria e Finanças", IsActive = true, CostCenter = "CC-004" },
            new() { Name = "Comercial", Code = "COM", Description = "Vendas e Relacionamento com Clientes", IsActive = true, CostCenter = "CC-005" },
            new() { Name = "Operações", Code = "OPS", Description = "Operações e Logística", IsActive = true, CostCenter = "CC-006" },
            new() { Name = "Administrativo", Code = "ADM", Description = "Suporte Administrativo Geral", IsActive = true, CostCenter = "CC-007" },
            new() { Name = "Jurídico", Code = "JUR", Description = "Assessoria Jurídica e Compliance", IsActive = true, CostCenter = "CC-008" },
            new() { Name = "Marketing", Code = "MKT", Description = "Marketing e Comunicação", IsActive = true, CostCenter = "CC-009" },
        };

        await _db.Departments.AddRangeAsync(departments, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded {Count} departments.", departments.Count);

        return departments;
    }

    private async Task<List<Position>> SeedPositionsAsync(List<Department> departments, CancellationToken cancellationToken)
    {
        if (await _db.Positions.AnyAsync(cancellationToken))
        {
            return await _db.Positions.ToListAsync(cancellationToken);
        }

        var dirDept = departments.FirstOrDefault(d => d.Code == "DIR");
        var tiDept = departments.FirstOrDefault(d => d.Code == "TI");
        var rhDept = departments.FirstOrDefault(d => d.Code == "RH");
        var finDept = departments.FirstOrDefault(d => d.Code == "FIN");
        var comDept = departments.FirstOrDefault(d => d.Code == "COM");

        var positions = new List<Position>
        {
            // Nível Executivo (5)
            new() { Title = "Diretor Geral", Code = "DIR-GER", Level = 5, MinSalary = 25000, MaxSalary = 50000, DefaultDepartmentId = dirDept?.Id, Description = "Responsável pela gestão geral da empresa", IsActive = true },
            new() { Title = "Diretor de TI", Code = "DIR-TI", Level = 5, MinSalary = 20000, MaxSalary = 40000, DefaultDepartmentId = tiDept?.Id, Description = "Responsável pela estratégia de tecnologia", IsActive = true },
            new() { Title = "Diretor Financeiro", Code = "DIR-FIN", Level = 5, MinSalary = 20000, MaxSalary = 40000, DefaultDepartmentId = finDept?.Id, Description = "CFO - Responsável pelas finanças", IsActive = true },
            new() { Title = "Diretor Comercial", Code = "DIR-COM", Level = 5, MinSalary = 20000, MaxSalary = 40000, DefaultDepartmentId = comDept?.Id, Description = "Responsável pela área comercial", IsActive = true },
            
            // Nível Gerência (4)
            new() { Title = "Gerente de TI", Code = "GER-TI", Level = 4, MinSalary = 12000, MaxSalary = 20000, DefaultDepartmentId = tiDept?.Id, Description = "Gerenciamento da equipe de TI", IsActive = true },
            new() { Title = "Gerente de RH", Code = "GER-RH", Level = 4, MinSalary = 10000, MaxSalary = 18000, DefaultDepartmentId = rhDept?.Id, Description = "Gerenciamento de Recursos Humanos", IsActive = true },
            new() { Title = "Gerente Financeiro", Code = "GER-FIN", Level = 4, MinSalary = 12000, MaxSalary = 20000, DefaultDepartmentId = finDept?.Id, Description = "Gerenciamento financeiro e contábil", IsActive = true },
            new() { Title = "Gerente Comercial", Code = "GER-COM", Level = 4, MinSalary = 12000, MaxSalary = 22000, DefaultDepartmentId = comDept?.Id, Description = "Gerenciamento da equipe de vendas", IsActive = true },
            
            // Nível Coordenação (3)
            new() { Title = "Coordenador de Desenvolvimento", Code = "CRD-DEV", Level = 3, MinSalary = 8000, MaxSalary = 14000, DefaultDepartmentId = tiDept?.Id, Description = "Coordenação da equipe de desenvolvimento", IsActive = true },
            new() { Title = "Coordenador de Infraestrutura", Code = "CRD-INFRA", Level = 3, MinSalary = 7000, MaxSalary = 12000, DefaultDepartmentId = tiDept?.Id, Description = "Coordenação de infraestrutura e suporte", IsActive = true },
            new() { Title = "Coordenador de Vendas", Code = "CRD-VEN", Level = 3, MinSalary = 6000, MaxSalary = 12000, DefaultDepartmentId = comDept?.Id, Description = "Coordenação da equipe de vendas", IsActive = true },
            
            // Nível Analista Sênior (2.5)
            new() { Title = "Analista Sênior de Sistemas", Code = "SR-SIS", Level = 3, MinSalary = 8000, MaxSalary = 14000, DefaultDepartmentId = tiDept?.Id, Description = "Análise e desenvolvimento de sistemas", IsActive = true },
            new() { Title = "Analista Sênior Financeiro", Code = "SR-FIN", Level = 3, MinSalary = 7000, MaxSalary = 12000, DefaultDepartmentId = finDept?.Id, Description = "Análise financeira avançada", IsActive = true },
            
            // Nível Analista Pleno (2)
            new() { Title = "Analista de Sistemas", Code = "AN-SIS", Level = 2, MinSalary = 5000, MaxSalary = 9000, DefaultDepartmentId = tiDept?.Id, Description = "Análise e desenvolvimento de sistemas", IsActive = true },
            new() { Title = "Analista de RH", Code = "AN-RH", Level = 2, MinSalary = 4000, MaxSalary = 7000, DefaultDepartmentId = rhDept?.Id, Description = "Análise de processos de RH", IsActive = true },
            new() { Title = "Analista Financeiro", Code = "AN-FIN", Level = 2, MinSalary = 4500, MaxSalary = 8000, DefaultDepartmentId = finDept?.Id, Description = "Análise financeira e contábil", IsActive = true },
            new() { Title = "Analista Comercial", Code = "AN-COM", Level = 2, MinSalary = 4000, MaxSalary = 8000, DefaultDepartmentId = comDept?.Id, Description = "Suporte à equipe comercial", IsActive = true },
            
            // Nível Júnior (1.5)
            new() { Title = "Analista Júnior de TI", Code = "JR-TI", Level = 2, MinSalary = 3000, MaxSalary = 5000, DefaultDepartmentId = tiDept?.Id, Description = "Suporte e desenvolvimento inicial", IsActive = true },
            new() { Title = "Assistente Administrativo", Code = "AST-ADM", Level = 1, MinSalary = 2000, MaxSalary = 3500, Description = "Suporte administrativo geral", IsActive = true },
            new() { Title = "Assistente Financeiro", Code = "AST-FIN", Level = 1, MinSalary = 2200, MaxSalary = 3800, DefaultDepartmentId = finDept?.Id, Description = "Suporte ao departamento financeiro", IsActive = true },
            
            // Nível Estagiário (1)
            new() { Title = "Estagiário de TI", Code = "EST-TI", Level = 1, MinSalary = 1200, MaxSalary = 2000, DefaultDepartmentId = tiDept?.Id, Description = "Estágio em tecnologia", IsActive = true },
            new() { Title = "Estagiário Administrativo", Code = "EST-ADM", Level = 1, MinSalary = 1000, MaxSalary = 1800, Description = "Estágio administrativo", IsActive = true },
        };

        await _db.Positions.AddRangeAsync(positions, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded {Count} positions.", positions.Count);

        return positions;
    }

    private async Task<ApplicationUser> EnsureDemoUserAsync(Tenant tenant, List<Department> departments, List<Position> positions, CancellationToken cancellationToken)
    {
        var email = $"demo@{tenant.Slug}.local";
        var user = await _userManager.FindByEmailAsync(email);
        
        // Get a suitable department and position for demo user
        var tiDept = departments.FirstOrDefault(d => d.Code == "TI");
        var analystPosition = positions.FirstOrDefault(p => p.Code == "AN-SIS");
        
        if (user is not null)
        {
            bool needsUpdate = false;
            
            if (user.TenantId != tenant.Id)
            {
                user.TenantId = tenant.Id;
                needsUpdate = true;
            }
            
            if (user.DepartmentId == null && tiDept != null)
            {
                user.DepartmentId = tiDept.Id;
                needsUpdate = true;
            }
            
            if (user.PositionId == null && analystPosition != null)
            {
                user.PositionId = analystPosition.Id;
                needsUpdate = true;
            }
            
            if (needsUpdate)
            {
                await _userManager.UpdateAsync(user);
            }

            return user;
        }

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            TenantId = tenant.Id,
            FullName = "Usuário Demonstração",
            PhoneNumber = "+5511999999999",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            HireDate = DateTime.UtcNow.AddYears(-2),
            DepartmentId = tiDept?.Id,
            PositionId = analystPosition?.Id,
            ContractType = "CLT",
            EmploymentStatus = "Ativo",
            Salary = 6500
        };

        var result = await _userManager.CreateAsync(user, "Demo@123!");
        if (!result.Succeeded)
        {
            var message = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Falha ao criar usuário demo: {message}");
        }

        await _userManager.AddToRoleAsync(user, "Vendedor");
        _logger.LogInformation("Demo user created with department {Dept} and position {Pos}", tiDept?.Name, analystPosition?.Title);
        
        return user;
    }

    private async Task UpdateAdminUserAsync(Tenant tenant, List<Department> departments, List<Position> positions, CancellationToken cancellationToken)
    {
        var adminUser = await _userManager.FindByEmailAsync("admin@erp.local");
        if (adminUser == null) return;

        bool needsUpdate = false;

        // Update tenant if needed
        if (adminUser.TenantId != tenant.Id)
        {
            adminUser.TenantId = tenant.Id;
            needsUpdate = true;
        }

        // Assign to Diretoria department
        var dirDept = departments.FirstOrDefault(d => d.Code == "DIR");
        if (adminUser.DepartmentId == null && dirDept != null)
        {
            adminUser.DepartmentId = dirDept.Id;
            needsUpdate = true;
        }

        // Assign Diretor Geral position
        var directorPosition = positions.FirstOrDefault(p => p.Code == "DIR-GER");
        if (adminUser.PositionId == null && directorPosition != null)
        {
            adminUser.PositionId = directorPosition.Id;
            needsUpdate = true;
        }

        // Set FullName if not set
        if (string.IsNullOrEmpty(adminUser.FullName))
        {
            adminUser.FullName = "Administrador do Sistema";
            needsUpdate = true;
        }

        // Set HireDate if not set
        if (adminUser.HireDate == null)
        {
            adminUser.HireDate = DateTime.UtcNow.AddYears(-5);
            needsUpdate = true;
        }

        // Set professional info
        if (string.IsNullOrEmpty(adminUser.ContractType))
        {
            adminUser.ContractType = "CLT";
            adminUser.EmploymentStatus = "Ativo";
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            await _userManager.UpdateAsync(adminUser);
            _logger.LogInformation("Updated admin@erp.local with tenant {Tenant}, department {Dept}, position {Pos}",
                tenant.Slug, dirDept?.Name, directorPosition?.Title);
        }
    }

    private async Task PurgeDemoDataAsync(int tenantId, CancellationToken cancellationToken)
    {
        var customerIds = await _db.Customers
            .Where(c => c.TenantId == tenantId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        if (customerIds.Count > 0)
        {
            var saleIds = await _db.Sales
                .Where(s => s.CustomerId.HasValue && customerIds.Contains(s.CustomerId.Value))
                .Select(s => s.Id)
                .ToListAsync(cancellationToken);

            if (saleIds.Count > 0)
            {
                var saleItems = await _db.SaleItems
                    .Where(i => saleIds.Contains(i.SaleId))
                    .ToListAsync(cancellationToken);
                _db.SaleItems.RemoveRange(saleItems);

                var sales = await _db.Sales
                    .Where(s => saleIds.Contains(s.Id))
                    .ToListAsync(cancellationToken);
                _db.Sales.RemoveRange(sales);
            }
        }

        var products = await _db.Products
            .Where(p => p.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        var suppliers = await _db.Suppliers
            .Where(s => s.TenantId == tenantId)
            .ToListAsync(cancellationToken);
        var customers = await _db.Customers
            .Where(c => c.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        if (products.Count > 0)
        {
            _db.Products.RemoveRange(products);
        }

        if (suppliers.Count > 0)
        {
            _db.Suppliers.RemoveRange(suppliers);
        }

        if (customers.Count > 0)
        {
            _db.Customers.RemoveRange(customers);
        }

        if (_db.ChangeTracker.HasChanges())
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedLookupsAsync(CancellationToken cancellationToken)
    {
        if (!await _db.ProductCategories.AnyAsync(cancellationToken))
        {
            var categories = new[]
            {
                new ProductCategory { Name = "Eletrônicos", Code = "ELEC" },
                new ProductCategory { Name = "Escritório", Code = "OFF" },
                new ProductCategory { Name = "Serviços", Code = "SERV" },
                new ProductCategory { Name = "Acessórios", Code = "ACC" }
            };

            await _db.ProductCategories.AddRangeAsync(categories, cancellationToken);
        }

        if (!await _db.Brands.AnyAsync(cancellationToken))
        {
            var brands = new[]
            {
                new Brand { Name = "Pillar" },
                new Brand { Name = "Apex" },
                new Brand { Name = "Vertex" }
            };

            await _db.Brands.AddRangeAsync(brands, cancellationToken);
        }

        if (!await _db.Warehouses.AnyAsync(cancellationToken))
        {
            var warehouses = new[]
            {
                new Warehouse { Name = "Matriz", Code = "MAT" },
                new Warehouse { Name = "Filial SP", Code = "SP01" }
            };

            await _db.Warehouses.AddRangeAsync(warehouses, cancellationToken);
        }

        if (_db.ChangeTracker.HasChanges())
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<List<FinancialCategory>> SeedFinancialCategoriesAsync(CancellationToken cancellationToken)
    {
        if (await _db.FinancialCategories.AnyAsync(cancellationToken))
        {
            return await _db.FinancialCategories.ToListAsync(cancellationToken);
        }

        // Parent categories
        var revenueParent = new FinancialCategory { Name = "Receitas", Code = "1", Type = CategoryType.Revenue, Description = "Todas as receitas da empresa" };
        var expenseParent = new FinancialCategory { Name = "Despesas", Code = "2", Type = CategoryType.Expense, Description = "Todas as despesas da empresa" };

        _db.FinancialCategories.AddRange(revenueParent, expenseParent);
        await _db.SaveChangesAsync(cancellationToken);

        // Revenue subcategories
        var revenueCategories = new List<FinancialCategory>
        {
            new() { Name = "Vendas de Produtos", Code = "1.1", Type = CategoryType.Revenue, ParentCategoryId = revenueParent.Id, Description = "Receita com venda de produtos" },
            new() { Name = "Prestação de Serviços", Code = "1.2", Type = CategoryType.Revenue, ParentCategoryId = revenueParent.Id, Description = "Receita com serviços prestados" },
            new() { Name = "Receitas Financeiras", Code = "1.3", Type = CategoryType.Revenue, ParentCategoryId = revenueParent.Id, Description = "Juros, rendimentos e aplicações" },
            new() { Name = "Outras Receitas", Code = "1.9", Type = CategoryType.Revenue, ParentCategoryId = revenueParent.Id, Description = "Receitas diversas" },
        };

        // Expense subcategories
        var expenseCategories = new List<FinancialCategory>
        {
            new() { Name = "Folha de Pagamento", Code = "2.1", Type = CategoryType.Expense, ParentCategoryId = expenseParent.Id, Description = "Salários, encargos e benefícios" },
            new() { Name = "Aluguel e Condomínio", Code = "2.2", Type = CategoryType.Expense, ParentCategoryId = expenseParent.Id, Description = "Custos de locação" },
            new() { Name = "Energia e Utilities", Code = "2.3", Type = CategoryType.Expense, ParentCategoryId = expenseParent.Id, Description = "Energia, água, gás, telefone, internet" },
            new() { Name = "Material de Escritório", Code = "2.4", Type = CategoryType.Expense, ParentCategoryId = expenseParent.Id, Description = "Suprimentos de escritório" },
            new() { Name = "Manutenção e Reparos", Code = "2.5", Type = CategoryType.Expense, ParentCategoryId = expenseParent.Id, Description = "Manutenção de equipamentos e instalações" },
            new() { Name = "Marketing e Publicidade", Code = "2.6", Type = CategoryType.Expense, ParentCategoryId = expenseParent.Id, Description = "Campanhas e material promocional" },
            new() { Name = "Impostos e Taxas", Code = "2.7", Type = CategoryType.Expense, ParentCategoryId = expenseParent.Id, Description = "Tributos diversos" },
            new() { Name = "Fornecedores", Code = "2.8", Type = CategoryType.Expense, ParentCategoryId = expenseParent.Id, Description = "Compras de mercadorias e insumos" },
            new() { Name = "Despesas Financeiras", Code = "2.9", Type = CategoryType.Expense, ParentCategoryId = expenseParent.Id, Description = "Juros, taxas bancárias" },
            new() { Name = "Viagens e Deslocamentos", Code = "2.10", Type = CategoryType.Expense, ParentCategoryId = expenseParent.Id, Description = "Passagens, hospedagem, combustível" },
            new() { Name = "Software e Tecnologia", Code = "2.11", Type = CategoryType.Expense, ParentCategoryId = expenseParent.Id, Description = "Licenças, assinaturas, serviços de TI" },
            new() { Name = "Outras Despesas", Code = "2.99", Type = CategoryType.Expense, ParentCategoryId = expenseParent.Id, Description = "Despesas diversas" },
        };

        var allCategories = new List<FinancialCategory> { revenueParent, expenseParent };
        allCategories.AddRange(revenueCategories);
        allCategories.AddRange(expenseCategories);

        _db.FinancialCategories.AddRange(revenueCategories);
        _db.FinancialCategories.AddRange(expenseCategories);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} financial categories.", allCategories.Count);
        return allCategories;
    }

    private async Task<List<CostCenter>> SeedCostCentersAsync(List<Department> departments, int createdByUserId, CancellationToken cancellationToken)
    {
        if (await _db.CostCenters.AnyAsync(cancellationToken))
        {
            return await _db.CostCenters.ToListAsync(cancellationToken);
        }

        var costCenters = departments.Select(d => new CostCenter
        {
            Name = d.Name,
            Code = d.CostCenter ?? $"CC-{d.Code}",
            Description = $"Centro de custo do departamento {d.Name}",
            ManagerUserId = d.ManagerId,
            MonthlyBudget = d.Code switch
            {
                "DIR" => 50000m,
                "TI" => 35000m,
                "RH" => 25000m,
                "FIN" => 30000m,
                "COM" => 40000m,
                "OPS" => 45000m,
                "MKT" => 20000m,
                _ => 15000m
            },
            IsActive = true
        }).ToList();

        await _db.CostCenters.AddRangeAsync(costCenters, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} cost centers.", costCenters.Count);
        return costCenters;
    }

    private async Task SeedAssetCategoriesAsync(CancellationToken cancellationToken)
    {
        if (await _db.AssetCategories.AnyAsync(cancellationToken))
        {
            return;
        }

        var categories = new List<AssetCategory>
        {
            new() { Name = "Computadores", Description = "Desktops, workstations", Icon = "Computer" },
            new() { Name = "Notebooks", Description = "Laptops e ultrabooks", Icon = "Laptop" },
            new() { Name = "Celulares", Description = "Smartphones corporativos", Icon = "PhoneAndroid" },
            new() { Name = "Tablets", Description = "Tablets e iPads", Icon = "Tablet" },
            new() { Name = "Monitores", Description = "Monitores e telas", Icon = "Monitor" },
            new() { Name = "Impressoras", Description = "Impressoras e multifuncionais", Icon = "Print" },
            new() { Name = "Mobiliário", Description = "Mesas, cadeiras, armários", Icon = "Chair" },
            new() { Name = "Veículos", Description = "Carros, motos, utilitários", Icon = "DirectionsCar" },
            new() { Name = "Ferramentas", Description = "Ferramentas e equipamentos", Icon = "Construction" },
            new() { Name = "Ar Condicionado", Description = "Climatizadores e ar condicionado", Icon = "AcUnit" },
            new() { Name = "Eletrodomésticos", Description = "Geladeira, micro-ondas, cafeteira", Icon = "Kitchen" },
            new() { Name = "Audiovisual", Description = "TVs, projetores, câmeras", Icon = "Videocam" },
            new() { Name = "Equipamentos de Rede", Description = "Switches, roteadores, access points", Icon = "Router" },
            new() { Name = "Segurança", Description = "Câmeras, alarmes, controle de acesso", Icon = "Security" },
            new() { Name = "Outros", Description = "Outros ativos não categorizados", Icon = "Category" },
        };

        await _db.AssetCategories.AddRangeAsync(categories, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} asset categories.", categories.Count);
    }

    private async Task SeedAssetsAsync(CancellationToken cancellationToken)
    {
        if (await _db.Assets.AnyAsync(cancellationToken))
        {
            return;
        }

        var categories = await _db.AssetCategories.ToListAsync(cancellationToken);
        if (categories.Count == 0) return;

        var faker = new Faker("pt_BR");
        var assets = new List<Asset>();

        for (int i = 0; i < 5; i++)
        {
            var category = categories[faker.Random.Int(0, categories.Count - 1)];
            var purchaseDate = DateTime.UtcNow.AddMonths(-faker.Random.Int(1, 36));

            var asset = new Asset
            {
                AssetCode = $"ATV-{faker.Random.AlphaNumeric(6).ToUpper()}",
                Name = faker.Commerce.ProductName(),
                Description = faker.Commerce.ProductDescription(),
                CategoryId = category.Id,
                SerialNumber = faker.Random.AlphaNumeric(10).ToUpper(),
                Manufacturer = faker.Company.CompanyName(),
                Model = faker.Commerce.ProductAdjective() + " " + faker.Random.Number(100, 900),
                PurchaseDate = purchaseDate,
                PurchaseValue = Math.Round(faker.Random.Decimal(500, 15000), 2),
                Status = faker.PickRandom<AssetStatus>(),
                Condition = faker.PickRandom<AssetCondition>(),
                Location = faker.PickRandom(new[] { "Escritório Central", "Filial SP", "Almoxarifado", "Sala de Reunião" }),
                WarrantyExpiryDate = purchaseDate.AddYears(faker.Random.Int(1, 3)),
                ExpectedLifespanMonths = faker.Random.Int(24, 60),
                Notes = faker.Lorem.Sentence(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            
            assets.Add(asset);
        }

        await _db.Assets.AddRangeAsync(assets, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Seeded {Count} assets.", assets.Count);
    }

    private async Task SeedAccountsPayableAsync(
        List<Supplier> suppliers, 
        List<FinancialCategory> categories, 
        List<CostCenter> costCenters, 
        int createdByUserId, 
        CancellationToken cancellationToken)
    {
        if (_options.AccountsPayable <= 0 || suppliers.Count == 0)
        {
            return;
        }

        var faker = new Faker("pt_BR");
        var expenseCategories = categories.Where(c => c.Type == CategoryType.Expense && c.ParentCategoryId != null).ToList();
        var accounts = new List<AccountPayable>();

        var descriptions = new[]
        {
            "Compra de materiais",
            "Serviço de manutenção",
            "Licenças de software",
            "Material de escritório",
            "Serviços de consultoria",
            "Equipamentos de TI",
            "Serviço de limpeza",
            "Aluguel mensal",
            "Conta de energia",
            "Serviço de internet"
        };

        for (var i = 0; i < _options.AccountsPayable; i++)
        {
            var supplier = suppliers[faker.Random.Int(0, suppliers.Count - 1)];
            var issueDate = DateTime.UtcNow.AddDays(-faker.Random.Int(1, 60));
            var dueDate = issueDate.AddDays(faker.Random.Int(15, 45));
            var amount = Math.Round(faker.Random.Decimal(500, 15000), 2);
            var isPaid = faker.Random.Bool(0.4f);

            var account = new AccountPayable
            {
                SupplierId = supplier.Id,
                InvoiceNumber = $"NF-{faker.Random.Int(10000, 99999)}",
                OriginalAmount = amount,
                DiscountAmount = isPaid ? Math.Round(amount * faker.Random.Decimal(0, 0.05m), 2) : 0,
                IssueDate = issueDate,
                DueDate = dueDate,
                PaymentDate = isPaid ? dueDate.AddDays(-faker.Random.Int(0, 5)) : null,
                Status = isPaid ? AccountStatus.Paid : (dueDate < DateTime.UtcNow ? AccountStatus.Overdue : AccountStatus.Pending),
                PaymentMethod = faker.PickRandom<PaymentMethod>(),
                CategoryId = expenseCategories.Count > 0 ? expenseCategories[faker.Random.Int(0, expenseCategories.Count - 1)].Id : null,
                CostCenterId = costCenters.Count > 0 ? costCenters[faker.Random.Int(0, costCenters.Count - 1)].Id : null,
                Notes = faker.PickRandom(descriptions),
                CreatedByUserId = createdByUserId,
                CreatedAt = issueDate
            };

            if (isPaid)
            {
                account.PaidAmount = account.NetAmount;
                account.PaidByUserId = createdByUserId;
            }

            accounts.Add(account);
        }

        await _db.AccountsPayable.AddRangeAsync(accounts, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} accounts payable.", accounts.Count);
    }

    private async Task SeedAccountsReceivableAsync(
        List<Customer> customers, 
        List<FinancialCategory> categories, 
        List<CostCenter> costCenters, 
        int createdByUserId, 
        CancellationToken cancellationToken)
    {
        if (_options.AccountsReceivable <= 0 || customers.Count == 0)
        {
            return;
        }

        var faker = new Faker("pt_BR");
        var revenueCategories = categories.Where(c => c.Type == CategoryType.Revenue && c.ParentCategoryId != null).ToList();
        var accounts = new List<AccountReceivable>();

        for (var i = 0; i < _options.AccountsReceivable; i++)
        {
            var customer = customers[faker.Random.Int(0, customers.Count - 1)];
            var issueDate = DateTime.UtcNow.AddDays(-faker.Random.Int(1, 45));
            var dueDate = issueDate.AddDays(faker.Random.Int(15, 30));
            var amount = Math.Round(faker.Random.Decimal(1000, 25000), 2);
            var isPaid = faker.Random.Bool(0.5f);

            var account = new AccountReceivable
            {
                CustomerId = customer.Id,
                InvoiceNumber = $"FAT-{DateTime.UtcNow:yyMM}-{faker.Random.Int(1000, 9999)}",
                OriginalAmount = amount,
                DiscountAmount = isPaid ? Math.Round(amount * faker.Random.Decimal(0, 0.03m), 2) : 0,
                IssueDate = issueDate,
                DueDate = dueDate,
                PaymentDate = isPaid ? dueDate.AddDays(-faker.Random.Int(0, 10)) : null,
                Status = isPaid ? AccountStatus.Paid : (dueDate < DateTime.UtcNow ? AccountStatus.Overdue : AccountStatus.Pending),
                PaymentMethod = faker.PickRandom<PaymentMethod>(),
                CategoryId = revenueCategories.Count > 0 ? revenueCategories[faker.Random.Int(0, revenueCategories.Count - 1)].Id : null,
                CostCenterId = costCenters.Count > 0 ? costCenters[faker.Random.Int(0, costCenters.Count - 1)].Id : null,
                Notes = $"Faturamento ref. pedido {faker.Random.Int(1000, 9999)}",
                CreatedByUserId = createdByUserId,
                CreatedAt = issueDate
            };

            if (isPaid)
            {
                account.PaidAmount = account.NetAmount;
                account.ReceivedByUserId = createdByUserId;
            }

            accounts.Add(account);
        }

        await _db.AccountsReceivable.AddRangeAsync(accounts, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} accounts receivable.", accounts.Count);
    }

    private async Task<List<Customer>> SeedCustomersAsync(int tenantId, int createdByUserId, CancellationToken cancellationToken)
    {
        if (_options.Customers <= 0)
        {
            return [];
        }

        var faker = BuildCustomerFaker(tenantId, createdByUserId);
        var customers = faker.Generate(_options.Customers);

        await _db.Customers.AddRangeAsync(customers, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return customers;
    }

    private async Task<List<Supplier>> SeedSuppliersAsync(int tenantId, int createdByUserId, CancellationToken cancellationToken)
    {
        if (_options.Suppliers <= 0)
        {
            return [];
        }

        var faker = BuildSupplierFaker(tenantId, createdByUserId);
        var suppliers = faker.Generate(_options.Suppliers);

        await _db.Suppliers.AddRangeAsync(suppliers, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        
        return suppliers;
    }

    private async Task<List<Product>> SeedProductsAsync(int tenantId, int createdByUserId, CancellationToken cancellationToken)
    {
        if (_options.Products <= 0)
        {
            return [];
        }

        var categories = await _db.ProductCategories.AsNoTracking().ToListAsync(cancellationToken);
        var brands = await _db.Brands.AsNoTracking().ToListAsync(cancellationToken);
        var faker = BuildProductFaker(tenantId, createdByUserId, categories, brands);
        var products = faker.Generate(_options.Products);

        await _db.Products.AddRangeAsync(products, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return products;
    }

    private async Task SeedSalesAsync(int userId, IReadOnlyList<Customer> customers, IReadOnlyList<Product> products, CancellationToken cancellationToken)
    {
        if (_options.Sales <= 0 || customers.Count == 0 || products.Count == 0)
        {
            return;
        }

        var faker = new Faker("pt_BR");
        var sales = new List<Sale>();

        for (var i = 0; i < _options.Sales; i++)
        {
            var customer = customers[faker.Random.Int(0, customers.Count - 1)];
            var sale = new Sale
            {
                SaleNumber = $"PED-{DateTime.UtcNow:yyMMdd}-{faker.Random.Int(1000, 9999)}-{i}",
                CustomerId = customer.Id,
                UserId = userId,
                SaleDate = DateTime.UtcNow.AddDays(-faker.Random.Int(0, 45)),
                PaymentMethod = faker.PickRandom(PaymentMethods),
                Status = faker.PickRandom(SaleStatuses),
                Notes = faker.Lorem.Sentence()
            };

            var itemCount = faker.Random.Int(1, Math.Min(4, products.Count));
            var selectedProducts = faker.Random.Shuffle(products).Take(itemCount).ToList();

            foreach (var product in selectedProducts)
            {
                var quantity = Math.Round(faker.Random.Decimal(1, 10), 2);
                var unitPrice = product.SalePrice <= 0 ? faker.Random.Decimal(50, 8000) : product.SalePrice;
                var discount = Math.Round(unitPrice * quantity * faker.Random.Decimal(0.0m, 0.15m), 2);
                var gross = Math.Round(unitPrice * quantity, 2);
                var total = gross - discount;

                sale.Items.Add(new SaleItem
                {
                    ProductId = product.Id,
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    Discount = discount,
                    Total = total
                });

                sale.TotalAmount += gross;
                sale.DiscountAmount += discount;
            }

            sale.NetAmount = sale.TotalAmount - sale.DiscountAmount;
            sales.Add(sale);
        }

        await _db.Sales.AddRangeAsync(sales, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static Faker<Customer> BuildCustomerFaker(int tenantId, int createdByUserId)
    {
        AutoFaker.Configure(builder => builder.WithRecursiveDepth(0));

        return new AutoBogus.AutoFaker<Customer>("pt_BR")
            .RuleFor(c => c.Id, 0)
            .RuleFor(c => c.TenantId, tenantId)
            .RuleFor(c => c.Document, f => f.Random.Bool() ? GenerateDigits(f, 11) : GenerateDigits(f, 14))
            .RuleFor(c => c.Name, f => f.Company.CompanyName())
            .RuleFor(c => c.TradeName, f => f.Company.CompanySuffix())
            .RuleFor(c => c.Email, (f, c) => f.Internet.Email(c.Name.Replace(' ', '.').ToLowerInvariant()))
            .RuleFor(c => c.Phone, f => $"55{GenerateDigits(f, 10)}")
            .RuleFor(c => c.Mobile, f => $"55{GenerateDigits(f, 11)}")
            .RuleFor(c => c.ZipCode, f => GenerateDigits(f, 8))
            .RuleFor(c => c.Address, f => f.Address.StreetAddress())
            .RuleFor(c => c.Number, f => f.Random.Number(10, 9999).ToString())
            .RuleFor(c => c.Neighborhood, f => f.Address.SecondaryAddress())
            .RuleFor(c => c.City, f => f.Address.City())
            .RuleFor(c => c.State, f => f.Address.StateAbbr())
            .RuleFor(c => c.PaymentMethod, f => f.PickRandom(PaymentMethods))
            .RuleFor(c => c.Type, f => f.PickRandom<CustomerType>())
            .RuleFor(c => c.CreatedByUserId, createdByUserId)
            .RuleFor(c => c.CreatedAt, f => DateTime.UtcNow.AddDays(-f.Random.Int(5, 60)));
    }

    private static Faker<Supplier> BuildSupplierFaker(int tenantId, int createdByUserId)
    {
        AutoFaker.Configure(builder => builder.WithRecursiveDepth(0));

        return new AutoBogus.AutoFaker<Supplier>("pt_BR")
            .RuleFor(s => s.Id, 0)
            .RuleFor(s => s.TenantId, tenantId)
            .RuleFor(s => s.Name, f => f.Company.CompanyName())
            .RuleFor(s => s.TradeName, f => f.Company.CompanySuffix())
            .RuleFor(s => s.TaxId, f => GenerateDigits(f, 14))
            .RuleFor(s => s.Email, (f, s) => f.Internet.Email(s.Name.Replace(' ', '.').ToLowerInvariant()))
            .RuleFor(s => s.Phone, f => $"55{GenerateDigits(f, 10)}")
            .RuleFor(s => s.ZipCode, f => GenerateDigits(f, 8))
            .RuleFor(s => s.Street, f => f.Address.StreetAddress())
            .RuleFor(s => s.City, f => f.Address.City())
            .RuleFor(s => s.State, f => f.Address.StateAbbr())
            .RuleFor(s => s.CategoryId, (int?)null)
            .RuleFor(s => s.MinimumOrderValue, f => Math.Round(f.Random.Decimal(500, 10000), 2))
            .RuleFor(s => s.PaymentTermDays, f => f.Random.Int(15, 60))
            .RuleFor(s => s.PaymentMethod, f => f.PickRandom(PaymentMethods))
            .RuleFor(s => s.CreatedByUserId, createdByUserId)
            .RuleFor(s => s.CreatedAt, f => DateTime.UtcNow.AddDays(-f.Random.Int(3, 45)));
    }

    private static Faker<Product> BuildProductFaker(
        int tenantId,
        int createdByUserId,
        IReadOnlyList<ProductCategory> categories,
        IReadOnlyList<Brand> brands)
    {
        AutoFaker.Configure(builder => builder.WithRecursiveDepth(0));

        return new AutoBogus.AutoFaker<Product>("pt_BR")
            .RuleFor(p => p.Id, 0)
            .RuleFor(p => p.TenantId, tenantId)
            .RuleFor(p => p.Sku, f => $"SKU-{f.Random.AlphaNumeric(6).ToUpperInvariant()}")
            .RuleFor(p => p.Barcode, f => GenerateDigits(f, 13))
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
            .RuleFor(p => p.CategoryId, f => categories[f.Random.Int(0, categories.Count - 1)].Id)
            .RuleFor(p => p.BrandId, f => brands.Count == 0 ? null : brands[f.Random.Int(0, brands.Count - 1)].Id)
            .RuleFor(p => p.Unit, f => f.PickRandom(new[] { "UN", "CX", "KG" }))
            .RuleFor(p => p.UnitsPerBox, f => Math.Round(f.Random.Decimal(1, 12), 2))
            .RuleFor(p => p.Weight, f => Math.Round(f.Random.Decimal(0.1m, 15m), 2))
            .RuleFor(p => p.Length, f => Math.Round(f.Random.Decimal(5m, 120m), 2))
            .RuleFor(p => p.Width, f => Math.Round(f.Random.Decimal(5m, 80m), 2))
            .RuleFor(p => p.Height, f => Math.Round(f.Random.Decimal(2m, 60m), 2))
            .RuleFor(p => p.CurrentStock, f => f.Random.Int(1, 10))
            .RuleFor(p => p.MinimumStock, f => Math.Round(f.Random.Decimal(5, 50), 2))
            .RuleFor(p => p.MaximumStock, (f, p) => p.MinimumStock + Math.Round(f.Random.Decimal(20, 200), 2))
            .RuleFor(p => p.ReorderPoint, (f, p) => Math.Round(p.MinimumStock * 0.7m, 2))
            .RuleFor(p => p.CostPrice, f => Math.Round(f.Random.Decimal(20, 5000), 2))
            .RuleFor(p => p.SalePrice, (f, p) => Math.Round(p.CostPrice * f.Random.Decimal(1.2m, 1.8m), 2))
            .RuleFor(p => p.NcmCode, f => GenerateDigits(f, 8))
            .RuleFor(p => p.CestCode, f => GenerateDigits(f, 7))
            .RuleFor(p => p.IcmsRate, f => Math.Round(f.Random.Decimal(0, 18), 2))
            .RuleFor(p => p.IpiRate, f => Math.Round(f.Random.Decimal(0, 15), 2))
            .RuleFor(p => p.PisRate, f => Math.Round(f.Random.Decimal(0, 3.65m), 2))
            .RuleFor(p => p.CofinsRate, f => Math.Round(f.Random.Decimal(0, 7.6m), 2))
            .RuleFor(p => p.CreatedByUserId, createdByUserId)
            .RuleFor(p => p.CreatedAt, f => DateTime.UtcNow.AddDays(-f.Random.Int(1, 40)))
            .RuleFor(p => p.Suppliers, _ => new List<ProductSupplier>())
            .RuleFor(p => p.Images, _ => new List<ProductImage>())
            .RuleFor(p => p.StockMovements, _ => new List<StockMovement>());
    }

    private static string GenerateDigits(Faker faker, int length)
    {
        Span<char> buffer = stackalloc char[length];
        for (var i = 0; i < length; i++)
        {
            buffer[i] = (char)('0' + faker.Random.Int(0, 9));
        }

        return new string(buffer);
    }
}
