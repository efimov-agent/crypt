using CryptApp.Web.Data.Models;
using CryptApp.Web.Features.Items;
using Microsoft.EntityFrameworkCore;

namespace CryptApp.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options)
    {
    }

    public DbSet<Item> Items => Set<Item>();

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
            entity.HasIndex(e => e.Name);
            // SQLite has no DateTimeKind: store UTC and read it back as UTC
            entity.Property(e => e.CreatedAt)
                .HasConversion(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            entity.Property(e => e.UpdatedAt)
                .HasConversion(v => v.HasValue ? v.Value.ToUniversalTime() : v,
                               v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
        });
    }
}