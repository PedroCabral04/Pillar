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
    }
}
