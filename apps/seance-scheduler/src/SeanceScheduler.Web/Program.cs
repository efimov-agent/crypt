using SeanceScheduler.Web.Data;
using SeanceScheduler.Web.Features.Common;
using SeanceScheduler.Web.Features.Items;
using SeanceScheduler.Web.Features.Mediums;
using SeanceScheduler.Web.Features.Seances;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddRazorComponents();
builder.Services.AddEndpointsApiExplorer();
// RFC 9457 bodies for API errors that never reach an endpoint (binding failures, unhandled exceptions)
builder.Services.AddProblemDetails();

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
builder.Services.AddScoped<MediumsService>();
builder.Services.AddScoped<SeancesService>();

// Register TimeProvider
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

// Apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
    // WAL lets readers proceed while a booking transaction holds the write lock (persistent per DB file)
    await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
}

// Configure the HTTP request pipeline
// API requests get application/problem+json for unhandled exceptions and empty error responses;
// pages keep the HTML error page.
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    branch =>
    {
        branch.UseExceptionHandler(new ExceptionHandlerOptions
        {
            // Binding failures (bad query value, malformed JSON) are client errors, not 500s
            // A database locked by another writer past the busy timeout is transient: 503 so clients may retry.
            StatusCodeSelector = ex => ex switch
            {
                BadHttpRequestException bad => bad.StatusCode,
                _ when SqliteErrors.IsBusy(ex) => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status500InternalServerError,
            },
        });
        branch.UseStatusCodePages();
    });

if (!app.Environment.IsDevelopment())
{
    app.UseWhen(
        context => !context.Request.Path.StartsWithSegments("/api"),
        branch => branch.UseExceptionHandler("/Error", createScopeForErrors: true));
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
app.MapMediumsEndpoints();
app.MapSeancesEndpoints();

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