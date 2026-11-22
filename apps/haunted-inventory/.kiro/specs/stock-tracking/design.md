# Stock Tracking Feature - Design

## Architecture Overview

The Stock Tracking feature extends HauntedInventory with inventory management capabilities. It follows the existing feature-based architecture:

1. **Data Layer**: Item model extensions + StockMovements table in EF Core
2. **Service Layer**: StockMovementsService for business logic
3. **API Layer**: Minimal API endpoints under `/api/items/{id}/movements`
4. **UI Layer**: Blazor static SSR pages (item list, item details)

## Data Model Design

### Item Model Extension

The existing `Item` class gains three new properties:

```csharp
public class Item
{
    // Existing fields
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // New stock tracking fields
    public required string Sku { get; set; }  // Unique, 3-32 chars, uppercase alphanumeric + dash
    public int Quantity { get; set; } = 0;   // Current stock level, CHECK Quantity >= 0
    public int ReorderLevel { get; set; } = 0;  // Reorder threshold, CHECK ReorderLevel >= 0

    // Navigation property
    public ICollection<StockMovement> Movements { get; set; } = [];
}
```

**Constraints**:
- `Sku`: Unique index, non-null, 3-32 character length validated in service, normalized (Trim + ToUpperInvariant) before validation
- `Quantity`: Non-negative (database CHECK constraint)
- `ReorderLevel`: Non-negative (database CHECK constraint)

### StockMovement Model

New table to track inventory changes:

```csharp
public class StockMovement
{
    public int Id { get; set; }

    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;

    public required int Delta { get; set; }  // Non-zero signed integer

    public required string Reason { get; set; }  // Enum: "Received", "Sold", "Damaged", "Adjustment"

    public string? Note { get; set; }  // Optional, max 200 chars

    public int ResultingQuantity { get; set; }  // Quantity after movement

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
```

**Design Decisions**:
- `Reason` stored as string (can be mapped to enum in DTOs)
- `ResultingQuantity` is denormalized for query efficiency (avoid N+1 when fetching history)
- `RecordedAt` is set at record creation; not modifiable
- Foreign key cascade: deleting an item deletes its movements

## Service Layer

### StockMovementsService

Centralized business logic for stock operations:

```csharp
public class StockMovementsService
{
    public async Task<StockMovement> RecordMovementAsync(
        int itemId,
        int delta,
        string reason,
        string? note,
        CancellationToken cancellationToken);

    public async Task<IReadOnlyList<StockMovement>> GetMovementsAsync(
        int itemId,
        string[]? reasonFilter = null,
        int limit = 100,
        int offset = 0,
        CancellationToken cancellationToken = default);

    public async Task<IReadOnlyList<Item>> GetLowStockItemsAsync(
        CancellationToken cancellationToken);
}
```

**Responsibilities**:
- Validate SKU format (3-32 chars, alphanumeric + dash, uppercase), with normalization (Trim + ToUpperInvariant)
- Enforce quantity constraints (non-negative, delta non-zero)
- Record movements atomically via transaction:
  1. `ExecuteUpdateAsync` with compound WHERE: `WHERE Id = id AND Quantity + delta >= 0`
  2. Set `Quantity = Quantity + delta` and `UpdatedAt = UtcNow`
  3. If 0 rows affected, item doesn't exist → 404; if constraint failed, insufficient stock → 409
  4. Insert `StockMovement` with `ResultingQuantity` read back from Item in same transaction
- Query movements with optional filtering, ordered newest first, default limit 100
- Identify low-stock items (Quantity <= ReorderLevel)

**Key Invariants**:
- If a movement would result in negative quantity, throw `InvalidOperationException` → HTTP 409
- Delta must be non-zero; throw `ArgumentException` → HTTP 400
- Reason must be a valid enum value
- Note must not exceed 200 characters
- SKU must be 3-32 chars, alphanumeric + dash, after normalization

### ItemsService Extension

Existing `ItemsService` gains methods for item creation with stock attributes:

