using CryptApp.Web.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CryptApp.Tests.Fixtures;

public class TestDatabaseFixture : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"test_{Guid.NewGuid()}.db";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory SQLite for testing
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite($"Data Source={_dbName}"));

            // Apply migrations (same schema path as production startup)
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
            
            // Seed test data
            SeedTestData(db);
        });
    }

    private void SeedTestData(ApplicationDbContext db)
    {
        // Add test data if needed
        if (!db.Items.Any())
        {
            db.Items.Add(new CryptApp.Web.Data.Models.Item
            {
                Name = "Test Item 1",
                Description = "A test item for unit testing",
                CreatedAt = DateTime.UtcNow
            });
            db.Items.Add(new CryptApp.Web.Data.Models.Item
            {
                Name = "Test Item 2",
                Description = "Another test item",
                CreatedAt = DateTime.UtcNow
            });
            db.SaveChanges();
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        
        // Clean up test database
        if (File.Exists(_dbName))
        {
            try
            {
                File.Delete(_dbName);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}