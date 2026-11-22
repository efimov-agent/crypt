# Stock Tracking Feature - Tasks

## Overview

Small, verifiable tasks for implementing the Stock Tracking feature. Each task should take 15–30 minutes and have a clear, testable outcome. Tasks are written in Kiro task-list format.

---

- [x] 1. Create StockMovement Model
  - Create `src/HauntedInventory.Web/Data/Models/StockMovement.cs` with properties: `Id`, `ItemId`, `Delta`, `Reason`, `Note`, `ResultingQuantity`, `RecordedAt`
  - Add `Item` navigation property to `StockMovement`; add `Movements` collection to `Item`
  - Use `required` keyword for mandatory fields; set `RecordedAt` default to `DateTime.UtcNow`
  - All properties use appropriate nullable reference types
  - _Requirements: Item Model Extensions, StockMovement Model_
  - Verification: `dotnet build` with no warnings

---

- [x] 2. Create EF Core Migration for Stock Tracking
  - Add `Sku` (string, unique, non-null), `Quantity` (int, default 0), `ReorderLevel` (int, default 0) properties to `Item` model
  - Update `ApplicationDbContext.OnModelCreating()`:
    - Set `Sku` as unique index
    - Add CHECK constraints: `Quantity >= 0` and `ReorderLevel >= 0` via `ToTable(t => t.HasCheckConstraint(...))`
    - Add HasMany/WithOne for Item ↔ StockMovements with cascade delete
    - Add index to `StockMovements`: `(ItemId, RecordedAt DESC)` for history queries
  - Run `dotnet ef migrations add AddStockTracking`
  - **Backfill** existing Items: set `Sku = 'ITEM-' || Id` for rows without Sku, then create unique index
  - Run `dotnet ef database update`
  - _Requirements: Item Table Extensions, StockMovement Record, EF Core Migration_
  - Verification: `dotnet build` and `dotnet test` pass, migration applied successfully

---

- [x] 3. Implement StockMovementsService
  - Create `src/HauntedInventory.Web/Features/Items/StockMovementsService.cs`
  - Implement `RecordMovementAsync(int itemId, int delta, string reason, string? note, CancellationToken)`:
    - Validate item exists (throw if not)
    - Validate delta ≠ 0 (throw `ArgumentException` if zero)
    - Validate reason is in ["Received", "Sold", "Damaged", "Adjustment"] (throw if invalid)
    - Validate note ≤ 200 chars (throw if too long)
    - **Atomic transaction**: Execute `ExecuteUpdateAsync` with `WHERE Id = id AND Quantity + delta >= 0`, set `Quantity = Quantity + delta` and `UpdatedAt = UtcNow`
    - If 0 rows affected: either item doesn't exist (404) or insufficient stock (409)
    - Query Item to get updated `Quantity`; insert `StockMovement` with `ResultingQuantity` in same transaction
    - Return created movement
  - Implement `GetMovementsAsync(int itemId, string[]? reasonFilter, int limit, int offset, CancellationToken)`:
    - Validate limit ≤ 1000, default 100
    - Return movements ordered by RecordedAt descending
    - Apply reason filter and pagination
    - Return empty list if no movements exist
  - Add logging: `ILogger<StockMovementsService>` for successful ops and validation failures
  - _Requirements: StockMovementsService, Concurrency_
  - Verification: `dotnet build` with no warnings

---

- [x] 4. Create Request/Response DTOs
  - Create `CreateMovementRequest` with properties: `Delta` (int), `Reason` (string), `Note` (string?)
    - Add `Validate()` returning `Dictionary<string, string[]>`; check delta ≠ 0, valid reason, note ≤ 200 chars
  - Create `StockMovementDto`: `Id`, `ItemId`, `Delta`, `Reason`, `Note`, `ResultingQuantity`, `RecordedAt`
  - Create `GetMovementsResponse`: `Movements` (list), `Total` (int), `Limit` (int), `Offset` (int)
  - Extend `CreateItemRequest` with `Sku` (string, required) and `ReorderLevel` (int, default 0)
    - Add SKU validation: normalize (Trim + ToUpperInvariant), then check 3–32 chars, uppercase alphanumeric + dash
  - _Requirements: Request/Response DTOs, Reason (store as string)_
  - Verification: `dotnet build` with no warnings

