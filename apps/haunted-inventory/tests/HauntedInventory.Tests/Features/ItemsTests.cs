using System.Net;
using System.Net.Http.Json;
using HauntedInventory.Tests.Fixtures;
using HauntedInventory.Web.Data.Models;
using HauntedInventory.Web.Features.Items;
using Xunit;

namespace HauntedInventory.Tests.Features;

[Collection("Integration tests")]
public class ItemsTests : IClassFixture<TestDatabaseFixture>
{
    private readonly HttpClient _client;

    public ItemsTests(TestDatabaseFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/health");

        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("timestamp", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetItems_ReturnsSeededItems()
    {
        var items = await _client.GetFromJsonAsync<List<ItemDto>>("/api/items");

        Assert.NotNull(items);
        Assert.Contains(items, i => i.Name == "Test Item 1");
    }

    [Fact]
    public async Task CreateItem_ReturnsCreatedWithLocation_AndItemIsReadable()
    {
        var response = await _client.PostAsJsonAsync("/api/items", new { name = "  Lantern  ", description = "Glows", sku = "lantern-001" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(created);
        Assert.Equal("Lantern", created.Name);
        Assert.Equal("LANTERN-001", created.Sku);
        Assert.Equal($"/api/items/{created.Id}", response.Headers.Location?.OriginalString);

        var fetched = await _client.GetFromJsonAsync<ItemDto>(response.Headers.Location);
        Assert.Equal("Glows", fetched?.Description);
    }

    [Fact]
    public async Task CreateItem_IgnoresClientSuppliedId()
    {
        var response = await _client.PostAsJsonAsync("/api/items", new { id = 1, name = "Overposted", sku = "overposted" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotEqual(1, created?.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateItem_WithoutName_ReturnsValidationProblem(string? name)
    {
        var response = await _client.PostAsJsonAsync("/api/items", new { name, sku = "test-sku" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Name", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task CreateItem_TooLong_ReturnsValidationProblem()
    {
        var response = await _client.PostAsJsonAsync("/api/items",
            new { name = new string('x', 201), description = new string('y', 1001), sku = "test-sku" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Name", body);
        Assert.Contains("Description", body);
    }

    [Fact]
    public async Task DeleteItem_RemovesItem_ThenReturns404()
    {
        var create = await _client.PostAsJsonAsync("/api/items", new { name = "Doomed", sku = "doomed-001" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var location = create.Headers.Location;

        var delete = await _client.DeleteAsync(location);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync(location)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync(location)).StatusCode);
    }

    [Fact]
    public async Task GetItem_NotFound_Returns404()
    {
        var response = await _client.GetAsync("/api/items/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetItem_NonNumericId_Returns404()
    {
        var response = await _client.GetAsync("/api/items/abc");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateItem_BlankDescription_IsStoredAsNull_AndTimestampIsUtc()
    {
        var response = await _client.PostAsJsonAsync("/api/items", new { name = "Candle", description = "   ", sku = "candle-001" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var json = await _client.GetStringAsync(response.Headers.Location);
        var fetched = System.Text.Json.JsonDocument.Parse(json).RootElement;

        Assert.Equal(System.Text.Json.JsonValueKind.Null, fetched.GetProperty("description").ValueKind);
        Assert.EndsWith("Z", fetched.GetProperty("createdAt").GetString());
    }
}
