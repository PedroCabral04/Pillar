using erp.Models;
using erp.Models.Identity;
using erp.Models.Audit;
using erp.Models.TimeTracking;
using erp.Models.Financial;
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
    
    // Financial
    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<AccountReceivable> AccountsReceivable { get; set; } = null!;
    public DbSet<AccountPayable> AccountsPayable { get; set; } = null!;
    public DbSet<FinancialCategory> FinancialCategories { get; set; } = null!;
    public DbSet<CostCenter> CostCenters { get; set; } = null!;
    
    // HR Management
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<Position> Positions { get; set; } = null!;
    
    // Audit
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<PayrollPeriod> PayrollPeriods { get; set; } = null!;
    public DbSet<PayrollEntry> PayrollEntries { get; set; } = null!;
    
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

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        SetAuditProperties();
        
        // Captura snapshots (apenas dados serializados, não referências)
        var auditSnapshots = CaptureAuditSnapshots();
        
        // Salva as mudanças E os logs de auditoria em uma única transação
        if (auditSnapshots.Any())
        {
            // Adiciona os logs ao contexto ANTES de salvar
            foreach (var snapshot in auditSnapshots)
            {
                AuditLogs.Add(snapshot.ToAuditLog());
            }
        }
        
        // Salva tudo de uma vez (transacional)
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        
        // Atualiza EntityId nos logs criados (apenas se necessário)
        if (auditSnapshots.Any(s => s.NeedsIdUpdate))
        {
            foreach (var snapshot in auditSnapshots.Where(s => s.NeedsIdUpdate))
            {
                snapshot.UpdateEntityId();
            }
            await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        
        return result;
    }

    public override int SaveChanges()
    {
        SetAuditProperties();
        
        var auditSnapshots = CaptureAuditSnapshots();
        
        if (auditSnapshots.Any())
        {
            foreach (var snapshot in auditSnapshots)
            {
                AuditLogs.Add(snapshot.ToAuditLog());
            }
        }
        
        var result = base.SaveChanges();
        
        if (auditSnapshots.Any(s => s.NeedsIdUpdate))
        {
            foreach (var snapshot in auditSnapshots.Where(s => s.NeedsIdUpdate))
            {
                snapshot.UpdateEntityId();
            }
            base.SaveChanges();
        }
        
        return result;
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
    /// Captura snapshots de auditoria (apenas dados serializados, não referências)
    /// </summary>
    private List<AuditSnapshot> CaptureAuditSnapshots()
    {
        var snapshots = new List<AuditSnapshot>();
        
        var auditableEntries = ChangeTracker.Entries()
            .Where(e => (e.Entity is IAuditable || e.Entity is ApplicationUser) && 
                       e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (!auditableEntries.Any())
            return snapshots;

        var userId = GetCurrentUserId();
        var userName = GetCurrentUserName();
        var ipAddress = GetCurrentIpAddress();
        var userAgent = GetCurrentUserAgent();
        var timestamp = DateTime.UtcNow;

        foreach (var entry in auditableEntries)
        {
            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Create,
                EntityState.Modified => AuditAction.Update,
                EntityState.Deleted => AuditAction.Delete,
                _ => (AuditAction?)null
            };

            if (!action.HasValue)
                continue;

            var snapshot = new AuditSnapshot
            {
                EntityName = entry.Entity.GetType().Name,
                Action = action.Value,
                UserId = userId,
                UserName = userName,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Timestamp = timestamp,
                NeedsIdUpdate = entry.State == EntityState.Added
            };

            // Captura EntityId (pode ser temporário para Added)
            var keyProperties = entry.Properties.Where(p => p.Metadata.IsPrimaryKey()).ToList();
            if (keyProperties.Any())
            {
                var keyValue = keyProperties[0].CurrentValue;
                snapshot.EntityId = keyValue?.ToString();
                
                // Para entidades novas, guardamos referência para atualizar depois
                if (snapshot.NeedsIdUpdate)
                {
                    snapshot.EntityReference = entry.Entity;
                    snapshot.KeyPropertyName = keyProperties[0].Metadata.Name;
                }
            }

            // Serializa valores conforme a operação
            if (entry.State == EntityState.Modified)
            {
                snapshot.OldValues = SerializeOriginalValues(entry);
                snapshot.NewValues = SerializeCurrentValues(entry);
                snapshot.ChangedProperties = SerializeChangedProperties(entry);
            }
            else if (entry.State == EntityState.Deleted)
            {
                snapshot.OldValues = SerializeOriginalValues(entry);
            }
            else if (entry.State == EntityState.Added)
            {
                snapshot.NewValues = SerializeCurrentValues(entry);
            }

            snapshots.Add(snapshot);
        }
        
        return snapshots;
    }
    
    /// <summary>
    /// Classe auxiliar para snapshot de auditoria (sem manter EntityEntry)
    /// </summary>
    private class AuditSnapshot
    {
        public string EntityName { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public AuditAction Action { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime Timestamp { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? ChangedProperties { get; set; }
        
        // Para atualizar ID após inserção
        public bool NeedsIdUpdate { get; set; }
        public object? EntityReference { get; set; }
        public string? KeyPropertyName { get; set; }
        
        private AuditLog? _auditLog;

        public AuditLog ToAuditLog()
        {
            _auditLog = new AuditLog
            {
                EntityName = EntityName,
                EntityId = EntityId ?? "0",
                Action = Action,
                UserId = UserId,
                UserName = UserName,
                IpAddress = IpAddress,
                UserAgent = UserAgent,
                Timestamp = Timestamp,
                OldValues = OldValues,
                NewValues = NewValues,
                ChangedProperties = ChangedProperties
            };
            return _auditLog;
        }

        public void UpdateEntityId()
        {
            if (_auditLog != null && EntityReference != null && !string.IsNullOrEmpty(KeyPropertyName))
            {
                var entityType = EntityReference.GetType();
                var keyProperty = entityType.GetProperty(KeyPropertyName);
                if (keyProperty != null)
                {
                    var newId = keyProperty.GetValue(EntityReference);
                    _auditLog.EntityId = newId?.ToString() ?? "0";
                }
            }
        }
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
        
        // Financial model configuration
        ConfigureFinancialModels(modelBuilder);
        
        // HR Management model configuration
        ConfigureHRModels(modelBuilder);
        
        // Audit model configuration
        ConfigureAuditModels(modelBuilder);

        // Time tracking / payroll configuration
        ConfigureTimeTrackingModels(modelBuilder);
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
    
    private void ConfigureFinancialModels(ModelBuilder modelBuilder)
    {
        // Supplier
        modelBuilder.Entity<Supplier>(s =>
        {
            s.ToTable("Suppliers");
            s.HasKey(x => x.Id);
            s.Property(x => x.Name).HasMaxLength(200).IsRequired();
            s.Property(x => x.TradeName).HasMaxLength(200);
            s.Property(x => x.TaxId).HasMaxLength(14).IsRequired();
            s.Property(x => x.StateRegistration).HasMaxLength(20);
            s.Property(x => x.MunicipalRegistration).HasMaxLength(20);
            s.Property(x => x.ZipCode).HasMaxLength(10);
            s.Property(x => x.Street).HasMaxLength(200);
            s.Property(x => x.Number).HasMaxLength(10);
            s.Property(x => x.Complement).HasMaxLength(100);
            s.Property(x => x.District).HasMaxLength(100);
            s.Property(x => x.City).HasMaxLength(100);
            s.Property(x => x.State).HasMaxLength(2);
            s.Property(x => x.Country).HasMaxLength(100);
            s.Property(x => x.Phone).HasMaxLength(20);
            s.Property(x => x.MobilePhone).HasMaxLength(20);
            s.Property(x => x.Email).HasMaxLength(200);
            s.Property(x => x.Website).HasMaxLength(200);
            s.Property(x => x.Category).HasMaxLength(100);
            s.Property(x => x.PaymentMethod).HasMaxLength(50);
            
            s.HasIndex(x => x.TaxId).IsUnique();
            s.HasIndex(x => x.Name);
            s.HasIndex(x => x.Email);
            s.HasIndex(x => x.IsActive);
            
            s.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // AccountReceivable
        modelBuilder.Entity<AccountReceivable>(ar =>
        {
            ar.ToTable("AccountsReceivable");
            ar.HasKey(x => x.Id);
            ar.Property(x => x.InvoiceNumber).HasMaxLength(50);
            ar.Property(x => x.BankSlipNumber).HasMaxLength(100);
            ar.Property(x => x.PixKey).HasMaxLength(100);
            ar.Property(x => x.OriginalAmount).HasPrecision(18, 2);
            ar.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            ar.Property(x => x.InterestAmount).HasPrecision(18, 2);
            ar.Property(x => x.FineAmount).HasPrecision(18, 2);
            ar.Property(x => x.PaidAmount).HasPrecision(18, 2);
            
            ar.HasIndex(x => x.CustomerId);
            ar.HasIndex(x => x.DueDate);
            ar.HasIndex(x => x.Status);
            ar.HasIndex(x => x.CategoryId);
            ar.HasIndex(x => x.CostCenterId);
            ar.HasIndex(x => new { x.Status, x.DueDate });
            
            ar.HasOne(x => x.Customer)
                .WithMany(x => x.AccountsReceivable)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
                
            ar.HasOne(x => x.Category)
                .WithMany(x => x.AccountsReceivable)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
                
            ar.HasOne(x => x.CostCenter)
                .WithMany(x => x.AccountsReceivable)
                .HasForeignKey(x => x.CostCenterId)
                .OnDelete(DeleteBehavior.SetNull);
                
            ar.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            ar.HasOne(x => x.ReceivedByUser)
                .WithMany()
                .HasForeignKey(x => x.ReceivedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
                
            ar.HasOne(x => x.ParentAccount)
                .WithMany(x => x.Installments)
                .HasForeignKey(x => x.ParentAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // AccountPayable
        modelBuilder.Entity<AccountPayable>(ap =>
        {
            ap.ToTable("AccountsPayable");
            ap.HasKey(x => x.Id);
            ap.Property(x => x.InvoiceNumber).HasMaxLength(50);
            ap.Property(x => x.BankSlipNumber).HasMaxLength(100);
            ap.Property(x => x.PixKey).HasMaxLength(100);
            ap.Property(x => x.OriginalAmount).HasPrecision(18, 2);
            ap.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            ap.Property(x => x.InterestAmount).HasPrecision(18, 2);
            ap.Property(x => x.FineAmount).HasPrecision(18, 2);
            ap.Property(x => x.PaidAmount).HasPrecision(18, 2);
            ap.Property(x => x.InvoiceAttachmentUrl).HasMaxLength(500);
            ap.Property(x => x.ProofOfPaymentUrl).HasMaxLength(500);
            
            ap.HasIndex(x => x.SupplierId);
            ap.HasIndex(x => x.DueDate);
            ap.HasIndex(x => x.Status);
            ap.HasIndex(x => x.CategoryId);
            ap.HasIndex(x => x.CostCenterId);
            ap.HasIndex(x => x.RequiresApproval);
            ap.HasIndex(x => new { x.Status, x.DueDate });
            ap.HasIndex(x => new { x.RequiresApproval, x.ApprovalDate });
            
            ap.HasOne(x => x.Supplier)
                .WithMany(x => x.AccountsPayable)
                .HasForeignKey(x => x.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
                
            ap.HasOne(x => x.Category)
                .WithMany(x => x.AccountsPayable)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
                
            ap.HasOne(x => x.CostCenter)
                .WithMany(x => x.AccountsPayable)
                .HasForeignKey(x => x.CostCenterId)
                .OnDelete(DeleteBehavior.SetNull);
                
            ap.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            ap.HasOne(x => x.PaidByUser)
                .WithMany()
                .HasForeignKey(x => x.PaidByUserId)
                .OnDelete(DeleteBehavior.SetNull);
                
            ap.HasOne(x => x.ApprovedByUser)
                .WithMany()
                .HasForeignKey(x => x.ApprovedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
                
            ap.HasOne(x => x.ParentAccount)
                .WithMany(x => x.Installments)
                .HasForeignKey(x => x.ParentAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // FinancialCategory
        modelBuilder.Entity<FinancialCategory>(fc =>
        {
            fc.ToTable("FinancialCategories");
            fc.HasKey(x => x.Id);
            fc.Property(x => x.Name).HasMaxLength(200).IsRequired();
            fc.Property(x => x.Code).HasMaxLength(20).IsRequired();
            fc.Property(x => x.Description).HasMaxLength(500);
            
            fc.HasIndex(x => x.Code).IsUnique();
            fc.HasIndex(x => x.Type);
            fc.HasIndex(x => x.ParentCategoryId);
            
            fc.HasOne(x => x.ParentCategory)
                .WithMany(x => x.SubCategories)
                .HasForeignKey(x => x.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // CostCenter
        modelBuilder.Entity<CostCenter>(cc =>
        {
            cc.ToTable("CostCenters");
            cc.HasKey(x => x.Id);
            cc.Property(x => x.Name).HasMaxLength(200).IsRequired();
            cc.Property(x => x.Code).HasMaxLength(20).IsRequired();
            cc.Property(x => x.Description).HasMaxLength(500);
            cc.Property(x => x.MonthlyBudget).HasPrecision(18, 2);
            
            cc.HasIndex(x => x.Code).IsUnique();
            cc.HasIndex(x => x.ManagerUserId);
            cc.HasIndex(x => x.IsActive);
            
            cc.HasOne(x => x.Manager)
                .WithMany()
                .HasForeignKey(x => x.ManagerUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        
        // Update Customer with new fields
        modelBuilder.Entity<Models.Sales.Customer>(c =>
        {
            c.Property(x => x.TradeName).HasMaxLength(200);
            c.Property(x => x.StateRegistration).HasMaxLength(20);
            c.Property(x => x.MunicipalRegistration).HasMaxLength(20);
            c.Property(x => x.Country).HasMaxLength(100);
            c.Property(x => x.Website).HasMaxLength(200);
            c.Property(x => x.CreditLimit).HasPrecision(18, 2);
            c.Property(x => x.PaymentMethod).HasMaxLength(50);
            
            c.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
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
            
            // Índices compostos otimizados para queries comuns
            // Índice para histórico de entidade (timeline de uma entidade específica)
            a.HasIndex(x => new { x.EntityName, x.EntityId, x.Timestamp })
                .HasDatabaseName("idx_audit_entity_timeline");
            
            // Índice para atividade de usuário (timeline de um usuário específico)
            a.HasIndex(x => new { x.UserId, x.Timestamp })
                .HasDatabaseName("idx_audit_user_timeline");
            
            // Índice para busca por tipo de ação em período
            a.HasIndex(x => new { x.Action, x.Timestamp })
                .HasDatabaseName("idx_audit_action_timeline");
            
            // Índice para busca geral por entidade e tipo de ação
            a.HasIndex(x => new { x.EntityName, x.Action, x.Timestamp })
                .HasDatabaseName("idx_audit_entity_action_timeline");
        });
    }

    private void ConfigureTimeTrackingModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PayrollPeriod>(p =>
        {
            p.ToTable("PayrollPeriods");
            p.HasKey(x => x.Id);
            p.Property(x => x.ReferenceMonth).IsRequired();
            p.Property(x => x.ReferenceYear).IsRequired();
            p.Property(x => x.Status)
                .HasConversion<int>();
            p.Property(x => x.CreatedAt).IsRequired();
            p.Property(x => x.UpdatedAt);

            p.HasIndex(x => new { x.ReferenceYear, x.ReferenceMonth }).IsUnique();

            p.HasOne(x => x.CreatedBy)
                .WithMany()
                .HasForeignKey(x => x.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            p.HasOne(x => x.UpdatedBy)
                .WithMany()
                .HasForeignKey(x => x.UpdatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PayrollEntry>(e =>
        {
            e.ToTable("PayrollEntries");
            e.HasKey(x => x.Id);
            e.Property(x => x.Faltas).HasColumnType("decimal(5,2)");
            e.Property(x => x.Abonos).HasColumnType("decimal(5,2)");
            e.Property(x => x.HorasExtras).HasColumnType("decimal(5,2)");
            e.Property(x => x.Atrasos).HasColumnType("decimal(5,2)");
            e.Property(x => x.Observacoes).HasMaxLength(1000);
            e.Property(x => x.CreatedAt).IsRequired();

            e.HasIndex(x => new { x.PayrollPeriodId, x.EmployeeId }).IsUnique();

            e.HasOne(x => x.PayrollPeriod)
                .WithMany(x => x.Entries)
                .HasForeignKey(x => x.PayrollPeriodId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.UpdatedBy)
                .WithMany()
                .HasForeignKey(x => x.UpdatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