```csharp
public async Task<Item> CreateItemWithStockAsync(
    string name,
    string? description,
    string sku,
    int reorderLevel = 0,
    CancellationToken cancellationToken = default);
```

Also add method to fetch items by low-stock status:

```csharp
public async Task<IReadOnlyList<Item>> GetItemsAsync(
    bool lowStockOnly = false,
    CancellationToken cancellationToken = default);
```

**CreateItemWithStockAsync**:
- Normalizes `sku` with `Trim()` + `ToUpperInvariant()` before validation
- Validates SKU format (3-32 chars, uppercase alphanumeric + dash)
- Checks if SKU already exists before insert; throws if duplicate
- Also catches `DbUpdateException` from unique-index constraint violation
- Sets `Quantity` to 0 (stock only changes through movements)
- Sets `ReorderLevel` to provided value (default 0)

## API Endpoints

### POST /api/items/{id}/movements

**Request**:
```json
{
  "delta": -1,
  "reason": "Sold",
  "note": "Halloween party rental"
}
```

**Validation**:
1. Item exists (404 if not)
2. Delta ≠ 0 (400 if zero)
3. Reason is valid enum (400 if invalid)
4. Note is ≤ 200 chars (400 if too long)
5. Resulting quantity ≥ 0 (409 if negative)

**Concurrency-Safe Atomic Update** (inside transaction):
1. Execute `ExecuteUpdateAsync` with compound WHERE: `WHERE Id = id AND Quantity + delta >= 0`
2. Set `Quantity = Quantity + delta` and `UpdatedAt = UtcNow`
3. Check rows affected:
   - 0 rows: Either item doesn't exist (404) or constraint failed—insufficient stock (409)
4. Query Item to get updated `Quantity`
5. Create `StockMovement` record with `ResultingQuantity = updatedQuantity`

**Response on Success** (201 Created):
```json
{
  "id": 123,
  "itemId": 42,
  "delta": -1,
  "reason": "Sold",
  "note": "Halloween party rental",
  "resultingQuantity": 4,
  "recordedAt": "2024-11-23T15:30:00Z"
}
```

**Response on Conflict** (409):
```json
{
  "type": "about:blank",
  "title": "Conflict",
  "status": 409,
  "detail": "Stock movement would result in negative quantity. Current: 5, Delta: -10"
}
```

### GET /api/items/{id}/movements

**Query Parameters**:
- `reason` (optional): Comma-separated enum values (e.g., `Sold,Damaged`)
- `limit` (optional, default 100, max 1000): Pagination size
- `offset` (optional, default 0): Pagination offset

**Response** (200 OK):
```json
{
  "movements": [
    {
      "id": 123,
      "itemId": 42,
      "delta": -1,
      "reason": "Sold",
      "note": "Halloween party rental",
      "resultingQuantity": 4,
      "recordedAt": "2024-11-23T15:30:00Z"
    }
  ],
  "total": 15,
  "limit": 100,
  "offset": 0
}
```

**Ordering**: Descending by `RecordedAt` (newest first)

**Empty**: Returns `[]` if no movements exist

### GET /api/items (extended)

**Query Parameters**:
- `lowStock` (optional, default false): If true, return only items with Quantity <= ReorderLevel

**Response** (200 OK):
```json
[
  {
    "id": 1,
    "name": "Vampire Cape",
    "description": "Classic black velvet cape",
    "sku": "CAPE-BLK-001",
    "quantity": 2,
    "reorderLevel": 5,
    "createdAt": "2024-11-20T10:00:00Z",
    "updatedAt": "2024-11-23T15:30:00Z"
  }
]
```

### POST /api/items (Create Item)

**Request**:
```json
{
  "name": "Vampire Cape",
  "description": "Classic black velvet",
  "sku": "cape-blk-001",
  "reorderLevel": 5
}
```

