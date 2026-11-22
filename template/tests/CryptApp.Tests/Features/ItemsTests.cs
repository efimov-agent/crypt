using CryptApp.Tests.Fixtures;
using Xunit;

namespace CryptApp.Tests.Features;

[Collection("Integration tests")]
public class ItemsTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public ItemsTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsOk()
    {
        // Arrange
        var client = _fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("timestamp", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetItems_ReturnsItems()
    {
        // Arrange
        var client = _fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/items");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Test Item", content);
    }

    [Fact]
    public async Task CreateItem_ReturnsCreated()
    {
        // Arrange
        var client = _fixture.CreateClient();
        var newItem = new 
        {
            name = "New Test Item",
            description = "Created by integration test"
        };
        var jsonContent = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(newItem),
            System.Text.Encoding.UTF8,
            "application/json");

        // Act
        var response = await client.PostAsync("/api/items", jsonContent);

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetItem_NotFound_Returns404()
    {
        // Arrange
        var client = _fixture.CreateClient();

        // Act
        var response = await client.GetAsync("/api/items/999999");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}