---

- [x] 5. Add StockMovements Endpoints
  - Add to `ItemsEndpoints.cs`: `MapPost("/{id:int}/movements", RecordMovement)` and `MapGet("/{id:int}/movements", GetMovements)`
  - Implement `RecordMovement(int id, CreateMovementRequest req, StockMovementsService svc, ILogger<...> log, CancellationToken ct)`:
    - Validate request; return 400 if validation errors
    - Call `svc.RecordMovementAsync(...)`; catch `ArgumentException` → 400, `InvalidOperationException` → 409 with detail
    - Return 201 Created with movement DTO
  - Implement `GetMovements(int id, string? reason, int limit, int offset, StockMovementsService svc, ILogger<...> log, CancellationToken ct)`:
    - Parse `reason` query parameter (comma-separated) into string[]
    - Call `svc.GetMovementsAsync(...)`
    - Return 200 with `GetMovementsResponse`
  - Register endpoints in `MapItemsEndpoints()`
  - _Requirements: Movement Recording Endpoint, Movement History Endpoint, Concurrency_
  - Verification: `dotnet build` and `dotnet test` pass

---

- [x] 6. Register StockMovementsService in DI Container
  - Update `Program.cs`: add `builder.Services.AddScoped<StockMovementsService>();` before `app.Build()`
  - _Requirements: (DI setup)_
  - Verification: `dotnet run --project src/HauntedInventory.Web` starts without errors

---

- [x] 7. Add Unit Tests for StockMovementsService
  - Create `tests/HauntedInventory.Tests/Features/StockMovementsTests.cs` using `TestDatabaseFixture`
  - Write tests covering:
    - **RecordMovement - Quantity Constraint**: negative result throws `InvalidOperationException`; valid movement updates quantity
    - **RecordMovement - Delta Validation**: delta=0 throws `ArgumentException`; non-zero accepted
    - **RecordMovement - Reason Validation**: invalid reason throws; valid reason recorded
    - **RecordMovement - Note Validation**: note>200 chars throws; valid note recorded
    - **GetMovements - Ordering**: returns descending by RecordedAt
    - **GetMovements - Reason Filtering**: filters correctly by reason
    - **GetMovements - Pagination**: limit/offset works correctly
  - Use Arrange-Act-Assert pattern; test names describe scenario
  - _Requirements: Concurrency (integration test: parallel movements never drive Quantity below 0)_
  - Verification: `dotnet test --filter "StockMovementsTests"` passes

---

- [x] 8. Add Integration Tests for Movements Endpoints
  - Add to `tests/HauntedInventory.Tests/Features/StockMovementsTests.cs` or separate `MovementsEndpointsTests.cs`
  - Use `WebApplicationFactory` and `TestDatabaseFixture` pattern (reference `ItemsTests.cs`)
  - Write integration tests:
    - **POST /api/items/{id}/movements**: valid request → 201; item not found → 404; delta=0 → 400; invalid reason → 400; negative quantity → 409; quantity updated on item
    - **GET /api/items/{id}/movements**: returns 200 ordered descending; `reason` filter works; `limit`/`offset` pagination works; item not found → 404; empty list if no movements
  - **Concurrency Test**: spin up many parallel negative movements, verify Quantity never goes below 0
  - Set up test items with Sku, Quantity, ReorderLevel before each test
  - _Requirements: Movement Recording Endpoint, Movement History Endpoint, Concurrency_
  - Verification: `dotnet test --filter "StockMovementsTests"` and `dotnet test --filter "MovementsEndpointsTests"` pass

---

