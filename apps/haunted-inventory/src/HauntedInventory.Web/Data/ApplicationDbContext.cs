using HauntedInventory.Web.Data.Models;
using HauntedInventory.Web.Features.Items;
using Microsoft.EntityFrameworkCore;

namespace HauntedInventory.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options)
    {
    }

    public DbSet<Item> Items => Set<Item>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Item entity
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(CreateItemRequest.NameMaxLength);
            entity.Property(e => e.Description)
                .HasMaxLength(CreateItemRequest.DescriptionMaxLength);
            entity.Property(e => e.Sku)
                .IsRequired()
                .HasMaxLength(32);
            entity.HasIndex(e => e.Sku).IsUnique();
            entity.HasIndex(e => e.Name);
            // SQLite has no DateTimeKind: store UTC and read it back as UTC
            entity.Property(e => e.CreatedAt)
                .HasConversion(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            entity.Property(e => e.UpdatedAt)
                .HasConversion(v => v.HasValue ? v.Value.ToUniversalTime() : v,
                               v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
            // Check constraints for non-negative values
            entity.ToTable(t => t.HasCheckConstraint("CK_Items_Quantity", "Quantity >= 0"));
            entity.ToTable(t => t.HasCheckConstraint("CK_Items_ReorderLevel", "ReorderLevel >= 0"));
        });

        // Configure StockMovement entity
        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Delta)
                .IsRequired();
            entity.Property(e => e.Reason)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Note)
                .HasMaxLength(200);
            entity.Property(e => e.ResultingQuantity)
                .IsRequired();
            entity.Property(e => e.RecordedAt)
                .HasConversion(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            entity.HasOne(e => e.Item)
                .WithMany(i => i.Movements)
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for efficient history queries
            entity.HasIndex(e => new { e.ItemId, e.RecordedAt });
        });
    }
}