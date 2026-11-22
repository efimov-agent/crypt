# Stock Tracking Feature - Requirements

## Overview

The Stock Tracking feature extends the existing Items feature to transform HauntedInventory into an inventory management system for a small Halloween costume & prop shop. Items gain SKU-based tracking, quantity management, reorder levels, and a full audit trail of stock movements.

## User Stories

### Story 1: Item Catalog with Stock Attributes
**As a** shop manager
**I want** each item to have a unique SKU, current quantity, and a reorder threshold
**So that** I can manage stock levels and know when to reorder

### Story 2: Stock Movements
**As a** shop staff member
**I want** to record stock in/out events (received, sold, damaged, adjustment)
**So that** we have an audit trail and quantity stays accurate

### Story 3: Low Stock Visibility
**As a** shop manager
**I want** to filter items by low stock status
**So that** I can quickly see what needs reordering

### Story 4: Movement History
**As a** an auditor or manager
**I want** to view the complete history of stock movements for any item
**So that** I can track changes and investigate discrepancies

## Functional Requirements

### Item Model Extensions

#### SKU (Stock Keeping Unit)
- **Format**: 3–32 characters, uppercase letters (A–Z), digits (0–9), dashes (-)
- **Normalization**: Trimmed and uppercased before validation (via `Trim()` + `ToUpperInvariant()`)
- **Uniqueness**: Each item must have a unique SKU across the inventory
- **Validation**: Enforced at the database level (unique constraint) and application layer
- **Example**: `COST-001`, `MASK-LATEX-BLK`, `PUMPKIN-5LB`
- **Duplicate Handling**: Check before insert; also catch `DbUpdateException` from unique-index constraint violation → return 409 ProblemDetails

#### Quantity
- **Type**: Non-negative integer (>= 0), enforced by CHECK constraint in database
- **Initial**: Always 0 on item creation (stock only changes through movements, so the history explains every unit)
- **Rules**: Cannot be negative; operations that would go negative are rejected (409 Conflict)
- **Audit**: Every change is logged in movement history
- **Concurrency**: Updates use atomic `ExecuteUpdateAsync` with compound condition (`WHERE Id = id AND Quantity + delta >= 0`)

#### Reorder Level
- **Type**: Non-negative integer (>= 0), enforced by CHECK constraint in database
- **Initial**: Set on creation (optional in request, defaults to 0)
- **Purpose**: Threshold for low-stock flagging
- **Rules**: Can be updated independently of quantity

### Stock Movements

#### Movement Record
Each movement tracks a single change to stock:
- **Item ID**: Reference to the item
- **Delta**: Non-zero signed integer (the change to quantity)
- **Reason**: String column in DB, enum in code—`Received`, `Sold`, `Damaged`, `Adjustment` (max 50 chars)
  - Unknown reason → 400 Bad Request with ValidationProblem
- **Note**: Optional text field (max 200 chars) for context (e.g., "Damaged in transit", "Return from rental")
- **Recorded At**: UTC timestamp (system-generated)
- **Result Quantity**: The quantity after the movement (denormalized for query efficiency)

#### Movement Recording Endpoint
- **Route**: `POST /api/items/{id}/movements`
- **Request Body**:
  ```json
  {
    "delta": -1,
    "reason": "Sold",
    "note": "Halloween party rental"
  }
  ```
- **Validation**:
  - `delta` must be non-zero (not 0)
  - `reason` must be a valid enum value (one of: Received, Sold, Damaged, Adjustment)
  - `note` is optional, max 200 characters
  - Item must exist
- **Concurrency Safety**: Inside a transaction:
  1. Run `ExecuteUpdateAsync` with compound WHERE: `WHERE Id = id AND Quantity + delta >= 0`
  2. Set `Quantity = Quantity + delta` and `UpdatedAt = UtcNow`
  3. If 0 rows affected:
     - If item doesn't exist → 404 Not Found
     - If constraint would fail (insufficient stock) → 409 Conflict
  4. Insert `StockMovement` record in same transaction with `ResultingQuantity` read back from Item
- **Conflict Handling**: Return `409 Conflict` with ProblemDetails:
  ```json
  {
    "type": "about:blank",
    "title": "Conflict",
    "status": 409,
    "detail": "Stock movement would result in negative quantity. Current: 5, Delta: -10",
    "instance": "/api/items/42/movements"
  }
  ```
