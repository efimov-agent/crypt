using CryptApp.Web.Data;
using CryptApp.Web.Features.Items;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorComponents();
builder.Services.AddEndpointsApiExplorer();

// Configure logging
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

// Configure EF Core with SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=App_Data/app.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// Register feature services
builder.Services.AddScoped<ItemsService>();

var app = builder.Build();

// Ensure the SQLite folder exists and apply pending migrations on startup
Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "App_Data"));
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
app.MapRazorComponents<CryptApp.Web.Components.App>();

// Health check endpoint
app.MapGet("/api/health", async (CancellationToken cancellationToken) =>
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Health check requested at {Time}", DateTime.UtcNow);
    
    return Results.Ok(new 
    { 
        Status = "Healthy", 
        Timestamp = DateTime.UtcNow, 
        Version = "1.0.0" 
    });
});

await app.RunAsync();

public partial class Program { }