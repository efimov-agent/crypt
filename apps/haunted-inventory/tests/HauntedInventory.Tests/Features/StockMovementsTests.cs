using System.Net;
using System.Net.Http.Json;
using HauntedInventory.Tests.Fixtures;
using HauntedInventory.Web.Data.Models;
using HauntedInventory.Web.Features.Items;
using Xunit;

namespace HauntedInventory.Tests.Features;

[Collection("Integration tests")]
public class StockMovementsTests : IClassFixture<TestDatabaseFixture>
{
    private readonly HttpClient _client;

    public StockMovementsTests(TestDatabaseFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    #region Task 7: StockMovementsService Unit Tests (via Integration)

    [Fact]
    public async Task RecordMovement_QuantityConstraint_NegativeResult_ThrowsInvalidOperationException()
    {
        // Arrange: Create an item with Quantity = 2
        var createResponse = await _client.PostAsJsonAsync("/api/items", new
        {
            name = "Constraint Test Item",
            sku = "CONST-001",
            reorderLevel = 1
        });
        var item = await createResponse.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(item);

        // Record a movement that would make quantity negative
        var response = await _client.PostAsJsonAsync($"/api/items/{item.Id}/movements", new
        {
            delta = -5, // Item has quantity 0, so this would result in -5
            reason = "Sold"
        });

        // Assert: 409 Conflict
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("negative quantity", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecordMovement_DeltaZero_Returns400()
    {
        // Arrange: Get the seeded item
        var items = await _client.GetFromJsonAsync<List<ItemDto>>("/api/items");
        var item = items?.FirstOrDefault(i => i.Sku == "TEST-001");
        Assert.NotNull(item);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/items/{item.Id}/movements", new
        {
            delta = 0,
            reason = "Sold"
        });

        // Assert: 400 Bad Request
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecordMovement_InvalidReason_Returns400()
    {
        // Arrange: Get the seeded item
        var items = await _client.GetFromJsonAsync<List<ItemDto>>("/api/items");
        var item = items?.FirstOrDefault(i => i.Sku == "TEST-001");
        Assert.NotNull(item);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/items/{item.Id}/movements", new
        {
            delta = 1,
            reason = "InvalidReason"
        });

        // Assert: 400 Bad Request
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecordMovement_NoteTooLong_Returns400()
    {
        // Arrange: Get the seeded item
        var items = await _client.GetFromJsonAsync<List<ItemDto>>("/api/items");
        var item = items?.FirstOrDefault(i => i.Sku == "TEST-001");
        Assert.NotNull(item);

        // Act: Note > 200 chars
        var longNote = new string('x', 201);
        var response = await _client.PostAsJsonAsync($"/api/items/{item.Id}/movements", new
        {
            delta = 1,
            reason = "Received",
            note = longNote
        });

        // Assert: 400 Bad Request
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecordMovement_ValidMovement_UpdatesQuantity()
    {
        // Arrange: Create an item
        var createResponse = await _client.PostAsJsonAsync("/api/items", new
        {
            name = "Quantity Update Test",
            sku = "QTY-001",
            reorderLevel = 0
        });
        var createdItem = await createResponse.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(createdItem);

        // Act: Record a positive movement
        var response = await _client.PostAsJsonAsync($"/api/items/{createdItem.Id}/movements", new
        {
            delta = 10,
            reason = "Received"
        });

        // Assert: 201 Created
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var movement = await response.Content.ReadFromJsonAsync<StockMovementDto>();
        Assert.NotNull(movement);
        Assert.Equal(10, movement.ResultingQuantity);

        // Verify the item quantity was updated
        var updatedItem = await _client.GetFromJsonAsync<ItemDto>($"/api/items/{createdItem.Id}");
        Assert.NotNull(updatedItem);
        Assert.Equal(10, updatedItem.Quantity);
    }

    [Fact]
    public async Task GetMovements_Ordering_ReturnsDescendingByRecordedAt()
    {
        // Arrange: Create an item and record multiple movements
        var createResponse = await _client.PostAsJsonAsync("/api/items", new
        {
            name = "Ordering Test",
            sku = "ORDER-001",
            reorderLevel = 0
        });
        var item = await createResponse.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(item);

        // Record movements with slight delay to ensure different timestamps
        await _client.PostAsJsonAsync($"/api/items/{item.Id}/movements", new { delta = 1, reason = "Received" });
        await Task.Delay(10);
        await _client.PostAsJsonAsync($"/api/items/{item.Id}/movements", new { delta = 1, reason = "Received" });
        await Task.Delay(10);
        await _client.PostAsJsonAsync($"/api/items/{item.Id}/movements", new { delta = 1, reason = "Received" });

        // Act
        var response = await _client.GetAsync($"/api/items/{item.Id}/movements");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetMovementsResponse>();
        Assert.NotNull(result);
        Assert.Equal(3, result.Movements.Count);

        // Assert: Ordered descending (newest first)
        for (int i = 0; i < result.Movements.Count - 1; i++)
        {
            Assert.True(result.Movements[i].RecordedAt >= result.Movements[i + 1].RecordedAt);
        }
    }

    [Fact]
    public async Task GetMovements_ReasonFiltering_FiltersCorrectly()
    {
        // Arrange: Create an item and record movements with different reasons
        var createResponse = await _client.PostAsJsonAsync("/api/items", new
        {
            name = "Filter Test",
            sku = "FILTER-001",
            reorderLevel = 0
        });
        var item = await createResponse.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(item);

        // Add some initial stock
        await _client.PostAsJsonAsync($"/api/items/{item.Id}/movements", new { delta = 5, reason = "Received" });

        // Record movements with different reasons
        await _client.PostAsJsonAsync($"/api/items/{item.Id}/movements", new { delta = 1, reason = "Received" });
        await _client.PostAsJsonAsync($"/api/items/{item.Id}/movements", new { delta = -1, reason = "Sold" });
        await _client.PostAsJsonAsync($"/api/items/{item.Id}/movements", new { delta = -1, reason = "Sold" });

        // Act: Filter by "Sold"
        var response = await _client.GetAsync($"/api/items/{item.Id}/movements?reason=Sold");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetMovementsResponse>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Movements.Count);
        Assert.All(result.Movements, m => Assert.Equal("Sold", m.Reason));
    }

    [Fact]
    public async Task GetMovements_Pagination_LimitOffset_WorksCorrectly()
    {
        // Arrange: Create an item and record 5 movements
        var createResponse = await _client.PostAsJsonAsync("/api/items", new
        {
            name = "Pagination Test",
            sku = "PAGE-001",
            reorderLevel = 0
        });
        var item = await createResponse.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(item);

        // Record 5 movements
        for (int i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync($"/api/items/{item.Id}/movements", new { delta = 1, reason = "Received" });
        }

        // Act: Get with limit=2, offset=1
        var response = await _client.GetAsync($"/api/items/{item.Id}/movements?limit=2&offset=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<GetMovementsResponse>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Movements.Count);
        Assert.Equal(2, result.Limit);
        Assert.Equal(1, result.Offset);
    }

    [Fact]
    public async Task GetMovements_ItemNotFound_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/api/items/999999/movements");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMovements_EmptyList_ReturnsEmptyArray()
    {
        // Arrange: Create an item with no movements
        var createResponse = await _client.PostAsJsonAsync("/api/items", new
        {
            name = "Empty Test",
            sku = "EMPTY-001",
            reorderLevel = 0
        });
        var item = await createResponse.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(item);

        // Act
        var response = await _client.GetAsync($"/api/items/{item.Id}/movements");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<GetMovementsResponse>();
        Assert.NotNull(result);
        Assert.Empty(result.Movements);
    }

    #endregion

    #region Task 8: Integration Tests for Movements Endpoints + Concurrency

    [Fact]
    public async Task PostMovement_ValidRequest_Returns201()
    {
        // Arrange: Create an item
        var createResponse = await _client.PostAsJsonAsync("/api/items", new
        {
            name = "Valid Request Test",
            sku = "VALID-001",
            reorderLevel = 0
        });
        var item = await createResponse.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(item);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/items/{item.Id}/movements", new
        {
            delta = 5,
            reason = "Received",
            note = "Test note"
        });

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var movement = await response.Content.ReadFromJsonAsync<StockMovementDto>();
        Assert.NotNull(movement);
        Assert.Equal(5, movement.Delta);
        Assert.Equal("Received", movement.Reason);
        Assert.Equal("Test note", movement.Note);
    }

    [Fact]
    public async Task PostMovement_ItemNotFound_Returns404()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/items/999999/movements", new
        {
            delta = 1,
            reason = "Received"
        });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostMovement_NegativeQuantity_Returns409()
    {
        // Arrange: Create an item (quantity starts at 0)
        var createResponse = await _client.PostAsJsonAsync("/api/items", new
        {
            name = "Negative Test",
            sku = "NEG-001",
            reorderLevel = 0
        });
        var item = await createResponse.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(item);

        // Act: Try to reduce by more than available
        var response = await _client.PostAsJsonAsync($"/api/items/{item.Id}/movements", new
        {
            delta = -1,
            reason = "Sold"
        });

        // Assert: 409 Conflict
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    /// <summary>
    /// Concurrency test: Fire at least 20 parallel POST /api/items/{id}/movements with delta -1 
    /// against an item with Quantity 5 and assert exactly 5 succeed (201), the rest 409, and final Quantity == 0.
    /// </summary>
    [Fact]
    public async Task PostMovement_Concurrency_Max5Succeed_QuantityNeverNegative()
    {
        // Arrange: Create an item with Quantity = 5
        var createResponse = await _client.PostAsJsonAsync("/api/items", new
        {
            name = "Concurrency Test",
            sku = "CONCUR-001",
            reorderLevel = 0
        });
        var item = await createResponse.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(item);

        // Add 5 units to get to quantity 5
        for (int i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync($"/api/items/{item.Id}/movements", new { delta = 1, reason = "Received" });
        }

        // Verify quantity is 5
        var verifyItem = await _client.GetFromJsonAsync<ItemDto>($"/api/items/{item.Id}");
        Assert.NotNull(verifyItem);
        Assert.Equal(5, verifyItem.Quantity);

        // Fire 20 parallel requests with delta -1
        const int parallelRequests = 20;
        var tasks = new List<Task<HttpResponseMessage>>();

        for (int i = 0; i < parallelRequests; i++)
        {
            tasks.Add(_client.PostAsJsonAsync($"/api/items/{item.Id}/movements", new
            {
                delta = -1,
                reason = "Sold"
            }));
        }

        var responses = await Task.WhenAll(tasks);

        // Count successes (201) and conflicts (409)
        int successCount = 0;
        int conflictCount = 0;

        foreach (var response in responses)
        {
            if (response.StatusCode == HttpStatusCode.Created)
                successCount++;
            else if (response.StatusCode == HttpStatusCode.Conflict)
                conflictCount++;
        }

        // Assert: Exactly 5 should succeed (201), the rest (15) should be 409
        Assert.Equal(5, successCount);
        Assert.Equal(parallelRequests - 5, conflictCount);

        // Final quantity should be 0
        var finalItem = await _client.GetFromJsonAsync<ItemDto>($"/api/items/{item.Id}");
        Assert.NotNull(finalItem);
        Assert.Equal(0, finalItem.Quantity);
    }

    #endregion

    #region Task 9: POST /api/items Tests (SKU validation, duplicate handling)

    [Fact]
    public async Task CreateItem_WithValidSku_ReturnsCreated()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/items", new
        {
            name = "Valid SKU Test",
            sku = "valid-sku-001", // Will be normalized to VALID-SKU-001
            description = "Test description",
            reorderLevel = 5
        });

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(item);
        Assert.Equal("VALID-SKU-001", item.Sku);
        Assert.Equal(0, item.Quantity); // New items start at 0
        Assert.Equal(5, item.ReorderLevel);
    }

    [Fact]
    public async Task CreateItem_SkuNormalization_TrimAndUpperInvariant()
    {
        // Act: Submit with lowercase and whitespace
        var response = await _client.PostAsJsonAsync("/api/items", new
        {
            name = "Normalization Test",
            sku = "  normalized-123  ",
            reorderLevel = 0
        });

        // Assert: Normalized to uppercase, trimmed
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(item);
        Assert.Equal("NORMALIZED-123", item.Sku);
    }

    [Theory]
    [InlineData("ab")] // Too short (2 chars)
    [InlineData("ab!cd")] // Invalid character after uppercasing
    [InlineData("ab@cd")] // Invalid character after uppercasing
    [InlineData("ab cd")] // Space character after uppercasing
    [InlineData("abcdefghijklmnopqrstuvwxyz1234567")] // Too long (>32 = 33 chars)
    public async Task CreateItem_InvalidSku_Returns400(string invalidSku)
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/items", new
        {
            name = "Invalid SKU Test",
            sku = invalidSku,
            reorderLevel = 0
        });

        // Assert: 400 Bad Request
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateItem_DuplicateSku_Returns409_PreCheck()
    {
        // Arrange: Create first item
        await _client.PostAsJsonAsync("/api/items", new
        {
            name = "First Item",
            sku = "DUPE-001",
            reorderLevel = 0
        });

        // Act: Try to create second item with same SKU
        var response = await _client.PostAsJsonAsync("/api/items", new
        {
            name = "Second Item",
            sku = "DUPE-001",
            reorderLevel = 0
        });

        // Assert: 409 Conflict (pre-check)
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("SKU already in use", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateItem_ReorderLevel_Negative_Returns400()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/items", new
        {
            name = "Negative Reorder",
            sku = "REORDER-001",
            reorderLevel = -1
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateItem_NewItemQuantity_IsZero()
    {
        // Act: Create item without specifying quantity
        var response = await _client.PostAsJsonAsync("/api/items", new
        {
            name = "Zero Quantity Test",
            sku = "ZEROQTY-001",
            reorderLevel = 5
        });

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(item);
        Assert.Equal(0, item.Quantity);
    }

    #endregion
}