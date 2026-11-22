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
    public async Task GetMovements_Ordering_ReturnsNewestFirst()
    {
        var item = await CreateItemAsync("ORDER-001");
        await RecordAsync(item.Id, 1, "Received");
        await RecordAsync(item.Id, 1, "Received");
        await RecordAsync(item.Id, 1, "Received");

        var result = await _client.GetFromJsonAsync<GetMovementsResponse>($"/api/items/{item.Id}/movements");

        Assert.NotNull(result);
        Assert.Equal([3, 2, 1], result.Movements.Select(m => m.ResultingQuantity));
    }

    [Fact]
    public async Task GetMovements_ReasonFiltering_SingleAndMultipleReasons()
    {
        var item = await CreateItemAsync("FILTER-001");
        await RecordAsync(item.Id, 5, "Received");     // qty 5
        await RecordAsync(item.Id, -1, "Sold");        // qty 4
        await RecordAsync(item.Id, -1, "Damaged");     // qty 3
        await RecordAsync(item.Id, 2, "Adjustment");   // qty 5
        await RecordAsync(item.Id, -2, "Sold");        // qty 3

        var sold = await _client.GetFromJsonAsync<GetMovementsResponse>($"/api/items/{item.Id}/movements?reason=Sold");
        Assert.NotNull(sold);
        Assert.Equal([3, 4], sold.Movements.Select(m => m.ResultingQuantity));
        Assert.Equal(2, sold.Total);

        var soldOrDamaged = await _client.GetFromJsonAsync<GetMovementsResponse>($"/api/items/{item.Id}/movements?reason=Sold,Damaged");
        Assert.NotNull(soldOrDamaged);
        Assert.Equal(["Sold", "Damaged", "Sold"], soldOrDamaged.Movements.Select(m => m.Reason));
        Assert.Equal(3, soldOrDamaged.Total);
    }

    [Fact]
    public async Task GetMovements_UnknownReasonFilter_Returns400()
    {
        var item = await CreateItemAsync("FILTER-002");
        await RecordAsync(item.Id, 5, "Received");

        var response = await _client.GetAsync($"/api/items/{item.Id}/movements?reason=Sodl");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetMovements_Pagination_ReturnsRequestedSliceAndTotal()
    {
        var item = await CreateItemAsync("PAGE-001");
        for (var i = 0; i < 150; i++)
        {
            await RecordAsync(item.Id, 1, "Received"); // ResultingQuantity 1..150
        }

        var page = await _client.GetFromJsonAsync<GetMovementsResponse>($"/api/items/{item.Id}/movements?limit=50&offset=100");

        Assert.NotNull(page);
        Assert.Equal(Enumerable.Range(1, 50).Reverse(), page.Movements.Select(m => m.ResultingQuantity));
        Assert.Equal(150, page.Total);
        Assert.Equal(50, page.Limit);
        Assert.Equal(100, page.Offset);
    }

    [Fact]
    public async Task GetMovements_LimitAboveMaximum_IsClampedAndReported()
    {
        var item = await CreateItemAsync("PAGE-002");
        await RecordAsync(item.Id, 1, "Received");

        var page = await _client.GetFromJsonAsync<GetMovementsResponse>($"/api/items/{item.Id}/movements?limit=5000");

        Assert.NotNull(page);
        Assert.Equal(StockMovementsService.MaxPageSize, page.Limit);
        Assert.Single(page.Movements);
    }

    [Fact]
    public async Task PostMovement_DeltaOutOfRange_Returns400()
    {
        var item = await CreateItemAsync("BIG-001");

        var response = await _client.PostAsJsonAsync($"/api/items/{item.Id}/movements",
            new { delta = int.MaxValue, reason = "Received" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetItems_LowStockFilter_ReturnsOnlyItemsAtOrBelowReorderLevel()
    {
        // Seeded: TEST-001 (10 / reorder 5) is not low; TEST-002 (2 / reorder 3) is low.
        var items = await _client.GetFromJsonAsync<List<ItemDto>>("/api/items?lowStock=true");

        Assert.NotNull(items);
        Assert.Contains(items, i => i.Sku == "TEST-002");
        Assert.DoesNotContain(items, i => i.Sku == "TEST-001");
        Assert.All(items, i => Assert.True(i.Quantity <= i.ReorderLevel));
    }

    private async Task<ItemDto> CreateItemAsync(string sku)
    {
        var response = await _client.PostAsJsonAsync("/api/items", new { name = $"Item {sku}", sku, reorderLevel = 0 });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var item = await response.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(item);
        return item;
    }

    private async Task RecordAsync(int itemId, int delta, string reason)
    {
        var response = await _client.PostAsJsonAsync($"/api/items/{itemId}/movements", new { delta, reason });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
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