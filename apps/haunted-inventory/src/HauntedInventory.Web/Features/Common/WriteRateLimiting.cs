using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace HauntedInventory.Web.Features.Common;

/// <summary>Per-client limit for state-changing requests (anything but GET/HEAD). Disabled when PermitLimit is 0.</summary>
public sealed class WriteRateLimitOptions
{
    public const string SectionName = "WriteRateLimit";

    /// <summary>Writes allowed per client IP per window; 0 disables the limit.</summary>
    public int PermitLimit { get; set; }

    public int WindowSeconds { get; set; } = 60;
}

public static class WriteRateLimiting
{
    /// <summary>
    /// Registers a global fixed-window limiter partitioned by client IP. Behind a reverse proxy enable
    /// forwarded headers (ASPNETCORE_FORWARDEDHEADERS_ENABLED) so the IP is the real client, not the proxy.
    /// </summary>
    public static IServiceCollection AddWriteRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WriteRateLimitOptions>(configuration.GetSection(WriteRateLimitOptions.SectionName));
        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = async (context, cancellationToken) =>
            {
                var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<WriteRateLimitOptions>>().Value;
                context.HttpContext.Response.Headers.RetryAfter = options.WindowSeconds.ToString();
                context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
                await context.HttpContext.Response.WriteAsync("Too many changes from your address. Try again in a minute.", cancellationToken);
            };
            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var options = httpContext.RequestServices.GetRequiredService<IOptions<WriteRateLimitOptions>>().Value;
                var method = httpContext.Request.Method;
                if (options.PermitLimit <= 0 || HttpMethods.IsGet(method) || HttpMethods.IsHead(method))
                {
                    return RateLimitPartition.GetNoLimiter("unlimited");
                }

                var client = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(client, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.PermitLimit,
                    Window = TimeSpan.FromSeconds(Math.Max(1, options.WindowSeconds)),
                    QueueLimit = 0,
                });
            });
        });
        return services;
    }
}