- **Success Response**: `201 Created` with movement record

#### Movement History Endpoint
- **Route**: `GET /api/items/{id}/movements`
- **Query Parameters**:
  - `reason` (optional): Filter by reason (comma-separated, e.g., `?reason=Sold,Damaged`)
  - `limit` (optional, default 100, max 1000): Number of records to return
  - `offset` (optional, default 0): Pagination offset
- **Response**: Paginated list of movements, ordered by timestamp descending (newest first), limited to 100 by default
- **Empty**: If no movements exist, return empty array

### Item List Filtering

#### Low Stock Filter
- **Endpoint**: `GET /api/items?lowStock=true`
- **Logic**: Returns only items where `Quantity <= ReorderLevel`
- **Empty**: If no items are low stock, return empty array
- **Badge**: UI displays a "Low Stock" badge for affected items

### POST /api/items (Create Item)
- **Route**: `POST /api/items`
- **Request Body**:
  ```json
  {
    "name": "Vampire Cape",
    "description": "Classic black velvet",
    "sku": "cape-blk-001",
    "reorderLevel": 5
  }
  ```
- **Validation**:
  - `name` is required, non-empty
  - `sku` is required, normalized (Trim + ToUpperInvariant), then validated: 3–32 chars, uppercase alphanumeric + dash
  - `reorderLevel` is optional, defaults to 0, must be >= 0
  - `description` is optional
- **Duplicate SKU**:
  - Check if SKU exists before insert
  - If exists, return 409 ProblemDetails with detail: "SKU already in use"
  - Also catch `DbUpdateException` from unique-index constraint violation → return 409
- **New Item Quantity**: Always starts at 0 (stock only changes through movements)
- **Success Response**: `201 Created` with item DTO
- **Update existing ItemsTests**: Remove/update tests that POST items without Sku field

### Blazor Components

#### Item List Page
- **Route**: `/items`
- **Features**:
  - Tabular view of all items (name, SKU, quantity, reorder level, status)
  - "Low Stock" badge (spooky styling) for items where quantity <= reorder level
  - Filter toggle for "Show only low stock items"
  - Link to item details for each item
- **Styling**: Dark theme, WCAG AA contrast, accessible labels
- **Form**: Plain HTML `<form method="post" action="...">` with antiforgery token; no JavaScript interactivity

#### Item Details Page
- **Route**: `/items/{id}`
- **Sections**:
  1. Item metadata (name, description, SKU, quantity, reorder level)
  2. Movement history table (date, reason, delta, note, resulting quantity)
  3. Record Movement form:
     - `delta` (numeric input, non-zero validation on submit)
     - `reason` (dropdown or radio buttons)
     - `note` (optional textarea)
     - Submit button
- **Styling**: Dark theme, WCAG AA contrast, focus states on all inputs
- **Form**: Plain HTML `<form>` with antiforgery token; submission via POST
- **Error Handling**: Display validation errors and conflict messages inline

### Data Model

#### Item Table Extensions
Add columns to the existing `Items` table:
- `Sku` (string, unique, 3–32 chars, not null)
  - **Migration**: Backfill existing rows with `'ITEM-' || Id` before creating unique index (e.g., 'ITEM-1', 'ITEM-2')
- `Quantity` (int, >= 0, default 0)
  - **Constraint**: `CHECK (Quantity >= 0)` via `ToTable(t => t.HasCheckConstraint(...))`
- `ReorderLevel` (int, >= 0, default 0)
  - **Constraint**: `CHECK (ReorderLevel >= 0)` via `ToTable(t => t.HasCheckConstraint(...))`

#### StockMovements Table
New table to store movement history:
- `Id` (int, primary key, auto-increment)
- `ItemId` (int, foreign key to Items)
- `Delta` (int, non-zero)
- `Reason` (string, enum value: "Received", "Sold", "Damaged", "Adjustment")
- `Note` (string?, max 200 chars, nullable)
- `ResultingQuantity` (int, quantity after movement)
- `RecordedAt` (datetime, UTC, system-generated)
- **Indexes**:
  - `(ItemId, RecordedAt DESC)` for efficient history queries
  - `(ItemId)` for foreign key lookup

## Acceptance Criteria (EARS Format)

