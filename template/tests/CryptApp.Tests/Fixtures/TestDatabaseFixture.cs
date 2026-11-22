using CryptApp.Web.Data;
using CryptApp.Web.Data.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CryptApp.Tests.Fixtures;

/// <summary>
/// Boots the real app against a throw-away SQLite file. Program.cs applies migrations on startup;
/// the fixture only swaps the connection string and seeds sample rows.
/// </summary>
public class TestDatabaseFixture : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"crypt_test_{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite($"Data Source={_dbPath};Pooling=False"));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (!db.Items.Any())
        {
            db.Items.AddRange(
                new Item { Name = "Test Item 1", Description = "A test item" },
                new Item { Name = "Test Item 2", Description = "Another test item" });
            db.SaveChanges();
        }

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }
}