- [x] 9. Update POST /api/items for Stock Attributes
  - Extend `CreateItemRequest`: add `Sku` (required, normalized with Trim+ToUpperInvariant before validation) and `ReorderLevel` (optional, default 0)
  - Update `ItemsEndpoints` POST handler:
    - Validate SKU format (3–32 chars, uppercase alphanumeric + dash)
    - **Check if SKU exists before insert** (throw if exists)
    - Also catch `DbUpdateException` from unique-index constraint → return 409 with detail "SKU already in use"
    - Set new item `Quantity = 0` (stock only changes through movements)
    - Return 201 with item DTO
  - **Update existing ItemsTests**: Remove/update tests that POST items without Sku field
  - _Requirements: POST /api/items (Create Item), SKU Management_
  - Verification: `dotnet test` passes; existing tests updated

---

- [x] 10. Create Items List Blazor Page
  - Create `src/HauntedInventory.Web/Components/Pages/Items.razor` with `@page "/items"`
  - Add markup:
    - Page heading: "Stock Inventory" (spooky variant acceptable)
    - Filter form (plain HTML): checkbox `lowStock`, label "Show only low stock items", submit button "Filter", antiforgery token
    - Items table: Name | SKU | Quantity | Reorder Level | Status | Actions
    - Display "Low Stock" badge (spooky OK) if Quantity ≤ ReorderLevel; link to `/items/{id}`
  - Code-behind: Fetch items from `GET /api/items?lowStock={filter}`; parse `lowStock` query param; handle loading/error states
  - Styling: dark theme (#0f0f1a background, #eaeaea text, #e94560 accent badge); WCAG AA contrast; focus states 2px outline
  - _Requirements: Item List Filtering, Blazor Pages, UI Theme_
  - Verification: `dotnet build`; navigate to `/items` in browser, see items table, toggle filter

---

- [x] 11. Create Item Details Blazor Page
  - Create `src/HauntedInventory.Web/Components/Pages/ItemDetails.razor` with `@page "/items/{Id:int}"`
  - Add markup:
    - Item header: Name, SKU, Quantity (highlight if low stock), Reorder Level, Description
    - Movement history section: Table (Date | Reason | Delta | Note | Resulting Qty), ordered descending; empty state "No movements recorded yet"
    - Record movement form (plain HTML): input `delta` (number, required, non-zero), select `reason` (Received|Sold|Damaged|Adjustment), textarea `note` (optional, max 200 chars), submit button "Record Movement", antiforgery token
    - Error messages (validation, conflict, 404); success message after movement recorded
  - Code-behind: Fetch item from `/api/items/{id}`; fetch movements from `/api/items/{id}/movements` (ordered descending, limit 100); handle 409 conflict messages
  - Styling: dark theme; form labels bold + associated with inputs; input focus 2px outline #e94560; error text #ff6b6b (WCAG AA); success text #51cf66; table subtle borders
  - _Requirements: Blazor Pages, Movement History Endpoint (ordered newest first, limit 100), UI Theme_
  - Verification: `dotnet build`; navigate to `/items/1`, see details, record movement, see it appear in history

---

- [x] 12. Update Navigation and Final Verification
  - Update `src/HauntedInventory.Web/Components/Layout/MainLayout.razor`: add link to `/items`, label "Stock Inventory"
  - Ensure navigation styling consistent; focus states visible
  - Run final verification:
    - `dotnet build` → no warnings
    - `dotnet test -v minimal` → all tests pass
    - `dotnet run --project src/HauntedInventory.Web` → starts without errors
    - Manual: navigate `/items`, see items; `/items/1`, see details; record movement; see history
  - _Requirements: (all previous)_
  - Verification: build clean, all tests pass, manual UI verification complete

---

## Task Dependencies

Task 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 (foundation tasks run in sequence)
Task 9 (extends POST /api/items; depends on foundation complete)
Task 10 ← Task 5 (needs movement endpoints)
Task 11 ← Task 10 (depends on list page and movement endpoints)
Task 12 ← All (final verification)