**Validation**:
1. `name` required, non-empty (400 if missing)
2. `sku` required, normalized (Trim + ToUpperInvariant), then validated: 3–32 chars, uppercase alphanumeric + dash (400 if invalid)
3. `reorderLevel` optional, defaults to 0, must be >= 0 (400 if invalid)
4. `description` optional
5. Check if SKU already exists before insert (409 if duplicate); also catch `DbUpdateException` from unique-index constraint

**Success Response** (201 Created):
```json
{
  "id": 1,
  "name": "Vampire Cape",
  "description": "Classic black velvet cape",
  "sku": "CAPE-BLK-001",
  "quantity": 0,
  "reorderLevel": 5,
  "createdAt": "2024-11-20T10:00:00Z",
  "updatedAt": null
}
```

**New Item Quantity**: Always 0 (stock only changes through movements)

## Blazor Pages

### /Items (List Page)

**Component Structure**:
```
Items.razor
├── @page "/items"
├── Item Filter Section (form)
│   └── Checkbox: Show only low stock items
├── Items Table
│   ├── Columns: Name | SKU | Quantity | Reorder Level | Status | Actions
│   ├── Status Badge: "Low Stock" (spooky styling)
│   └── Action: Link to details page
└── Error section (if API fails)
```

**Form**:
- Plain HTML `<form method="get" action="/items">` with query parameter `lowStock`
- Checkbox with label
- Submit button labeled "Filter"
- Antiforgery token (via `@Html.AntiForgeryToken()`)

**Styling**:
- Dark theme: background #0f0f1a, text #eaeaea
- Badge: Accent color #e94560 for low stock
- Table: Border color #1a1a2e (subtle)
- Focus states: 2px outline, accent color

### /Items/{id} (Details Page)

**Component Structure**:
```
ItemDetails.razor
├── @page "/items/{id}"
├── Item Header
│   ├── Name | SKU
│   ├── Quantity: X (highlight if low stock)
│   ├── Reorder Level: Y
│   └── Description
├── Movements History Section
│   └── Table
│       ├── Columns: Date | Reason | Delta | Note | Resulting Qty
│       ├── Rows: Each movement
│       └── Empty state: "No movements recorded"
├── Record Movement Form
│   ├── Delta (numeric input, required, non-zero)
│   ├── Reason (dropdown or radio buttons)
│   ├── Note (textarea, optional, max 200 chars)
│   ├── Submit button: "Record Movement"
│   └── Validation errors (displayed inline)
└── Error section (conflict, validation, 404)
```

**Form**:
- Plain HTML `<form method="post" action="/api/items/{id}/movements">`
- Inputs:
  - `delta`: type="number", required, label "Quantity Change"
  - `reason`: select or radio buttons, options: Received, Sold, Damaged, Adjustment, label "Reason"
  - `note`: textarea, optional, max 200, label "Notes"
- Antiforgery token (via `@Html.AntiForgeryToken()`)
- Submit button with text "Record Movement"

**Error Handling**:
- **Conflict (409)**: Display message: "Cannot record movement. This would make quantity negative. Current: X, Attempted change: Y"
- **Validation (400)**: Display per-field error messages
- **Not Found (404)**: Display "Item not found"

