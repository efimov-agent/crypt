using SeanceScheduler.Web.Data.Models;
using SeanceScheduler.Web.Features.Items;
using Microsoft.EntityFrameworkCore;

namespace SeanceScheduler.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Item> Items => Set<Item>();
    public DbSet<Medium> Mediums => Set<Medium>();
    public DbSet<Seance> Seances => Set<Seance>();

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

        // Configure Medium entity
        modelBuilder.Entity<Medium>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Specialty)
                .HasMaxLength(200);
            // Unique index on Name (case-insensitive via COLLATE NOCASE in SQLite)
            entity.HasIndex(e => e.Name)
                .IsUnique();
            // Index on IsActive for filtering
            entity.HasIndex(e => e.IsActive);
            // UTC conversion for CreatedAt
            entity.Property(e => e.CreatedAt)
                .HasConversion(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });

        // Configure Seance entity
        modelBuilder.Entity<Seance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ClientName)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(16);
            // Foreign key with DeleteBehavior.Restrict (history survives medium deactivation)
            entity.HasOne(e => e.Medium)
                .WithMany(m => m.Seances)
                .HasForeignKey(e => e.MediumId)
                .OnDelete(DeleteBehavior.Restrict);
            // Index on (MediumId, StartsAt) for overlap detection
            entity.HasIndex(e => new { e.MediumId, e.StartsAt });
            // Index on Status for filtering
            entity.HasIndex(e => e.Status);
            // Index on CreatedAt for sorting
            entity.HasIndex(e => e.CreatedAt);
            // UTC conversion for all DateTime properties
            entity.Property(e => e.StartsAt)
                .HasConversion(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            entity.Property(e => e.EndsAt)
                .HasConversion(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            entity.Property(e => e.CreatedAt)
                .HasConversion(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            entity.Property(e => e.CancelledAt)
                .HasConversion(v => v.HasValue ? v.Value.ToUniversalTime() : v,
                               v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
        });
    }
}