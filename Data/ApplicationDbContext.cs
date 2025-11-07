using erp.Models;
using erp.Models.Identity;
using erp.Models.Audit;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace erp.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, int>(options)
{
    // Mantemos os DbSets existentes se ainda forem usados em outras partes (tabelas próprias do app)
    public new DbSet<User> Users { get; set; } = null!;
    public new DbSet<Role> Roles { get; set; } = null!;
    public new DbSet<UserRole> UserRoles { get; set; } = null!;

    // Kanban
    public DbSet<Models.Kanban.KanbanBoard> KanbanBoards { get; set; } = null!;
    public DbSet<Models.Kanban.KanbanColumn> KanbanColumns { get; set; } = null!;
    public DbSet<Models.Kanban.KanbanCard> KanbanCards { get; set; } = null!;

    // Inventory
    public DbSet<Models.Inventory.Product> Products { get; set; } = null!;
    public DbSet<Models.Inventory.ProductCategory> ProductCategories { get; set; } = null!;
    public DbSet<Models.Inventory.Brand> Brands { get; set; } = null!;
    public DbSet<Models.Inventory.ProductImage> ProductImages { get; set; } = null!;
    public DbSet<Models.Inventory.ProductSupplier> ProductSuppliers { get; set; } = null!;
    public DbSet<Models.Inventory.StockMovement> StockMovements { get; set; } = null!;
    public DbSet<Models.Inventory.Warehouse> Warehouses { get; set; } = null!;
    public DbSet<Models.Inventory.StockCount> StockCounts { get; set; } = null!;
    public DbSet<Models.Inventory.StockCountItem> StockCountItems { get; set; } = null!;

    // Sales
    public DbSet<Models.Sales.Customer> Customers { get; set; } = null!;
    public DbSet<Models.Sales.Sale> Sales { get; set; } = null!;
    public DbSet<Models.Sales.SaleItem> SaleItems { get; set; } = null!;
    
    // HR Management
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<Position> Positions { get; set; } = null!;
    
    // Audit
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    
    // Serviços injetados para auditoria
    private readonly IHttpContextAccessor? _httpContextAccessor;
    
    // Construtor adicional para injeção de IHttpContextAccessor
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IHttpContextAccessor? httpContextAccessor = null) 
        : this(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseNpgsql("Host=localhost;Database=erp;Username=postgres;Password=123");

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        SetAuditProperties();
        CaptureAuditLogs();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges()
    {
        SetAuditProperties();
        CaptureAuditLogs();
        return base.SaveChanges();
    }

    private void SetAuditProperties()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is User &&
                        e.State is EntityState.Added or EntityState.Modified);

        foreach (var entityEntry in entries )
        {
            var userEntity = (User)entityEntry.Entity;
            if (entityEntry.State == EntityState.Modified)
            {
                userEntity.LastUpdatedAt = DateTime.UtcNow;
            }
            
            if (entityEntry.State == EntityState.Added)
            {
                userEntity.CreatedAt = DateTime.UtcNow;
            }
        }
    }
    
    /// <summary>
    /// Captura mudanças em entidades auditáveis e cria registros de auditoria
    /// </summary>
    private void CaptureAuditLogs()
    {
        var auditableEntries = ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditable && 
                       e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (!auditableEntries.Any())
            return;

        var userId = GetCurrentUserId();
        var userName = GetCurrentUserName();
        var ipAddress = GetCurrentIpAddress();
        var userAgent = GetCurrentUserAgent();

        foreach (var entry in auditableEntries)
        {
            var auditLog = CreateAuditLog(entry, userId, userName, ipAddress, userAgent);
            if (auditLog != null)
            {
                AuditLogs.Add(auditLog);
            }
        }
    }
    
    /// <summary>
    /// Cria um registro de auditoria para uma mudança específica
    /// </summary>
    private AuditLog? CreateAuditLog(EntityEntry entry, int? userId, string? userName, string? ipAddress, string? userAgent)
    {
        var entityName = entry.Entity.GetType().Name;
        var entityId = GetEntityId(entry);
        
        if (string.IsNullOrEmpty(entityId))
            return null;

        var action = entry.State switch
        {
            EntityState.Added => AuditAction.Create,
            EntityState.Modified => AuditAction.Update,
            EntityState.Deleted => AuditAction.Delete,
            _ => (AuditAction?)null
        };

        if (!action.HasValue)
            return null;

        var auditLog = new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action.Value,
            UserId = userId,
            UserName = userName,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow
        };

        // Serializar valores antigos e novos
        if (entry.State == EntityState.Modified)
        {
            auditLog.OldValues = SerializeOriginalValues(entry);
            auditLog.NewValues = SerializeCurrentValues(entry);
            auditLog.ChangedProperties = SerializeChangedProperties(entry);
        }
        else if (entry.State == EntityState.Added)
        {
            auditLog.NewValues = SerializeCurrentValues(entry);
        }
        else if (entry.State == EntityState.Deleted)
        {
            auditLog.OldValues = SerializeOriginalValues(entry);
        }

        return auditLog;
    }
    
    /// <summary>
    /// Obtém o ID da entidade como string
    /// </summary>
    private string? GetEntityId(EntityEntry entry)
    {
        var keyProperties = entry.Properties
            .Where(p => p.Metadata.IsPrimaryKey())
            .ToList();

        if (!keyProperties.Any())
            return null;

        if (keyProperties.Count == 1)
        {
            return keyProperties[0].CurrentValue?.ToString();
        }

        // Composite key
        return string.Join("|", keyProperties.Select(p => p.CurrentValue?.ToString() ?? "null"));
    }
    
    /// <summary>
    /// Serializa os valores originais da entidade
    /// </summary>
    private string? SerializeOriginalValues(EntityEntry entry)
    {
        var values = new Dictionary<string, object?>();
        
        foreach (var property in entry.Properties.Where(p => !p.Metadata.IsPrimaryKey()))
        {
            // Ignora propriedades de navegação
            if (property.Metadata.IsForeignKey() || property.Metadata.IsKey())
                continue;
                
            values[property.Metadata.Name] = property.OriginalValue;
        }

        return values.Any() ? JsonSerializer.Serialize(values) : null;
    }
    
    /// <summary>
    /// Serializa os valores atuais da entidade
    /// </summary>
    private string? SerializeCurrentValues(EntityEntry entry)
    {
        var values = new Dictionary<string, object?>();
        
        foreach (var property in entry.Properties.Where(p => !p.Metadata.IsPrimaryKey()))
        {
            // Ignora propriedades de navegação
            if (property.Metadata.IsForeignKey() || property.Metadata.IsKey())
                continue;
                
            values[property.Metadata.Name] = property.CurrentValue;
        }

        return values.Any() ? JsonSerializer.Serialize(values) : null;
    }
    
    /// <summary>
    /// Serializa apenas as propriedades que foram modificadas
    /// </summary>
    private string? SerializeChangedProperties(EntityEntry entry)
    {
        var changedProperties = entry.Properties
            .Where(p => p.IsModified && !p.Metadata.IsPrimaryKey())
            .Select(p => new
            {
                PropertyName = p.Metadata.Name,
                OldValue = p.OriginalValue,
                NewValue = p.CurrentValue
            })
            .ToList();

        return changedProperties.Any() ? JsonSerializer.Serialize(changedProperties) : null;
    }
    
    /// <summary>
    /// Obtém o ID do usuário atual do contexto HTTP
    /// </summary>
    private int? GetCurrentUserId()
    {
        try
        {
            var userIdClaim = _httpContextAccessor?.HttpContext?.User
                ?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
        }
        catch
        {
            // Ignora erros ao obter usuário
        }

        return null;
    }
    
    /// <summary>
    /// Obtém o nome do usuário atual do contexto HTTP
    /// </summary>
    private string? GetCurrentUserName()
    {
        try
        {
            return _httpContextAccessor?.HttpContext?.User?.Identity?.Name;
        }
        catch
        {
            // Ignora erros ao obter nome
        }

        return null;
    }
    
    /// <summary>
    /// Obtém o endereço IP do cliente
    /// </summary>
    private string? GetCurrentIpAddress()
    {
        try
        {
            var context = _httpContextAccessor?.HttpContext;
            if (context != null)
            {
                // Verifica proxy/load balancer headers
                var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    return forwardedFor.Split(',')[0].Trim();
                }
                
                return context.Connection.RemoteIpAddress?.ToString();
            }
        }
        catch
        {
            // Ignora erros ao obter IP
        }

        return null;
    }
    
    /// <summary>
    /// Obtém o User-Agent do cliente
    /// </summary>
    private string? GetCurrentUserAgent()
    {
        try
        {
            return _httpContextAccessor?.HttpContext?.Request.Headers["User-Agent"].FirstOrDefault();
        }
        catch
        {
            // Ignora erros ao obter User-Agent
        }

        return null;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

    // As entidades de Identity usam tabelas padrão: AspNetUsers, AspNetRoles, etc.
    // As entidades do app usam suas próprias tabelas: Users, Roles, UserRoles.
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(ur => new { ur.UserId, ur.RoleId });

            entity.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .IsRequired();

            entity.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .IsRequired();
    });

        // Kanban model configuration
        modelBuilder.Entity<Models.Kanban.KanbanBoard>(b =>
        {
            b.ToTable("KanbanBoards");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(x => new { x.OwnerId, x.Name });
        });

        modelBuilder.Entity<Models.Kanban.KanbanColumn>(c =>
        {
            c.ToTable("KanbanColumns");
            c.HasKey(x => x.Id);
            c.Property(x => x.Title).HasMaxLength(200).IsRequired();
            c.Property(x => x.Position).IsRequired();
            c.HasOne(x => x.Board)
                .WithMany(x => x.Columns)
                .HasForeignKey(x => x.BoardId)
                .OnDelete(DeleteBehavior.Cascade);
            c.HasIndex(x => new { x.BoardId, x.Position });
        });

        modelBuilder.Entity<Models.Kanban.KanbanCard>(t =>
        {
            t.ToTable("KanbanCards");
            t.HasKey(x => x.Id);
            t.Property(x => x.Title).HasMaxLength(300).IsRequired();
            t.Property(x => x.Description).HasMaxLength(4000);
            t.Property(x => x.Position).IsRequired();
            t.HasOne(x => x.Column)
                .WithMany(x => x.Cards)
                .HasForeignKey(x => x.ColumnId)
                .OnDelete(DeleteBehavior.Cascade);
            t.HasIndex(x => new { x.ColumnId, x.Position });
        });

        // Inventory model configuration
        ConfigureInventoryModels(modelBuilder);
        
        // Sales model configuration
        ConfigureSalesModels(modelBuilder);
        
        // HR Management model configuration
        ConfigureHRModels(modelBuilder);
        
        // Audit model configuration
        ConfigureAuditModels(modelBuilder);
    }

    private void ConfigureInventoryModels(ModelBuilder modelBuilder)
    {
        // Product
        modelBuilder.Entity<Models.Inventory.Product>(p =>
        {
            p.ToTable("Products");
            p.HasKey(x => x.Id);
            p.Property(x => x.Sku).HasMaxLength(50).IsRequired();
            p.Property(x => x.Barcode).HasMaxLength(50);
            p.Property(x => x.Name).HasMaxLength(200).IsRequired();
            p.Property(x => x.Description).HasMaxLength(2000);
            p.Property(x => x.Unit).HasMaxLength(10);
            p.Property(x => x.WarehouseLocation).HasMaxLength(100);
            p.Property(x => x.NcmCode).HasMaxLength(10);
            p.Property(x => x.CestCode).HasMaxLength(10);
            p.Property(x => x.MainImageUrl).HasMaxLength(500);
            
            // Indexes
            p.HasIndex(x => x.Sku).IsUnique();
            p.HasIndex(x => x.Barcode);
            p.HasIndex(x => x.Name);
            p.HasIndex(x => x.CategoryId);
            p.HasIndex(x => x.Status);
            
            // Relationships
            p.HasOne(x => x.Category)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
                
            p.HasOne(x => x.Brand)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.BrandId)
                .OnDelete(DeleteBehavior.SetNull);
                
            p.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ProductCategory
        modelBuilder.Entity<Models.Inventory.ProductCategory>(c =>
        {
            c.ToTable("ProductCategories");
            c.HasKey(x => x.Id);
            c.Property(x => x.Name).HasMaxLength(100).IsRequired();
            c.Property(x => x.Code).HasMaxLength(50).IsRequired();
            
            c.HasIndex(x => x.Code).IsUnique();
            c.HasIndex(x => x.ParentCategoryId);
            
            c.HasOne(x => x.ParentCategory)
                .WithMany(x => x.SubCategories)
                .HasForeignKey(x => x.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Brand
        modelBuilder.Entity<Models.Inventory.Brand>(b =>
        {
            b.ToTable("Brands");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.Description).HasMaxLength(500);
            b.Property(x => x.LogoUrl).HasMaxLength(500);
            
            b.HasIndex(x => x.Name);
        });

        // ProductImage
        modelBuilder.Entity<Models.Inventory.ProductImage>(i =>
        {
            i.ToTable("ProductImages");
            i.HasKey(x => x.Id);
            i.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
            i.Property(x => x.ThumbnailUrl).HasMaxLength(500);
            
            i.HasIndex(x => new { x.ProductId, x.Position });
            
            i.HasOne(x => x.Product)
                .WithMany(x => x.Images)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProductSupplier
        modelBuilder.Entity<Models.Inventory.ProductSupplier>(s =>
        {
            s.ToTable("ProductSuppliers");
            s.HasKey(x => x.Id);
            s.Property(x => x.SupplierName).HasMaxLength(200).IsRequired();
            s.Property(x => x.SupplierProductCode).HasMaxLength(100);
            
            s.HasIndex(x => new { x.ProductId, x.SupplierId });
            
            s.HasOne(x => x.Product)
                .WithMany(x => x.Suppliers)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Warehouse
        modelBuilder.Entity<Models.Inventory.Warehouse>(w =>
        {
            w.ToTable("Warehouses");
            w.HasKey(x => x.Id);
            w.Property(x => x.Name).HasMaxLength(100).IsRequired();
            w.Property(x => x.Code).HasMaxLength(50).IsRequired();
            w.Property(x => x.Address).HasMaxLength(500);
            
            w.HasIndex(x => x.Code).IsUnique();
        });

        // StockMovement
        modelBuilder.Entity<Models.Inventory.StockMovement>(m =>
        {
            m.ToTable("StockMovements");
            m.HasKey(x => x.Id);
            m.Property(x => x.DocumentNumber).HasMaxLength(100);
            m.Property(x => x.Notes).HasMaxLength(1000);
            
            m.HasIndex(x => x.ProductId);
            m.HasIndex(x => x.MovementDate);
            m.HasIndex(x => x.Type);
            m.HasIndex(x => x.WarehouseId);
            m.HasIndex(x => x.SaleOrderId);
            
            m.HasOne(x => x.Product)
                .WithMany(x => x.StockMovements)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
                
            m.HasOne(x => x.Warehouse)
                .WithMany(x => x.StockMovements)
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
                
            m.HasOne(x => x.SaleOrder)
                .WithMany()
                .HasForeignKey(x => x.SaleOrderId)
                .OnDelete(DeleteBehavior.Restrict);
                
            m.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // StockCount
        modelBuilder.Entity<Models.Inventory.StockCount>(c =>
        {
            c.ToTable("StockCounts");
            c.HasKey(x => x.Id);
            c.Property(x => x.CountNumber).HasMaxLength(50).IsRequired();
            c.Property(x => x.Notes).HasMaxLength(1000);
            
            c.HasIndex(x => x.CountNumber).IsUnique();
            c.HasIndex(x => x.Status);
            c.HasIndex(x => x.CountDate);
            
            c.HasOne(x => x.Warehouse)
                .WithMany(x => x.StockCounts)
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
                
            c.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            c.HasOne(x => x.ApprovedByUser)
                .WithMany()
                .HasForeignKey(x => x.ApprovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // StockCountItem
        modelBuilder.Entity<Models.Inventory.StockCountItem>(i =>
        {
            i.ToTable("StockCountItems");
            i.HasKey(x => x.Id);
            i.Property(x => x.Notes).HasMaxLength(500);
            
            i.HasIndex(x => new { x.StockCountId, x.ProductId }).IsUnique();
            
            i.HasOne(x => x.StockCount)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.StockCountId)
                .OnDelete(DeleteBehavior.Cascade);
                
            i.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureSalesModels(ModelBuilder modelBuilder)
    {
        // Customer
        modelBuilder.Entity<Models.Sales.Customer>(c =>
        {
            c.ToTable("Customers");
            c.HasKey(x => x.Id);
            c.Property(x => x.Document).HasMaxLength(14).IsRequired();
            c.Property(x => x.Name).HasMaxLength(200).IsRequired();
            c.Property(x => x.Email).HasMaxLength(200);
            c.Property(x => x.Phone).HasMaxLength(20);
            c.Property(x => x.Mobile).HasMaxLength(20);
            c.Property(x => x.ZipCode).HasMaxLength(10);
            c.Property(x => x.Address).HasMaxLength(200);
            c.Property(x => x.Number).HasMaxLength(10);
            c.Property(x => x.Complement).HasMaxLength(100);
            c.Property(x => x.Neighborhood).HasMaxLength(100);
            c.Property(x => x.City).HasMaxLength(100);
            c.Property(x => x.State).HasMaxLength(2);
            
            c.HasIndex(x => x.Document).IsUnique();
            c.HasIndex(x => x.Name);
            c.HasIndex(x => x.Email);
        });

        // Sale
        modelBuilder.Entity<Models.Sales.Sale>(s =>
        {
            s.ToTable("Sales");
            s.HasKey(x => x.Id);
            s.Property(x => x.SaleNumber).HasMaxLength(20).IsRequired();
            s.Property(x => x.Status).HasMaxLength(20).IsRequired();
            s.Property(x => x.PaymentMethod).HasMaxLength(50);
            
            s.HasIndex(x => x.SaleNumber).IsUnique();
            s.HasIndex(x => x.CustomerId);
            s.HasIndex(x => x.UserId);
            s.HasIndex(x => x.SaleDate);
            s.HasIndex(x => x.Status);
            
            s.HasOne(x => x.Customer)
                .WithMany(x => x.Sales)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
                
            s.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // SaleItem
        modelBuilder.Entity<Models.Sales.SaleItem>(i =>
        {
            i.ToTable("SaleItems");
            i.HasKey(x => x.Id);
            
            i.HasIndex(x => new { x.SaleId, x.ProductId });
            
            i.HasOne(x => x.Sale)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Cascade);
                
            i.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
    
    private void ConfigureHRModels(ModelBuilder modelBuilder)
    {
        // Department
        modelBuilder.Entity<Department>(d =>
        {
            d.ToTable("Departments");
            d.HasKey(x => x.Id);
            d.Property(x => x.Name).HasMaxLength(100).IsRequired();
            d.Property(x => x.Description).HasMaxLength(500);
            d.Property(x => x.Code).HasMaxLength(10);
            d.Property(x => x.CostCenter).HasMaxLength(50);
            
            d.HasIndex(x => x.Code).IsUnique();
            d.HasIndex(x => x.ParentDepartmentId);
            d.HasIndex(x => x.ManagerId);
            
            // Relacionamento hierárquico
            d.HasOne(x => x.ParentDepartment)
                .WithMany(x => x.SubDepartments)
                .HasForeignKey(x => x.ParentDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Gerente do departamento
            d.HasOne(x => x.Manager)
                .WithMany()
                .HasForeignKey(x => x.ManagerId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        
        // Position
        modelBuilder.Entity<Position>(p =>
        {
            p.ToTable("Positions");
            p.HasKey(x => x.Id);
            p.Property(x => x.Title).HasMaxLength(100).IsRequired();
            p.Property(x => x.Description).HasMaxLength(500);
            p.Property(x => x.Code).HasMaxLength(10);
            
            p.HasIndex(x => x.Code).IsUnique();
            p.HasIndex(x => x.DefaultDepartmentId);
            
            p.HasOne(x => x.DefaultDepartment)
                .WithMany()
                .HasForeignKey(x => x.DefaultDepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        
        // ApplicationUser - Configuração adicional para relacionamentos de RH
        modelBuilder.Entity<ApplicationUser>(u =>
        {
            u.HasIndex(x => x.Cpf).IsUnique().HasFilter("\"Cpf\" IS NOT NULL");
            u.HasIndex(x => x.Rg).HasFilter("\"Rg\" IS NOT NULL");
            u.HasIndex(x => x.FullName);
            u.HasIndex(x => x.DepartmentId);
            u.HasIndex(x => x.PositionId);
            u.HasIndex(x => x.HireDate);
            u.HasIndex(x => x.EmploymentStatus);
            
            u.HasOne(x => x.Department)
                .WithMany(x => x.Employees)
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
                
            u.HasOne(x => x.Position)
                .WithMany(x => x.Employees)
                .HasForeignKey(x => x.PositionId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
    
    private void ConfigureAuditModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(a =>
        {
            a.ToTable("AuditLogs");
            a.HasKey(x => x.Id);
            
            a.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
            a.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
            a.Property(x => x.Action).HasMaxLength(20).IsRequired();
            a.Property(x => x.UserName).HasMaxLength(200);
            a.Property(x => x.IpAddress).HasMaxLength(45);
            a.Property(x => x.UserAgent).HasMaxLength(500);
            a.Property(x => x.AdditionalInfo).HasMaxLength(1000);
            a.Property(x => x.Timestamp).IsRequired();
            
            // Configura colunas JSON para PostgreSQL
            a.Property(x => x.OldValues).HasColumnType("jsonb");
            a.Property(x => x.NewValues).HasColumnType("jsonb");
            a.Property(x => x.ChangedProperties).HasColumnType("jsonb");
            
            // Índices para otimizar consultas
            a.HasIndex(x => x.EntityName);
            a.HasIndex(x => new { x.EntityName, x.EntityId });
            a.HasIndex(x => x.UserId);
            a.HasIndex(x => x.Timestamp);
            a.HasIndex(x => x.Action);
        });
    }
}