**Styling**:
- Dark theme: background #0f0f1a, text #eaeaea
- Form labels: Bold, associated with inputs via `for` attribute
- Input focus: 2px outline, accent color #e94560
- Buttons: Accent background, dark text, hover darker shade
- Error text: Color #ff6b6b (red, WCAG AA contrast)
- Success message: Green (#51cf66, with WCAG AA contrast)

## EF Core Migration

**Migration Name**: `AddStockTracking`

**Changes**:
1. Add columns to `Items` table:
   - `Sku` (nvarchar(32), unique, not null)
   - `Quantity` (int, not null, default 0)
   - `ReorderLevel` (int, not null, default 0)

2. **Backfill existing Items**:
   - For rows without Sku, set `Sku = 'ITEM-' || Id` (e.g., 'ITEM-1', 'ITEM-2')
   - Set default `Quantity = 0`
   - Set default `ReorderLevel = 0`

3. Create unique index on `Items.Sku` **after backfill**

4. Add CHECK constraints to `Items`:
   - `CHECK (Quantity >= 0)`
   - `CHECK (ReorderLevel >= 0)`

5. Create `StockMovements` table:
   - `Id` (int, primary key, auto-increment)
   - `ItemId` (int, foreign key, not null)
   - `Delta` (int, not null)
   - `Reason` (nvarchar(50), not null, stored as string)
   - `Note` (nvarchar(200), nullable)
   - `ResultingQuantity` (int, not null)
   - `RecordedAt` (datetime2, not null, default GetUtcDate())

6. Create indexes:
   - `Items.Sku` (unique)
   - `StockMovements.(ItemId, RecordedAt DESC)` (history queries)
   - Foreign key: `StockMovements.ItemId` → `Items.Id` with cascade delete

## Request/Response DTOs

### CreateMovementRequest
```csharp
public class CreateMovementRequest
{
    public required int Delta { get; set; }
    public required string Reason { get; set; }
    public string? Note { get; set; }

    public Dictionary<string, string[]> Validate() { /* ... */ }
}
```

### StockMovementDto
```csharp
public class StockMovementDto
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public int Delta { get; set; }
    public string Reason { get; set; } = "";
    public string? Note { get; set; }
    public int ResultingQuantity { get; set; }
    public DateTime RecordedAt { get; set; }
}
```

### ItemDto (extended)
```csharp
public class ItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Sku { get; set; } = "";
    public int Quantity { get; set; }
    public int ReorderLevel { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

## Key Design Decisions

1. **Denormalized `ResultingQuantity`**: Avoids N+1 query problem when fetching movement history with quantities.

2. **Enum as String in DB**: Stored as string for readability and schema flexibility; mapped to enum in service/API layer.

3. **Atomic Movement Recording**: Single transaction updates Item.Quantity and creates StockMovement record; ensures consistency.

4. **Low Stock Query**: Computed via LINQ (not a flag column); reorder level is the source of truth.

5. **Blazor Static SSR**: No interactivity; forms submit via POST to trigger page refresh. Keeps implementation simple and performant.

6. **Plain HTML Forms**: No JavaScript; standard form submission with antiforgery tokens. Aligns with Blazor static SSR model.

7. **409 Conflict for Negative Quantity**: Clear HTTP semantics; client can retry with valid delta or display error.

## Testing Strategy

### Unit Tests (xUnit)
- SKU validation (format, uniqueness)
- Quantity constraint enforcement
- Delta validation (non-zero)
- Low stock filtering logic

### Integration Tests (WebApplicationFactory)
- API endpoints: POST, GET movements
- API endpoint: GET /api/items?lowStock=true
- Database transactions (atomic movement + quantity update)
- EF Core migration execution

### Manual/E2E Tests (Out of Scope)
- Blazor pages (UI behavior, form submission)
- Playwright e2e tests (separate task)

## Error Handling

| Scenario | Status | Response |
|----------|--------|----------|
| Item not found | 404 | `{ "message": "Item not found" }` |
| Delta is 0 | 400 | `ValidationProblem` with field error |
| Reason invalid | 400 | `ValidationProblem` with field error |
| Note > 200 chars | 400 | `ValidationProblem` with field error |
| Negative quantity result | 409 | `ProblemDetails` with detail explaining conflict |
| SKU not unique | 409 | `ProblemDetails` with detail: "SKU already in use" |
| SKU format invalid | 400 | `ValidationProblem` with SKU field error |
| Unknown reason value | 400 | `ValidationProblem` with field error |

## Logging

- **Information**: Movement recorded (itemId, delta, reason, resulting quantity)
- **Information**: Low stock query executed (count of results)
- **Warning**: Attempted movement rejected (itemId, reason for rejection)
- **Error**: Database errors (transaction failure, constraint violations)
