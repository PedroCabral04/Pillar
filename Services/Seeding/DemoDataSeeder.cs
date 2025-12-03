using AutoBogus;
using Bogus;
using erp.Data;
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
    public string TenantSlug { get; set; } = "demo";
}

public sealed class DemoDataSeeder
{
    private static readonly string[] PaymentMethods = ["Pix", "Boleto", "Cartão de Crédito", "Cartão de Débito"];
    private static readonly string[] SaleStatuses = ["Confirmada", "Pendente", "Enviada", "Finalizada"];

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
        var demoUser = await EnsureDemoUserAsync(tenant, cancellationToken);

        // Ensure admin user is also linked to this tenant
        var adminUser = await _userManager.FindByEmailAsync("admin@erp.local");
        if (adminUser != null && adminUser.TenantId != tenant.Id)
        {
            adminUser.TenantId = tenant.Id;
            await _userManager.UpdateAsync(adminUser);
            _logger.LogInformation("Updated admin@erp.local to belong to tenant {Tenant}", tenant.Slug);
        }

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

        var customers = await SeedCustomersAsync(tenant.Id, demoUser.Id, cancellationToken);
        await SeedSuppliersAsync(tenant.Id, demoUser.Id, cancellationToken);
        var products = await SeedProductsAsync(tenant.Id, demoUser.Id, cancellationToken);
        await SeedSalesAsync(demoUser.Id, customers, products, cancellationToken);

        _logger.LogInformation(
            "Demo data seeded: {Customers} customers, {Suppliers} suppliers, {Products} products, {Sales} sales.",
            customers.Count,
            _options.Suppliers,
            products.Count,
            _options.Sales);
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

    private async Task<ApplicationUser> EnsureDemoUserAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        var email = $"demo@{tenant.Slug}.local";
        var user = await _userManager.FindByEmailAsync(email);
        if (user is not null)
        {
            if (user.TenantId != tenant.Id)
            {
                user.TenantId = tenant.Id;
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
            FullName = "Usuário Demo",
            PhoneNumber = "+5511999999999",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, "Demo@123!");
        if (!result.Succeeded)
        {
            var message = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Falha ao criar usuário demo: {message}");
        }

        await _userManager.AddToRoleAsync(user, "Administrador");
        return user;
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

    private async Task SeedSuppliersAsync(int tenantId, int createdByUserId, CancellationToken cancellationToken)
    {
        if (_options.Suppliers <= 0)
        {
            return;
        }

        var faker = BuildSupplierFaker(tenantId, createdByUserId);
        var suppliers = faker.Generate(_options.Suppliers);

        await _db.Suppliers.AddRangeAsync(suppliers, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
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
            .RuleFor(s => s.Category, f => f.Commerce.Department())
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
