using erp.Models;
using erp.Models.Identity;
using erp.Models.Audit;
using erp.Models.Chatbot;
using erp.Models.TimeTracking;
using erp.Models.Financial;
using erp.Models.Payroll;
using erp.Models.Tenancy;
using erp.Models.Dashboard;
using erp.Services.Tenancy;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Reflection;
using System.Text.Json;

namespace erp.Data;

/// <summary>
/// Main Entity Framework database context for the Pillar ERP application.
/// </summary>
/// <remarks>
/// DbSet properties are initialized by EF Core infrastructure, hence the null-forgiving operator (!).
/// The null! suppression is correct here - EF Core guarantees these properties will never be null at runtime.
/// </remarks>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    // IdentityDbContext already provides Users, Roles, and UserRole management.
    // Legacy models are being removed to ensure consistency.

    // Kanban
    public DbSet<Models.Kanban.KanbanBoard> KanbanBoards { get; set; } = null!;
    public DbSet<Models.Kanban.KanbanColumn> KanbanColumns { get; set; } = null!;
    public DbSet<Models.Kanban.KanbanCard> KanbanCards { get; set; } = null!;
    public DbSet<Models.Kanban.KanbanLabel> KanbanLabels { get; set; } = null!;
    public DbSet<Models.Kanban.KanbanCardLabel> KanbanCardLabels { get; set; } = null!;
    public DbSet<Models.Kanban.KanbanComment> KanbanComments { get; set; } = null!;
    public DbSet<Models.Kanban.KanbanCardHistory> KanbanCardHistories { get; set; } = null!;

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

    // Service Orders
    public DbSet<Models.ServiceOrders.ServiceOrder> ServiceOrders { get; set; } = null!;
    public DbSet<Models.ServiceOrders.ServiceOrderItem> ServiceOrderItems { get; set; } = null!;

    // Financial
    public DbSet<Supplier> Suppliers { get; set; } = null!;
    public DbSet<AccountReceivable> AccountsReceivable { get; set; } = null!;
    public DbSet<AccountPayable> AccountsPayable { get; set; } = null!;
    public DbSet<FinancialCategory> FinancialCategories { get; set; } = null!;
    public DbSet<CostCenter> CostCenters { get; set; } = null!;
    public DbSet<Commission> Commissions { get; set; } = null!;
    
    // HR Management
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<Position> Positions { get; set; } = null!;
    
    // Asset Management
    public DbSet<AssetCategory> AssetCategories { get; set; } = null!;
    public DbSet<Asset> Assets { get; set; } = null!;
    public DbSet<AssetAssignment> AssetAssignments { get; set; } = null!;
    public DbSet<AssetMaintenance> AssetMaintenances { get; set; } = null!;
    public DbSet<AssetDocument> AssetDocuments { get; set; } = null!;
    public DbSet<AssetTransfer> AssetTransfers { get; set; } = null!;
    public DbSet<Tenant> Tenants { get; set; } = null!;
    public DbSet<TenantBranding> TenantBrandings { get; set; } = null!;
    public DbSet<TenantMembership> TenantMemberships { get; set; } = null!;
    
    // Audit
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<AuditLogArchive> AuditLogArchives { get; set; } = null!;
    public DbSet<PayrollPeriod> PayrollPeriods { get; set; } = null!;
    public DbSet<PayrollEntry> PayrollEntries { get; set; } = null!;
    public DbSet<PayrollResult> PayrollResults { get; set; } = null!;
    public DbSet<PayrollComponent> PayrollComponents { get; set; } = null!;
    public DbSet<PayrollSlip> PayrollSlips { get; set; } = null!;
    public DbSet<PayrollTaxBracket> PayrollTaxBrackets { get; set; } = null!;
    
    // Onboarding
    public DbSet<erp.Models.Onboarding.UserOnboardingProgress> UserOnboardingProgress { get; set; } = null!;
    
    // Dashboard
    public DbSet<UserDashboardLayout> UserDashboardLayouts { get; set; } = null!;
    public DbSet<WidgetRoleConfiguration> WidgetRoleConfigurations { get; set; } = null!;
    
    // Chatbot Conversations
    public DbSet<ChatConversation> ChatConversations { get; set; } = null!;
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
    
    // Module Permissions
    public DbSet<ModulePermission> ModulePermissions { get; set; } = null!;
    public DbSet<RoleModulePermission> RoleModulePermissions { get; set; } = null!;
    public DbSet<ModuleActionPermission> ModuleActionPermissions { get; set; } = null!;
    public DbSet<RoleModuleActionPermission> RoleModuleActionPermissions { get; set; } = null!;
    
    // Serviços injetados para auditoria
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly erp.Services.Tenancy.ITenantContextAccessor? _tenantContextAccessor;

    /// <summary>
    /// Gets the current tenant ID from the tenant context accessor.
    /// Returns null if no tenant context is available (used in query filters).
    /// </summary>
    private int? CurrentTenantId => _tenantContextAccessor?.Current?.TenantId;

    /// <summary>
    /// Determines if tenant filtering should be applied.
    /// Returns false when no tenant context exists (allowing all data access).
    /// </summary>
    private bool ShouldApplyTenantFilter => _tenantContextAccessor != null 
        && _tenantContextAccessor.Current != null 
        && _tenantContextAccessor.Current.TenantId.HasValue;
    
    // Construtor adicional para injeção de IHttpContextAccessor
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IHttpContextAccessor? httpContextAccessor = null,
        erp.Services.Tenancy.ITenantContextAccessor? tenantContextAccessor = null) 
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
        _tenantContextAccessor = tenantContextAccessor;
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>()
            .HaveConversion<DateTimeToUtcConverter>();

        configurationBuilder.Properties<DateTime?>()
            .HaveConversion<NullableDateTimeToUtcConverter>();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // Only configure here if no provider was configured via DI (AddDbContext)
        if (!options.IsConfigured)
        {
            throw new InvalidOperationException(
                "Database connection string not configured. " +
                "Please set 'DbContextSettings:ConnectionString' in appsettings.json " +
                "or use the 'ConnectionStrings__DefaultConnection' environment variable.");
        }
    }

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        SetAuditProperties();
        SetTenantId();
        
        // Força detecção de mudanças antes de capturar snapshots
        ChangeTracker.DetectChanges();
        
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
        SetTenantId();
        
        // Força detecção de mudanças antes de capturar snapshots
        ChangeTracker.DetectChanges();
        
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

    private void SetTenantId()
    {
        var currentTenantId = _tenantContextAccessor?.Current?.TenantId;
        
        // 1. Fallback: Try to resolve from HttpContext.Items (populated by middleware)
        if (!currentTenantId.HasValue || currentTenantId.Value == 0)
        {
            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext != null && httpContext.Items.TryGetValue(erp.Services.Tenancy.TenantResolutionMiddleware.TenantItemKey, out var tenantObj))
            {
                if (tenantObj is Tenant tenant)
                {
                    currentTenantId = tenant.Id;
                }
            }
        }
        
        // 2. Fallback: Try to resolve from User Claims directly
        if (!currentTenantId.HasValue || currentTenantId.Value == 0)
        {
            var user = _httpContextAccessor?.HttpContext?.User;
            var tenantIdClaim = user?.FindFirst(erp.Services.Tenancy.TenantClaimTypes.TenantId)?.Value;
            if (int.TryParse(tenantIdClaim, out var tid))
            {
                currentTenantId = tid;
            }
        }

        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IMustHaveTenant && e.State == EntityState.Added)
            .ToList();

        if (!entries.Any()) return;

        foreach (var entry in entries)
        {
            var entity = (IMustHaveTenant)entry.Entity;
            
            // If TenantId is already explicitly set, validate it's not 0
            if (entity.TenantId != 0)
            {
                continue; // Already has a valid tenant, skip
            }
            
            // Try to inherit TenantId from parent entity (for child entities like SaleItem, KanbanColumn, etc.)
            var inheritedTenantId = TryGetParentTenantId(entry);
            if (inheritedTenantId.HasValue && inheritedTenantId.HasValue && inheritedTenantId.Value != 0)
            {
                entity.TenantId = inheritedTenantId.Value;
                continue;
            }
            
            // If no tenant context and TenantId is 0, this is a data integrity issue
            if (!currentTenantId.HasValue || currentTenantId.Value == 0)
            {
                var entityType = entry.Entity.GetType().Name;
                var xTenant = _httpContextAccessor?.HttpContext?.Request?.Headers["X-Tenant"].ToString() ?? "N/A";
                var userIdentity = _httpContextAccessor?.HttpContext?.User?.Identity?.Name ?? "Anonymous";

                throw new InvalidOperationException(
                    $"Cannot save entity '{entityType}' without tenant context. " +
                    $"Debug: X-Tenant={xTenant}, User={userIdentity}. " +
                    $"Entities implementing IMustHaveTenant require a valid TenantId.");
            }
            
            entity.TenantId = currentTenantId.Value;
        }
    }

    /// <summary>
    /// Tries to get TenantId from a parent entity via navigation properties.
    /// This ensures child entities inherit the same TenantId as their parent.
    /// </summary>
    private int? TryGetParentTenantId(EntityEntry entry)
    {
        // Check all navigation properties for a parent that implements IMustHaveTenant
        foreach (var navigation in entry.Navigations)
        {
            // Skip collection navigations - we only want single references (parent)
            if (navigation.Metadata.IsCollection)
                continue;
            
            var navigationValue = navigation.CurrentValue;
            if (navigationValue is IMustHaveTenant parentTenant && parentTenant.TenantId != 0)
            {
                return parentTenant.TenantId;
            }
        }
        
        return null;
    }

    private void SetAuditProperties()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is ApplicationUser &&
                        e.State is EntityState.Added or EntityState.Modified);

        foreach (var entityEntry in entries )
        {
            var userEntity = (ApplicationUser)entityEntry.Entity;
            if (entityEntry.State == EntityState.Modified)
            {
                userEntity.UpdatedAt = DateTime.UtcNow;
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
        var currentTenantId = _tenantContextAccessor?.Current?.TenantId ?? 0;

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

            // Obtém TenantId da entidade se ela implementar IMustHaveTenant
            var entityTenantId = currentTenantId;
            if (entry.Entity is IMustHaveTenant tenantEntity)
            {
                entityTenantId = tenantEntity.TenantId > 0 ? tenantEntity.TenantId : currentTenantId;
            }

            var snapshot = new AuditSnapshot
            {
                EntityName = entry.Entity.GetType().Name,
                Action = action.Value,
                UserId = userId,
                UserName = userName,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                Timestamp = timestamp,
                NeedsIdUpdate = entry.State == EntityState.Added,
                TenantId = entityTenantId
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

            // Captura descrição legível da entidade
            snapshot.EntityDescription = GetEntityDescription(entry.Entity);

            // Serializa valores conforme a operação
            if (entry.State == EntityState.Modified)
            {
                snapshot.OldValues = SerializeOriginalValues(entry);
                snapshot.NewValues = SerializeCurrentValues(entry);
                snapshot.ChangedProperties = SerializeChangedProperties(entry);
                snapshot.References = SerializeReferences(entry, useCurrentValues: true);
            }
            else if (entry.State == EntityState.Deleted)
            {
                snapshot.OldValues = SerializeOriginalValues(entry);
                snapshot.References = SerializeReferences(entry, useCurrentValues: false);
            }
            else if (entry.State == EntityState.Added)
            {
                snapshot.NewValues = SerializeCurrentValues(entry);
                snapshot.References = SerializeReferences(entry, useCurrentValues: true);
            }

            snapshots.Add(snapshot);
        }
        
        return snapshots;
    }
    
    /// <summary>
    /// Obtém uma descrição legível da entidade (nome, título, etc)
    /// </summary>
    private string? GetEntityDescription(object entity)
    {
        var entityType = entity.GetType();
        
        // Lista de propriedades comuns que representam a descrição da entidade
        var descriptionPropertyNames = new[] { "Name", "FullName", "Title", "Description", "Sku", "SaleNumber", "InvoiceNumber", "AssetCode" };
        
        foreach (var propName in descriptionPropertyNames)
        {
            var property = entityType.GetProperty(propName);
            if (property != null && property.PropertyType == typeof(string))
            {
                var value = property.GetValue(entity) as string;
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Serializa referências legíveis para FKs
    /// </summary>
    private string? SerializeReferences(EntityEntry entry, bool useCurrentValues)
    {
        var references = new Dictionary<string, string?>();
        var entityType = entry.Entity.GetType();
        
        // Mapeia propriedades FK para suas entidades de navegação
        foreach (var navigation in entry.Navigations)
        {
            if (navigation.Metadata.IsCollection)
                continue;
                
            var navigationValue = navigation.CurrentValue;
            if (navigationValue == null)
                continue;
            
            var navType = navigationValue.GetType();
            var fkPropertyName = navigation.Metadata.Name + "Id";
            
            // Tenta obter descrição legível da entidade referenciada
            var description = GetEntityDescription(navigationValue);
            if (!string.IsNullOrEmpty(description))
            {
                references[navigation.Metadata.Name] = description;
            }
        }
        
        // Tenta também obter referências via propriedades FK sem navegação carregada
        foreach (var property in entry.Properties.Where(p => p.Metadata.IsForeignKey()))
        {
            var fkName = property.Metadata.Name;
            var fkValue = useCurrentValues ? property.CurrentValue : property.OriginalValue;
            
            if (fkValue != null && !references.ContainsKey(fkName.Replace("Id", "")))
            {
                // Armazena o ID como fallback se não tiver navegação
                var navName = fkName.EndsWith("Id") ? fkName[..^2] : fkName;
                if (!references.ContainsKey(navName))
                {
                    // Tenta buscar a descrição da entidade referenciada no contexto
                    var refDescription = TryGetReferenceDescription(fkName, fkValue);
                    if (refDescription != null)
                    {
                        references[navName] = refDescription;
                    }
                }
            }
        }
        
        return references.Any() ? JsonSerializer.Serialize(references) : null;
    }
    
    /// <summary>
    /// Tenta obter descrição da entidade referenciada pelo FK
    /// </summary>
    private string? TryGetReferenceDescription(string fkPropertyName, object? fkValue)
    {
        if (fkValue == null) return null;
        
        try
        {
            // Mapeamento de FKs conhecidas para suas entidades
            return fkPropertyName switch
            {
                "SupplierId" => Suppliers.Local.FirstOrDefault(s => s.Id == (int)fkValue)?.Name 
                    ?? Suppliers.Find((int)fkValue)?.Name,
                "CustomerId" => Customers.Local.FirstOrDefault(c => c.Id == (int)fkValue)?.Name 
                    ?? Customers.Find((int)fkValue)?.Name,
                "CategoryId" => FinancialCategories.Local.FirstOrDefault(c => c.Id == (int)fkValue)?.Name 
                    ?? FinancialCategories.Find((int)fkValue)?.Name,
                "CostCenterId" => CostCenters.Local.FirstOrDefault(c => c.Id == (int)fkValue)?.Name 
                    ?? CostCenters.Find((int)fkValue)?.Name,
                "DepartmentId" => Departments.Local.FirstOrDefault(d => d.Id == (int)fkValue)?.Name 
                    ?? Departments.Find((int)fkValue)?.Name,
                "PositionId" => Positions.Local.FirstOrDefault(p => p.Id == (int)fkValue)?.Title 
                    ?? Positions.Find((int)fkValue)?.Title,
                "WarehouseId" => Warehouses.Local.FirstOrDefault(w => w.Id == (int)fkValue)?.Name 
                    ?? Warehouses.Find((int)fkValue)?.Name,
                "TenantId" => Tenants.Local.FirstOrDefault(t => t.Id == (int)fkValue)?.Name 
                    ?? Tenants.Find((int)fkValue)?.Name,
                "ProductId" => Products.Local.FirstOrDefault(p => p.Id == (int)fkValue)?.Name 
                    ?? Products.Find((int)fkValue)?.Name,
                "AssetId" => Assets.Local.FirstOrDefault(a => a.Id == (int)fkValue)?.Name 
                    ?? Assets.Find((int)fkValue)?.Name,
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Verifica se uma propriedade deve ser excluída da auditoria
    /// </summary>
    private bool ShouldExcludeFromAudit(PropertyInfo property)
    {
        return property.GetCustomAttribute<AuditExcludeAttribute>() != null;
    }
    
    /// <summary>
    /// Verifica se uma propriedade EF deve ser excluída da auditoria
    /// </summary>
    private bool ShouldExcludeFromAudit(PropertyEntry property)
    {
        var clrProperty = property.Metadata.PropertyInfo;
        if (clrProperty == null) return false;
        
        return ShouldExcludeFromAudit(clrProperty);
    }
    
    /// <summary>
    /// Classe auxiliar para snapshot de auditoria (sem manter EntityEntry)
    /// </summary>
    private class AuditSnapshot
    {
        public string EntityName { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? EntityDescription { get; set; }
        public AuditAction Action { get; set; }
        public int? UserId { get; set; }
        public string? UserName { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime Timestamp { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? ChangedProperties { get; set; }
        public string? References { get; set; }
        public int TenantId { get; set; }
        
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
                EntityDescription = EntityDescription,
                Action = Action,
                UserId = UserId,
                UserName = UserName,
                IpAddress = IpAddress,
                UserAgent = UserAgent,
                Timestamp = Timestamp,
                OldValues = OldValues,
                NewValues = NewValues,
                ChangedProperties = ChangedProperties,
                References = References,
                TenantId = TenantId
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
        
        foreach (var property in entry.Properties)
        {
            // Ignora chaves primárias
            if (property.Metadata.IsPrimaryKey())
                continue;
            
            // Ignora propriedades marcadas com [AuditExclude]
            if (ShouldExcludeFromAudit(property))
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
        
        foreach (var property in entry.Properties)
        {
            // Ignora chaves primárias
            if (property.Metadata.IsPrimaryKey())
                continue;
                
            // Ignora propriedades marcadas com [AuditExclude]
            if (ShouldExcludeFromAudit(property))
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
        var changedProperties = new List<object>();
        
        foreach (var property in entry.Properties)
        {
            // Ignora chaves primárias e propriedades excluídas
            if (property.Metadata.IsPrimaryKey() || ShouldExcludeFromAudit(property))
                continue;
            
            var originalValue = property.OriginalValue;
            var currentValue = property.CurrentValue;
            
            // Verifica se houve mudança de várias formas
            bool hasChanged = property.IsModified;
            
            // Se não está marcado como modificado, verifica comparando valores
            if (!hasChanged)
            {
                // Ambos null = sem mudança
                if (originalValue == null && currentValue == null)
                    continue;
                    
                // Um null e outro não = mudança
                if (originalValue == null || currentValue == null)
                {
                    hasChanged = true;
                }
                else
                {
                    // Compara usando ToString para evitar problemas com tipos de referência
                    hasChanged = !string.Equals(
                        originalValue.ToString(), 
                        currentValue.ToString(), 
                        StringComparison.Ordinal);
                }
            }
            
            if (hasChanged)
            {
                changedProperties.Add(new
                {
                    PropertyName = property.Metadata.Name,
                    OldValue = originalValue,
                    NewValue = currentValue
                });
            }
        }

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

        // Ensure all DateTime values are UTC when writing to PostgreSQL
        // PostgreSQL 'timestamp with time zone' requires DateTime.Kind == UTC
        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
            v => v.HasValue && v.Value.Kind == DateTimeKind.Local ? v.Value.ToUniversalTime() : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(nullableDateTimeConverter);
                }
            }
        }

        // Application specific configurations follow below.
        
        // Kanban model configuration
        modelBuilder.Entity<Models.Kanban.KanbanBoard>(b =>
        {
            b.ToTable("KanbanBoards");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.HasOne(x => x.Owner)
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
            t.Property(x => x.Color).HasMaxLength(7);
            t.Property(x => x.Priority).HasConversion<int>().HasDefaultValue(Models.Kanban.KanbanPriority.None);
            t.HasOne(x => x.Column)
                .WithMany(x => x.Cards)
                .HasForeignKey(x => x.ColumnId)
                .OnDelete(DeleteBehavior.Cascade);
            t.HasOne(x => x.AssignedUser)
                .WithMany()
                .HasForeignKey(x => x.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);
            t.HasIndex(x => new { x.ColumnId, x.Position });
            t.HasIndex(x => x.AssignedUserId);
            t.HasIndex(x => x.DueDate);
            t.HasIndex(x => x.Priority);
            t.HasIndex(x => x.IsArchived);
        });

        // KanbanLabel
        modelBuilder.Entity<Models.Kanban.KanbanLabel>(l =>
        {
            l.ToTable("KanbanLabels");
            l.HasKey(x => x.Id);
            l.Property(x => x.Name).HasMaxLength(50).IsRequired();
            l.Property(x => x.Color).HasMaxLength(7).IsRequired();
            l.HasOne(x => x.Board)
                .WithMany(x => x.Labels)
                .HasForeignKey(x => x.BoardId)
                .OnDelete(DeleteBehavior.Cascade);
            l.HasIndex(x => new { x.BoardId, x.Name });
        });

        // KanbanCardLabel (many-to-many)
        modelBuilder.Entity<Models.Kanban.KanbanCardLabel>(cl =>
        {
            cl.ToTable("KanbanCardLabels");
            cl.HasKey(x => new { x.CardId, x.LabelId });
            cl.HasOne(x => x.Card)
                .WithMany(x => x.CardLabels)
                .HasForeignKey(x => x.CardId)
                .OnDelete(DeleteBehavior.Cascade);
            cl.HasOne(x => x.Label)
                .WithMany()
                .HasForeignKey(x => x.LabelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // KanbanComment
        modelBuilder.Entity<Models.Kanban.KanbanComment>(c =>
        {
            c.ToTable("KanbanComments");
            c.HasKey(x => x.Id);
            c.Property(x => x.Content).HasMaxLength(4000).IsRequired();
            c.HasOne(x => x.Card)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.CardId)
                .OnDelete(DeleteBehavior.Cascade);
            c.HasOne(x => x.Author)
                .WithMany()
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
            c.HasIndex(x => x.CardId);
            c.HasIndex(x => x.CreatedAt);
        });

        // KanbanCardHistory
        modelBuilder.Entity<Models.Kanban.KanbanCardHistory>(h =>
        {
            h.ToTable("KanbanCardHistories");
            h.HasKey(x => x.Id);
            h.Property(x => x.Action).HasConversion<int>().IsRequired();
            h.Property(x => x.Description).HasMaxLength(500).IsRequired();
            h.Property(x => x.OldValue).HasMaxLength(1000);
            h.Property(x => x.NewValue).HasMaxLength(1000);
            h.HasOne(x => x.Card)
                .WithMany(x => x.History)
                .HasForeignKey(x => x.CardId)
                .OnDelete(DeleteBehavior.Cascade);
            h.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            h.HasIndex(x => new { x.CardId, x.CreatedAt });
        });

        ConfigureTenancyModels(modelBuilder);

        // Inventory model configuration
        ConfigureInventoryModels(modelBuilder);
        
        // Sales model configuration
        ConfigureSalesModels(modelBuilder);

        // Service Orders model configuration
        ConfigureServiceOrderModels(modelBuilder);

        // Financial model configuration
        ConfigureFinancialModels(modelBuilder);
        
        // HR Management model configuration
        ConfigureHRModels(modelBuilder);
        
        // Asset Management model configuration
        ConfigureAssetModels(modelBuilder);
        
        // Audit model configuration
        ConfigureAuditModels(modelBuilder);

        // Onboarding configuration
        modelBuilder.Entity<erp.Models.Onboarding.UserOnboardingProgress>()
            .HasIndex(p => new { p.UserId, p.TourId })
            .IsUnique();

        // Payroll model configuration
        ConfigurePayrollModels(modelBuilder); 

        // Time tracking / payroll configuration
        ConfigureTimeTrackingModels(modelBuilder);
        
        // Dashboard model configuration
        ConfigureDashboardModels(modelBuilder);
        
        // Chatbot model configuration
        ConfigureChatbotModels(modelBuilder);
        
        // Module permissions configuration
        ConfigureModulePermissionModels(modelBuilder);

        // Apply Global Query Filters for Multi-Tenancy
        // These filters ensure queries only return data for the current tenant.
        // When no tenant context exists (tests, admin operations, anonymous endpoints), all data is accessible.
        // The filter pattern: skip filter if no tenant context, otherwise match entity.TenantId to current tenant.
        
        ConfigureTenantQueryFilters(modelBuilder);
    }

    private void ConfigureTenancyModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TenantBranding>(branding =>
        {
            branding.ToTable("TenantBrandings");
            branding.HasKey(x => x.Id);
            branding.Property(x => x.LogoUrl).HasMaxLength(500);
            branding.Property(x => x.FaviconUrl).HasMaxLength(500);
            branding.Property(x => x.PrimaryColor).HasMaxLength(20);
            branding.Property(x => x.SecondaryColor).HasMaxLength(20);
            branding.Property(x => x.AccentColor).HasMaxLength(20);
            branding.Property(x => x.LoginBackgroundUrl).HasMaxLength(500);
            branding.Property(x => x.EmailFooterHtml).HasMaxLength(2000);
            branding.Property(x => x.CustomCss).HasMaxLength(2000);
        });

        modelBuilder.Entity<Tenant>(tenant =>
        {
            tenant.ToTable("Tenants");
            tenant.HasKey(x => x.Id);
            tenant.Property(x => x.Name).HasMaxLength(200).IsRequired();
            tenant.Property(x => x.Slug).HasMaxLength(64).IsRequired();
            tenant.Property(x => x.DocumentNumber).HasMaxLength(20);
            tenant.Property(x => x.PrimaryContactName).HasMaxLength(200);
            tenant.Property(x => x.PrimaryContactEmail).HasMaxLength(200);
            tenant.Property(x => x.PrimaryContactPhone).HasMaxLength(20);
            tenant.Property(x => x.Region).HasMaxLength(200);
            tenant.Property(x => x.Notes).HasMaxLength(2000);
            tenant.Property(x => x.ConfigurationJson).HasColumnType("jsonb");

            tenant.HasIndex(x => x.Slug)
                .IsUnique()
                .HasFilter("\"Status\" != 4"); // 4 = TenantStatus.Archived
            tenant.HasIndex(x => x.Status);
            tenant.HasIndex(x => x.DocumentNumber);

            tenant.HasOne(x => x.Branding)
                .WithMany()
                .HasForeignKey(x => x.BrandingId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TenantMembership>(membership =>
        {
            membership.ToTable("TenantMemberships");
            membership.HasKey(x => x.Id);
            membership.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();

            membership.HasOne(x => x.Tenant)
                .WithMany(x => x.Memberships)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            membership.HasOne(x => x.User)
                .WithMany(x => x.TenantMemberships)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApplicationUser>(user =>
        {
            user.HasIndex(x => x.TenantId);
            user.HasOne(x => x.Tenant)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ApplicationRole>(role =>
        {
            role.HasIndex(x => x.TenantId);
        });
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

            // Pricing precision
            p.Property(x => x.CostPrice).HasPrecision(18, 2);
            p.Property(x => x.SalePrice).HasPrecision(18, 2);
            p.Property(x => x.WholesalePrice).HasPrecision(18, 2);
            p.Property(x => x.CommissionPercent).HasPrecision(5, 2);

            // Concurrency Token (PostgreSQL xmin)
            p.Property(x => x.Version).IsRowVersion();
            
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

    private void ConfigureServiceOrderModels(ModelBuilder modelBuilder)
    {
        // ServiceOrder
        modelBuilder.Entity<Models.ServiceOrders.ServiceOrder>(entity =>
        {
            entity.ToTable("ServiceOrders");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.OrderNumber).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.OrderNumber).IsUnique();

            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Status);

            entity.Property(e => e.DeviceBrand).HasMaxLength(100);
            entity.Property(e => e.DeviceModel).HasMaxLength(100);
            entity.Property(e => e.DeviceType).HasMaxLength(50);
            entity.Property(e => e.SerialNumber).HasMaxLength(100);
            entity.Property(e => e.Password).HasMaxLength(20);
            entity.Property(e => e.Accessories).HasColumnType("jsonb");

            entity.Property(e => e.ProblemDescription).HasMaxLength(2000);
            entity.Property(e => e.TechnicalNotes).HasMaxLength(2000);
            entity.Property(e => e.CustomerNotes).HasMaxLength(2000);

            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.NetAmount).HasPrecision(18, 2);
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);

            entity.Property(e => e.WarrantyType).HasMaxLength(50);

            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.EntryDate);
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.Customer)
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Items)
                .WithOne(i => i.ServiceOrder)
                .HasForeignKey(i => i.ServiceOrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ServiceOrderItem
        modelBuilder.Entity<Models.ServiceOrders.ServiceOrderItem>(entity =>
        {
            entity.ToTable("ServiceOrderItems");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Description).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ServiceType).HasMaxLength(50);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.TechnicalDetails).HasMaxLength(2000);

            entity.HasIndex(e => e.ServiceOrderId);

            entity.HasOne(e => e.ServiceOrder)
                .WithMany(o => o.Items)
                .HasForeignKey(e => e.ServiceOrderId)
                .OnDelete(DeleteBehavior.Cascade);
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
            s.Property(x => x.PaymentMethod).HasMaxLength(50);

            s.HasIndex(x => x.TaxId).IsUnique();
            s.HasIndex(x => x.Name);
            s.HasIndex(x => x.Email);
            s.HasIndex(x => x.IsActive);
            s.HasIndex(x => x.CategoryId);

            s.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

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
            c.Property(x => x.CreatedByUserId).IsRequired(false); // Nullable for backward compatibility with existing data
            
            c.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Commission
        modelBuilder.Entity<Commission>(c =>
        {
            c.ToTable("Commissions");
            c.HasKey(x => x.Id);
            c.Property(x => x.ProfitAmount).HasPrecision(18, 2);
            c.Property(x => x.CommissionPercent).HasPrecision(5, 2);
            c.Property(x => x.CommissionAmount).HasPrecision(18, 2);

            c.HasIndex(x => x.SaleId);
            c.HasIndex(x => x.SaleItemId);
            c.HasIndex(x => x.ProductId);
            c.HasIndex(x => x.UserId);
            c.HasIndex(x => x.Status);
            c.HasIndex(x => x.PayrollId);
            c.HasIndex(x => new { x.UserId, x.Status, x.CreatedAt });

            c.HasOne(x => x.Sale)
                .WithMany()
                .HasForeignKey(x => x.SaleId)
                .OnDelete(DeleteBehavior.Restrict);

            c.HasOne(x => x.SaleItem)
                .WithMany()
                .HasForeignKey(x => x.SaleItemId)
                .OnDelete(DeleteBehavior.Restrict);

            c.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            c.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            c.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            c.HasOne(x => x.UpdatedByUser)
                .WithMany()
                .HasForeignKey(x => x.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            c.HasOne(x => x.Payroll)
                .WithMany()
                .HasForeignKey(x => x.PayrollId)
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
    
    private void ConfigureAssetModels(ModelBuilder modelBuilder)
    {
        // AssetCategory
        modelBuilder.Entity<AssetCategory>(c =>
        {
            c.ToTable("AssetCategories");
            c.HasKey(x => x.Id);
            c.Property(x => x.Name).HasMaxLength(100).IsRequired();
            c.Property(x => x.Description).HasMaxLength(500);
            c.Property(x => x.Icon).HasMaxLength(500); // MudBlazor icons are SVG paths, need more space
            c.Property(x => x.CreatedAt).HasConversion(
                v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                v => v);
            
            c.HasIndex(x => x.Name);
            c.HasIndex(x => x.IsActive);
        });
        
        // Asset
        modelBuilder.Entity<Asset>(a =>
        {
            a.ToTable("Assets");
            a.HasKey(x => x.Id);
            a.Property(x => x.AssetCode).HasMaxLength(50).IsRequired();
            a.Property(x => x.Name).HasMaxLength(200).IsRequired();
            a.Property(x => x.Description).HasMaxLength(2000);
            a.Property(x => x.SerialNumber).HasMaxLength(100);
            a.Property(x => x.Manufacturer).HasMaxLength(100);
            a.Property(x => x.Model).HasMaxLength(100);
            a.Property(x => x.InvoiceNumber).HasMaxLength(50);
            a.Property(x => x.Location).HasMaxLength(200);
            a.Property(x => x.Notes).HasMaxLength(2000);
            a.Property(x => x.ImageUrl).HasMaxLength(500);
            a.Property(x => x.PurchaseValue).HasPrecision(18, 2);
            a.Property(x => x.PurchaseDate).HasConversion(
                v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : (DateTime?)null,
                v => v);
            a.Property(x => x.WarrantyExpiryDate).HasConversion(
                v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : (DateTime?)null,
                v => v);
            a.Property(x => x.CreatedAt).HasConversion(
                v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                v => v);
            a.Property(x => x.UpdatedAt).HasConversion(
                v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : (DateTime?)null,
                v => v);
            
            a.HasIndex(x => x.AssetCode).IsUnique();
            a.HasIndex(x => x.SerialNumber);
            a.HasIndex(x => x.CategoryId);
            a.HasIndex(x => x.Status);
            a.HasIndex(x => x.IsActive);
            a.HasIndex(x => x.SupplierId);
            
            a.HasOne(x => x.Category)
                .WithMany(x => x.Assets)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // AssetAssignment
        modelBuilder.Entity<AssetAssignment>(aa =>
        {
            aa.ToTable("AssetAssignments");
            aa.HasKey(x => x.Id);
            aa.Property(x => x.AssignmentNotes).HasMaxLength(1000);
            aa.Property(x => x.ReturnNotes).HasMaxLength(1000);
            aa.Property(x => x.AssignedDate).HasConversion(
                v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                v => v);
            aa.Property(x => x.ReturnedDate).HasConversion(
                v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : (DateTime?)null,
                v => v);
            aa.Property(x => x.CreatedAt).HasConversion(
                v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                v => v);
            aa.Property(x => x.UpdatedAt).HasConversion(
                v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : (DateTime?)null,
                v => v);
            
            aa.HasIndex(x => x.AssetId);
            aa.HasIndex(x => x.AssignedToUserId);
            aa.HasIndex(x => x.AssignedDate);
            aa.HasIndex(x => x.ReturnedDate);
            aa.HasIndex(x => new { x.AssetId, x.ReturnedDate });
            
            aa.HasOne(x => x.Asset)
                .WithMany(x => x.Assignments)
                .HasForeignKey(x => x.AssetId)
                .OnDelete(DeleteBehavior.Restrict);
                
            aa.HasOne(x => x.AssignedToUser)
                .WithMany()
                .HasForeignKey(x => x.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            aa.HasOne(x => x.AssignedByUser)
                .WithMany()
                .HasForeignKey(x => x.AssignedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            aa.HasOne<ApplicationUser>(x => x.ReturnedByUser)
                .WithMany()
                .HasForeignKey(x => x.ReturnedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        
        // AssetMaintenance
        modelBuilder.Entity<AssetMaintenance>(am =>
        {
            am.ToTable("AssetMaintenances");
            am.HasKey(x => x.Id);
            am.Property(x => x.Description).HasMaxLength(1000).IsRequired();
            am.Property(x => x.ServiceDetails).HasMaxLength(2000);
            am.Property(x => x.ServiceProvider).HasMaxLength(200);
            am.Property(x => x.InvoiceNumber).HasMaxLength(50);
            am.Property(x => x.Notes).HasMaxLength(2000);
            am.Property(x => x.Cost).HasPrecision(18, 2);
            am.Property(x => x.ScheduledDate).HasConversion(
                v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                v => v);
            am.Property(x => x.StartedDate).HasConversion(
                v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : (DateTime?)null,
                v => v);
            am.Property(x => x.CompletedDate).HasConversion(
                v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : (DateTime?)null,
                v => v);
            am.Property(x => x.NextMaintenanceDate).HasConversion(
                v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : (DateTime?)null,
                v => v);
            am.Property(x => x.CreatedAt).HasConversion(
                v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                v => v);
            am.Property(x => x.UpdatedAt).HasConversion(
                v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : (DateTime?)null,
                v => v);
            
            am.HasIndex(x => x.AssetId);
            am.HasIndex(x => x.Type);
            am.HasIndex(x => x.Status);
            am.HasIndex(x => x.ScheduledDate);
            am.HasIndex(x => x.NextMaintenanceDate);
            am.HasIndex(x => new { x.Status, x.ScheduledDate });
            
            am.HasOne(x => x.Asset)
                .WithMany(x => x.MaintenanceRecords)
                .HasForeignKey(x => x.AssetId)
                .OnDelete(DeleteBehavior.Restrict);
                
            am.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            am.HasOne<ApplicationUser>(x => x.CompletedByUser)
                .WithMany()
                .HasForeignKey(x => x.CompletedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        
        // AssetDocument
        modelBuilder.Entity<AssetDocument>(ad =>
        {
            ad.ToTable("AssetDocuments");
            ad.HasKey(x => x.Id);
            ad.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            ad.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
            ad.Property(x => x.FilePath).HasMaxLength(1000).IsRequired();
            ad.Property(x => x.ContentType).HasMaxLength(200).IsRequired();
            ad.Property(x => x.Description).HasMaxLength(1000);
            ad.Property(x => x.DocumentNumber).HasMaxLength(100);
            ad.Property(x => x.DocumentDate).HasConversion(
                v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : (DateTime?)null,
                v => v);
            ad.Property(x => x.ExpiryDate).HasConversion(
                v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : (DateTime?)null,
                v => v);
            ad.Property(x => x.CreatedAt).HasConversion(
                v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                v => v);
            ad.Property(x => x.UpdatedAt).HasConversion(
                v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : (DateTime?)null,
                v => v);
            
            ad.HasIndex(x => x.AssetId);
            ad.HasIndex(x => x.Type);
            ad.HasIndex(x => x.DocumentDate);
            ad.HasIndex(x => x.ExpiryDate);
            ad.HasIndex(x => new { x.AssetId, x.Type });
            
            ad.HasOne(x => x.Asset)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.AssetId)
                .OnDelete(DeleteBehavior.Cascade);
                
            ad.HasOne(x => x.UploadedByUser)
                .WithMany()
                .HasForeignKey(x => x.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        
        // AssetTransfer
        modelBuilder.Entity<AssetTransfer>(at =>
        {
            at.ToTable("AssetTransfers");
            at.HasKey(x => x.Id);
            at.Property(x => x.FromLocation).HasMaxLength(200).IsRequired();
            at.Property(x => x.ToLocation).HasMaxLength(200).IsRequired();
            at.Property(x => x.Reason).HasMaxLength(1000);
            at.Property(x => x.Notes).HasMaxLength(2000);
            at.Property(x => x.TransferDate).HasConversion(
                v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                v => v);
            at.Property(x => x.ApprovedDate).HasConversion(
                v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : (DateTime?)null,
                v => v);
            at.Property(x => x.CompletedDate).HasConversion(
                v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : (DateTime?)null,
                v => v);
            at.Property(x => x.CreatedAt).HasConversion(
                v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                v => v);
            at.Property(x => x.UpdatedAt).HasConversion(
                v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : (DateTime?)null,
                v => v);
            
            at.HasIndex(x => x.AssetId);
            at.HasIndex(x => x.FromDepartmentId);
            at.HasIndex(x => x.ToDepartmentId);
            at.HasIndex(x => x.TransferDate);
            at.HasIndex(x => x.Status);
            at.HasIndex(x => new { x.Status, x.TransferDate });
            
            at.HasOne(x => x.Asset)
                .WithMany(x => x.Transfers)
                .HasForeignKey(x => x.AssetId)
                .OnDelete(DeleteBehavior.Restrict);
                
            at.HasOne(x => x.FromDepartment)
                .WithMany()
                .HasForeignKey(x => x.FromDepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
                
            at.HasOne(x => x.ToDepartment)
                .WithMany()
                .HasForeignKey(x => x.ToDepartmentId)
                .OnDelete(DeleteBehavior.SetNull);
                
            at.HasOne(x => x.RequestedByUser)
                .WithMany()
                .HasForeignKey(x => x.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            at.HasOne<ApplicationUser>(x => x.ApprovedByUser)
                .WithMany()
                .HasForeignKey(x => x.ApprovedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
                
            at.HasOne<ApplicationUser>(x => x.CompletedByUser)
                .WithMany()
                .HasForeignKey(x => x.CompletedByUserId)
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
            a.Property(x => x.EntityDescription).HasMaxLength(500);
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
            a.Property(x => x.References).HasColumnType("jsonb");
            
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
            
            // Índice para filtro por tenant
            a.HasIndex(x => new { x.TenantId, x.Timestamp })
                .HasDatabaseName("idx_audit_tenant_timeline");
            
            // Índice combinado para tenant + entidade
            a.HasIndex(x => new { x.TenantId, x.EntityName, x.Timestamp })
                .HasDatabaseName("idx_audit_tenant_entity_timeline");
            
            // Query filter para multi-tenancy
            a.HasQueryFilter(log => 
                _tenantContextAccessor == null || 
                _tenantContextAccessor.Current == null || 
                !_tenantContextAccessor.Current.TenantId.HasValue || 
                log.TenantId == 0 || // Logs sem tenant (sistema)
                log.TenantId == _tenantContextAccessor.Current.TenantId.GetValueOrDefault());
        });
    }

    private void ConfigurePayrollModels(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PayrollResult>(entity =>
        {
            entity.ToTable("PayrollResults");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.EmployeeNameSnapshot).HasMaxLength(200).IsRequired();
            entity.Property(x => x.EmployeeCpfSnapshot).HasMaxLength(14);
            entity.Property(x => x.DepartmentSnapshot).HasMaxLength(100);
            entity.Property(x => x.PositionSnapshot).HasMaxLength(100);
            entity.Property(x => x.BankNameSnapshot).HasMaxLength(100);
            entity.Property(x => x.BankAgencySnapshot).HasMaxLength(10);
            entity.Property(x => x.BankAccountSnapshot).HasMaxLength(20);

            entity.Property(x => x.BaseSalarySnapshot).HasPrecision(18, 2);
            entity.Property(x => x.TotalEarnings).HasPrecision(18, 2);
            entity.Property(x => x.TotalDeductions).HasPrecision(18, 2);
            entity.Property(x => x.TotalContributions).HasPrecision(18, 2);
            entity.Property(x => x.NetAmount).HasPrecision(18, 2);
            entity.Property(x => x.GrossAmount).HasPrecision(18, 2);
            entity.Property(x => x.InssAmount).HasPrecision(18, 2);
            entity.Property(x => x.IrrfAmount).HasPrecision(18, 2);
            entity.Property(x => x.AdditionalEmployerCost).HasPrecision(18, 2);

            entity.HasIndex(x => new { x.PayrollPeriodId, x.EmployeeId }).IsUnique();

            entity.HasOne(x => x.PayrollPeriod)
                .WithMany(p => p.Results)
                .HasForeignKey(x => x.PayrollPeriodId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Employee)
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.PayrollEntry)
                .WithMany()
                .HasForeignKey(x => x.PayrollEntryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(x => x.UpdatedBy)
                .WithMany()
                .HasForeignKey(x => x.UpdatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PayrollComponent>(entity =>
        {
            entity.ToTable("PayrollComponents");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Code).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.BaseAmount).HasPrecision(18, 2);
            entity.Property(x => x.ReferenceQuantity).HasPrecision(18, 2);

            entity.HasIndex(x => x.PayrollResultId);
            entity.HasIndex(x => x.Type);

            entity.HasOne(x => x.PayrollResult)
                .WithMany(r => r.Components)
                .HasForeignKey(x => x.PayrollResultId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PayrollSlip>(entity =>
        {
            entity.ToTable("PayrollSlips");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.FilePath).HasMaxLength(500).IsRequired();
            entity.Property(x => x.FileHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(500);

            entity.HasIndex(x => x.PayrollResultId).IsUnique();

            entity.HasOne(x => x.PayrollResult)
                .WithOne(r => r.Slip)
                .HasForeignKey<PayrollSlip>(x => x.PayrollResultId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.GeneratedBy)
                .WithMany()
                .HasForeignKey(x => x.GeneratedById)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.UpdatedBy)
                .WithMany()
                .HasForeignKey(x => x.UpdatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PayrollTaxBracket>(entity =>
        {
            entity.ToTable("PayrollTaxBrackets");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.RangeStart).HasPrecision(18, 2);
            entity.Property(x => x.RangeEnd).HasPrecision(18, 2);
            entity.Property(x => x.Rate).HasPrecision(5, 4);
            entity.Property(x => x.Deduction).HasPrecision(18, 2);

            entity.HasIndex(x => new { x.TaxType, x.IsActive });
            entity.HasIndex(x => new { x.TaxType, x.EffectiveFrom });

            entity.Property(x => x.SortOrder).HasDefaultValue(0);
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
            p.Property(x => x.TotalGrossAmount).HasPrecision(18, 2);
            p.Property(x => x.TotalNetAmount).HasPrecision(18, 2);
            p.Property(x => x.TotalInssAmount).HasPrecision(18, 2);
            p.Property(x => x.TotalIrrfAmount).HasPrecision(18, 2);
            p.Property(x => x.TotalEmployerCost).HasPrecision(18, 2);
            p.Property(x => x.Notes).HasMaxLength(1000);

            p.HasIndex(x => new { x.ReferenceYear, x.ReferenceMonth }).IsUnique();

            p.HasOne(x => x.CreatedBy)
                .WithMany()
                .HasForeignKey(x => x.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            p.HasOne(x => x.UpdatedBy)
                .WithMany()
                .HasForeignKey(x => x.UpdatedById)
                .OnDelete(DeleteBehavior.Restrict);

            p.HasOne(x => x.ApprovedBy)
                .WithMany()
                .HasForeignKey(x => x.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            p.HasOne(x => x.PaidBy)
                .WithMany()
                .HasForeignKey(x => x.PaidById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PayrollEntry>(e =>
        {
            e.ToTable("PayrollEntries");
            e.HasKey(x => x.Id);
            e.Property(x => x.Faltas).HasColumnType("decimal(18,2)");
            e.Property(x => x.Abonos).HasColumnType("decimal(18,2)");
            e.Property(x => x.HorasExtras).HasColumnType("decimal(18,2)");
            e.Property(x => x.Atrasos).HasColumnType("decimal(18,2)");
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

    private void ConfigureDashboardModels(ModelBuilder modelBuilder)
    {
        // UserDashboardLayout
        modelBuilder.Entity<UserDashboardLayout>(layout =>
        {
            layout.ToTable("UserDashboardLayouts");
            layout.HasKey(x => x.Id);
            
            layout.Property(x => x.LayoutJson).HasColumnType("jsonb").IsRequired();
            layout.Property(x => x.LayoutType).HasMaxLength(20).IsRequired();
            layout.Property(x => x.Columns).HasDefaultValue(3);
            layout.Property(x => x.LastModified).IsRequired();
            layout.Property(x => x.CreatedAt).IsRequired();
            
            layout.HasIndex(x => x.UserId).IsUnique();
            
                layout.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // WidgetRoleConfiguration
        modelBuilder.Entity<WidgetRoleConfiguration>(config =>
        {
            config.ToTable("WidgetRoleConfigurations");
            config.HasKey(x => x.Id);
            
            config.Property(x => x.ProviderKey).HasMaxLength(50).IsRequired();
            config.Property(x => x.WidgetKey).HasMaxLength(100).IsRequired();
            config.Property(x => x.RolesJson).HasColumnType("jsonb");
            config.Property(x => x.LastModified).IsRequired();
            
            config.HasIndex(x => new { x.ProviderKey, x.WidgetKey }).IsUnique();
            
            config.HasOne(x => x.ModifiedByUser)
                .WithMany()
                .HasForeignKey(x => x.ModifiedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
    
    private void ConfigureModulePermissionModels(ModelBuilder modelBuilder)
    {
        // ModulePermission
        modelBuilder.Entity<ModulePermission>(mp =>
        {
            mp.ToTable("ModulePermissions");
            mp.HasKey(x => x.Id);
            
            mp.Property(x => x.ModuleKey).HasMaxLength(50).IsRequired();
            mp.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            mp.Property(x => x.Description).HasMaxLength(500);
            mp.Property(x => x.Icon).HasMaxLength(100);
            mp.Property(x => x.DisplayOrder).HasDefaultValue(0);
            mp.Property(x => x.IsActive).HasDefaultValue(true);
            
            mp.HasIndex(x => x.ModuleKey).IsUnique();
            mp.HasIndex(x => x.DisplayOrder);
        });
        
        // RoleModulePermission (junction table)
        modelBuilder.Entity<RoleModulePermission>(rmp =>
        {
            rmp.ToTable("RoleModulePermissions");
            rmp.HasKey(x => x.Id);
            
            rmp.HasIndex(x => new { x.RoleId, x.ModulePermissionId }).IsUnique();
            
            rmp.HasOne(x => x.Role)
                .WithMany(r => r.ModulePermissions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
                
            rmp.HasOne(x => x.ModulePermission)
                .WithMany(mp => mp.RolePermissions)
                .HasForeignKey(x => x.ModulePermissionId)
                .OnDelete(DeleteBehavior.Cascade);
                
            rmp.HasOne(x => x.GrantedByUser)
                .WithMany()
                .HasForeignKey(x => x.GrantedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ModuleActionPermission
        modelBuilder.Entity<ModuleActionPermission>(map =>
        {
            map.ToTable("ModuleActionPermissions");
            map.HasKey(x => x.Id);

            map.Property(x => x.ActionKey).HasMaxLength(100).IsRequired();
            map.Property(x => x.DisplayName).HasMaxLength(150).IsRequired();
            map.Property(x => x.Description).HasMaxLength(500);
            map.Property(x => x.DisplayOrder).HasDefaultValue(0);
            map.Property(x => x.IsActive).HasDefaultValue(true);

            map.HasIndex(x => new { x.ModulePermissionId, x.ActionKey }).IsUnique();
            map.HasIndex(x => new { x.ModulePermissionId, x.DisplayOrder });

            map.HasOne(x => x.ModulePermission)
                .WithMany(m => m.Actions)
                .HasForeignKey(x => x.ModulePermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RoleModuleActionPermission (junction table)
        modelBuilder.Entity<RoleModuleActionPermission>(rmap =>
        {
            rmap.ToTable("RoleModuleActionPermissions");
            rmap.HasKey(x => x.Id);

            rmap.HasIndex(x => new { x.RoleId, x.ModuleActionPermissionId }).IsUnique();

            rmap.HasOne(x => x.Role)
                .WithMany(r => r.ModuleActionPermissions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            rmap.HasOne(x => x.ModuleActionPermission)
                .WithMany(a => a.RolePermissions)
                .HasForeignKey(x => x.ModuleActionPermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            rmap.HasOne(x => x.GrantedByUser)
                .WithMany()
                .HasForeignKey(x => x.GrantedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        
        // ApplicationRole additional configuration
        modelBuilder.Entity<ApplicationRole>(role =>
        {
            role.Property(x => x.Description).HasMaxLength(500);
            role.Property(x => x.Icon).HasMaxLength(100);
        });
    }
    
    private void ConfigureChatbotModels(ModelBuilder modelBuilder)
    {
        // ChatConversation
        modelBuilder.Entity<ChatConversation>(c =>
        {
            c.ToTable("ChatConversations");
            c.HasKey(x => x.Id);
            
            c.Property(x => x.Title).HasMaxLength(200).IsRequired();
            c.Property(x => x.CreatedAt).IsRequired();
            
            c.HasIndex(x => x.UserId);
            c.HasIndex(x => x.TenantId);
            c.HasIndex(x => new { x.UserId, x.CreatedAt });
            c.HasIndex(x => new { x.UserId, x.IsArchived });
            
            c.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Query filter for multi-tenancy
            c.HasQueryFilter(conv => 
                _tenantContextAccessor == null || 
                _tenantContextAccessor.Current == null || 
                !_tenantContextAccessor.Current.TenantId.HasValue || 
                conv.TenantId == _tenantContextAccessor.Current.TenantId.GetValueOrDefault());
        });
        
        // ChatMessage
        modelBuilder.Entity<ChatMessage>(m =>
        {
            m.ToTable("ChatMessages");
            m.HasKey(x => x.Id);
            
            m.Property(x => x.Role).HasMaxLength(20).IsRequired();
            m.Property(x => x.Content).IsRequired();
            m.Property(x => x.Timestamp).IsRequired();
            m.Property(x => x.Order).IsRequired();
            
            m.HasIndex(x => x.ConversationId);
            m.HasIndex(x => new { x.ConversationId, x.Order });
            
            m.HasOne(x => x.Conversation)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureTenantQueryFilters(ModelBuilder modelBuilder)
    {
        // Apply global query filters to all entities implementing IMustHaveTenant
        // This ensures multi-tenant data isolation at the query level
        // Filter logic:
        // - No filter when tenant context is not available (tests, admin operations, anonymous endpoints)
        // - Filter to current tenant when tenant context is available
        
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(IMustHaveTenant).IsAssignableFrom(entityType.ClrType))
                continue;
            
            // Skip entities that already have a query filter configured (e.g., AuditLog, ChatConversation)
            if (entityType.GetQueryFilter() != null)
                continue;
            
            var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
            var tenantIdProperty = System.Linq.Expressions.Expression.Property(parameter, nameof(IMustHaveTenant.TenantId));
            
            // Build the filter expression:
            // e => _tenantContextAccessor == null || 
            //      _tenantContextAccessor.Current == null || 
            //      !_tenantContextAccessor.Current.TenantId.HasValue || 
            //      e.TenantId == _tenantContextAccessor.Current.TenantId.GetValueOrDefault()
            
            var accessorField = System.Linq.Expressions.Expression.Constant(this);
            var accessorProperty = System.Linq.Expressions.Expression.Field(accessorField, "_tenantContextAccessor");
            
            // _tenantContextAccessor == null
            var accessorNull = System.Linq.Expressions.Expression.Equal(
                accessorProperty, 
                System.Linq.Expressions.Expression.Constant(null, typeof(ITenantContextAccessor)));
            
            // _tenantContextAccessor.Current
            var currentProperty = System.Linq.Expressions.Expression.Property(accessorProperty, "Current");
            
            // _tenantContextAccessor.Current == null
            var currentNull = System.Linq.Expressions.Expression.Equal(
                currentProperty, 
                System.Linq.Expressions.Expression.Constant(null, typeof(TenantContext)));
            
            // _tenantContextAccessor.Current.TenantId
            var currentTenantIdProperty = System.Linq.Expressions.Expression.Property(currentProperty, "TenantId");
            
            // !_tenantContextAccessor.Current.TenantId.HasValue
            var hasValueProperty = System.Linq.Expressions.Expression.Property(currentTenantIdProperty, "HasValue");
            var noTenantId = System.Linq.Expressions.Expression.Not(hasValueProperty);
            
            // _tenantContextAccessor.Current.TenantId.GetValueOrDefault()
            var getValueOrDefaultMethod = typeof(int?).GetMethod("GetValueOrDefault", Type.EmptyTypes)!;
            var currentTenantValue = System.Linq.Expressions.Expression.Call(currentTenantIdProperty, getValueOrDefaultMethod);
            
            // e.TenantId == _tenantContextAccessor.Current.TenantId.GetValueOrDefault()
            var tenantMatch = System.Linq.Expressions.Expression.Equal(tenantIdProperty, currentTenantValue);
            
            // Combine: accessorNull || currentNull || noTenantId || tenantMatch
            var filterExpression = System.Linq.Expressions.Expression.OrElse(
                System.Linq.Expressions.Expression.OrElse(
                    System.Linq.Expressions.Expression.OrElse(accessorNull, currentNull),
                    noTenantId),
                tenantMatch);
            
            var lambda = System.Linq.Expressions.Expression.Lambda(filterExpression, parameter);
            entityType.SetQueryFilter(lambda);
        }
    }
}

public class DateTimeToUtcConverter : ValueConverter<DateTime, DateTime>
{
    public DateTimeToUtcConverter() : base(
        v => v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    { }
}

public class NullableDateTimeToUtcConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableDateTimeToUtcConverter() : base(
        v => v.HasValue ? v.Value.ToUniversalTime() : v,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v)
    { }
}

