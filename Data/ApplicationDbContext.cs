using erp.Models;
using erp.Models.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseNpgsql("Host=localhost;Database=erp;Username=postgres;Password=123");

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        SetAuditProperties();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges()
    {
        SetAuditProperties();
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
            
            m.HasOne(x => x.Product)
                .WithMany(x => x.StockMovements)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
                
            m.HasOne(x => x.Warehouse)
                .WithMany(x => x.StockMovements)
                .HasForeignKey(x => x.WarehouseId)
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
}