### SKU Management
**Given** an item in the system
**When** the item is created or updated
**Then** the SKU must be unique, 3–32 chars, uppercase letters/digits/dashes only
**And** duplicate SKUs are rejected with a 409 Conflict (checked before insert, also caught from unique-index constraint)

**Given** an item with an existing SKU
**When** I attempt to create another item with the same SKU
**Then** the creation fails with a 409 Conflict and detail: "SKU already in use"

**Given** I create a new item with `sku: "cape-blk-001"` (lowercase)
**When** the request is processed
**Then** the SKU is normalized to "CAPE-BLK-001" before validation and storage
**And** subsequent requests with "CAPE-BLK-001", "cape-blk-001", or "  CAPE-BLK-001  " (with whitespace) refer to the same item

### Quantity Constraints
**Given** an item with quantity 5
**When** I record a movement with delta -10
**Then** the system rejects it with a 409 Conflict
**And** the response includes the current quantity and attempted delta

**Given** an item with quantity 0
**When** I record a movement with delta -1
**Then** the system rejects it with a 409 Conflict

**Given** an item with any quantity
**When** I record a movement with delta 0
**Then** the system rejects it with a 400 Bad Request (non-zero delta required)

**Given** I create a new item (without specifying quantity)
**When** the item is created
**Then** its quantity is 0 and stock only changes through movements thereafter

### Low Stock Filtering
**Given** items with quantities [10, 5, 2, 0] and reorder level 5
**When** I request `GET /api/items?lowStock=true`
**Then** I receive items with quantities [5, 2, 0]
**And** the item with quantity 10 is excluded

**Given** no items have quantity <= reorder level
**When** I request `GET /api/items?lowStock=true`
**Then** I receive an empty array

### Movement History
**Given** an item with 3 movements recorded
**When** I request `GET /api/items/{id}/movements`
**Then** I receive all 3 movements ordered by timestamp descending

**Given** movements recorded with different reasons
**When** I request `GET /api/items/{id}/movements?reason=Sold,Damaged`
**Then** I receive only movements with reason "Sold" or "Damaged"

**Given** an item with 150 movements
**When** I request `GET /api/items/{id}/movements?limit=50&offset=100`
**Then** I receive 50 movements starting from offset 100

### Blazor UI - Item List
**Given** I am on the item list page
**When** an item has quantity <= reorder level
**Then** a "Low Stock" badge is displayed next to the item
**And** the badge uses accessible color contrast and clear labeling

**Given** I am on the item list page
**When** I toggle the "Show only low stock" filter
**Then** only low-stock items remain visible
**And** the page refetches data via form submission (no JavaScript)

### Blazor UI - Item Details
**Given** I am viewing an item detail page
**When** the item has 5 past movements
**Then** I see a table with all 5 movements, columns: date, reason, delta, note, resulting quantity

**Given** I am on the item details page
**When** I fill in the movement form and click submit
**Then** the movement is recorded
**And** the page shows a success message
**And** the movement appears in the history table
**And** the quantity display updates

**Given** I fill in the movement form with an invalid delta (e.g., 0, non-numeric)
**When** I submit the form
**Then** the form shows a validation error message
**And** the movement is not recorded

**Given** I record a movement that would make quantity negative
**When** I submit the form
**Then** the page displays a conflict error message
**And** the quantity remains unchanged

## Non-Functional Requirements

### Performance
- Low stock queries must complete in < 100ms for 10,000 items
- Movement history pagination must include proper indexes

### Security
- Input validation on all movement fields (delta non-zero, reason enum, note length)
- Antiforgery tokens on all POST/DELETE forms
- No SQL injection vulnerabilities (use parameterized queries via EF Core)

### Accessibility
- All form inputs must have associated `<label>` elements
- Focus states must be visible (outline or background change, 2px minimum)
- Color contrast must meet WCAG AA (4.5:1 for body text, 3:1 for large text)
- Error messages must be associated with inputs via `aria-describedby`

### UI Theme Compliance
- Spooky wording only in UI copy (e.g., "Creep these items back into stock", "Haunted quantities")
- Code identifiers remain professional (e.g., `StockMovementsService`, not `SpecterMovementsService`)
- Dark theme colors: background #0f0f1a, sidebar #1a1a2e, accent #e94560, text #eaeaea
