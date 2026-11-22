using SeanceScheduler.Web.Data;
using SeanceScheduler.Web.Features.Items;
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

// Unknown non-API pages (GET only) get the HTML not-found page; API 404s keep their own (JSON or empty) body,
// and other methods are not re-executed (a re-executed POST would hit antiforgery instead of returning 404).
app.UseWhen(
    context => HttpMethods.IsGet(context.Request.Method) && !context.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));

app.UseStaticFiles();
// Explicit routing after status code pages so the re-executed /not-found request is routed again.
app.UseRouting();
app.UseAntiforgery();

// Map API endpoints
app.MapItemsEndpoints();

// Map Razor components
app.MapRazorComponents<SeanceScheduler.Web.Components.App>();

// Health check endpoint
app.MapGet("/api/health", (ILogger<Program> logger) =>
{
    logger.LogDebug("Health check requested");
    return Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow, Version = "1.0.0" });
});

await app.RunAsync();

public partial class Program { }