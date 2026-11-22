using HauntedInventory.Web.Data;
using HauntedInventory.Web.Features.Items;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorComponents();
builder.Services.AddEndpointsApiExplorer();

// App_Data holds the SQLite database and Data Protection keys (a volume in Docker)
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDir);

// Persist Data Protection keys so antiforgery tokens survive container restarts
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataDir, "keys")));

// Configure EF Core with SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? $"Data Source={Path.Combine(dataDir, "app.db")}";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Register feature services
builder.Services.AddScoped<ItemsService>();
builder.Services.AddScoped<StockMovementsService>();

var app = builder.Build();

// Apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

// Map API endpoints
app.MapItemsEndpoints();

// Map Razor components
app.MapRazorComponents<HauntedInventory.Web.Components.App>();

// Health check endpoint
app.MapGet("/api/health", (ILogger<Program> logger) =>
{
    logger.LogDebug("Health check requested");
    return Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow, Version = "1.0.0" });
});

await app.RunAsync();

public partial class Program { }