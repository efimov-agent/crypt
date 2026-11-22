using System.Net;
using System.Net.Http.Json;
using HauntedInventory.Tests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace HauntedInventory.Tests.Features;

public class WriteRateLimitingTests
{
    private sealed class LimitedFixture : TestDatabaseFixture
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("WriteRateLimit:PermitLimit", "2");
            builder.UseSetting("WriteRateLimit:WindowSeconds", "3600");
        }
    }

    // Invalid bodies are enough: the limiter runs before the endpoint, and nothing is written.
    private static Task<HttpResponseMessage> PostInvalid(HttpClient client) =>
        client.PostAsJsonAsync("/api/items", new { name = "" });

    [Fact]
    public async Task Writes_AboveLimit_Get429_ReadsStillServed()
    {
        using var factory = new LimitedFixture();
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.BadRequest, (await PostInvalid(client)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PostInvalid(client)).StatusCode);

        var rejected = await PostInvalid(client);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal("3600", rejected.Headers.GetValues("Retry-After").Single());
        Assert.Contains("Too many changes", await rejected.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/items")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/")).StatusCode);
    }

    [Fact]
    public async Task WithoutConfiguration_WritesAreNotLimited()
    {
        using var factory = new TestDatabaseFixture();
        var client = factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(HttpStatusCode.BadRequest, (await PostInvalid(client)).StatusCode);
        }
    }
}
