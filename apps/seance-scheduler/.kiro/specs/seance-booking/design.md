# SeanceScheduler: Seance Booking Feature — Design

## Data Model & Schema

### Medium Entity

Located in `src/SeanceScheduler.Web/Data/Models/Medium.cs`:

```
Medium
├── Id: int (PK, auto-increment)
├── Name: string (required, 1–100, unique case-insensitive)
├── Specialty: string? (optional, 0–200)
├── IsActive: bool (required, default true)
└── CreatedAt: DateTime (UTC, immutable)
```

**Navigation:**
- `Seances: ICollection<Seance>` (one-to-many inverse)

**Constraints & Indices:**
- Unique index on `NormalizedName` (`Name.Trim().ToUpperInvariant()`): case-insensitive for any script — SQLite `COLLATE NOCASE` folds ASCII only (Cyrillic duplicates slipped through). `Name` keeps NOCASE for ordering.
- Regular index on `IsActive` (for filtering active mediums in UI)

### Seance Entity

Located in `src/SeanceScheduler.Web/Data/Models/Seance.cs`:

```
Seance
├── Id: int (PK, auto-increment)
├── MediumId: int (FK → Medium.Id, required)
├── ClientName: string (required, 1–100)
├── StartsAt: DateTime (UTC, required)
├── EndsAt: DateTime (UTC, required, > StartsAt)
├── Status: string (required, default "Scheduled", enum: "Scheduled" | "Cancelled")
├── CreatedAt: DateTime (UTC, immutable)
└── CancelledAt: DateTime? (UTC, nullable, set only when Status = "Cancelled")
```

**Navigation:**
- `Medium: Medium` (many-to-one)

**Constraints & Indices:**
- Foreign key: `MediumId` → `Medium.Id` with `DeleteBehavior.Restrict` (mediums are deactivated, never deleted; history must survive)
- Non-unique index on `(MediumId, StartsAt)` for overlap detection. NOT unique: the same slot may be booked, cancelled and booked again, so several Cancelled rows with identical times are legal
- `Status` stored as string (`HasConversion<string>()`, max length 16)
- All `DateTime` properties use a value converter that marks values read from SQLite as `DateTimeKind.Utc` (SQLite returns `Unspecified`); the service normalises incoming times with `ToUniversalTime()` before validation
- Regular indices on `Status`, `CreatedAt` for query filtering and sorting

**Validation Rules (enforced at model and service layer):**
- Duration: `(EndsAt - StartsAt).TotalMinutes ∈ [30, 240]` and divisible by 15
- StartsAt: on 15-minute boundary (minutes ∈ {0, 15, 30, 45})
- StartsAt: in future (relative to injected `TimeProvider`)
- EndsAt: in future
- Overlap check: for Scheduled seances, no two seances for the same medium with overlapping [StartsAt, EndsAt)
  - Back-to-back seances are **allowed** (e.g., one ends at 14:30, another starts at 14:30)

### Database Context

Located in `src/SeanceScheduler.Web/Data/ApplicationDbContext.cs`:

**DbSets:**
- `DbSet<Medium> Mediums`
- `DbSet<Seance> Seances`

**ModelCreating Configuration:**
- Configure entity mappings, constraints, indices, and UTC datetime conversions
- Define relationships (one-to-many)

## API Design

### Request/Response DTOs

#### CreateMediumRequest
```csharp
public sealed record CreateMediumRequest(string? Name, string? Specialty)
{
    public Dictionary<string, string[]> Validate() { /* ... */ }
}
```

#### MediumDto
```csharp
public sealed record MediumDto(int Id, string Name, string? Specialty, bool IsActive, DateTime CreatedAt);
```

#### CreateSeanceRequest
```csharp
public sealed record CreateSeanceRequest(int MediumId, string? ClientName, DateTime StartsAt, DateTime EndsAt)
{
    public Dictionary<string, string[]> Validate() { /* ... */ }
}
```

#### SeanceDto
```csharp
public sealed record SeanceDto(int Id, int MediumId, string ClientName, DateTime StartsAt, DateTime EndsAt, string Status, DateTime CreatedAt, DateTime? CancelledAt);
```

### Endpoints

**Features/Mediums/MediumsEndpoints.cs:**

```
GET  /api/mediums              → List<MediumDto>
GET  /api/mediums/{id}         → MediumDto | 404
POST /api/mediums              → MediumDto | 201 | 400 | 409
POST /api/mediums/{id}/deactivate → MediumDto | 200 | 404
```

**Features/Seances/SeancesEndpoints.cs:**

```
GET  /api/seances              → List<SeanceDto> | 200 | 400
POST /api/seances              → SeanceDto | 201 | 400 | 404 | 409
POST /api/seances/{id}/cancel  → SeanceDto | 200 | 404 | 409
```

### Error Responses

All error responses use RFC 7807 ProblemDetails format. Common scenarios:

- **400 Bad Request:** Invalid duration, not on 15-min boundary, in past, name/client name too long, missing required fields
- **404 Not Found:** Medium or seance does not exist
- **409 Conflict:** Duplicate medium name, inactive medium, overlapping seance, already cancelled seance

Example:
```json
{
  "type": "https://example.com/errors/conflict",
  "title": "Conflict",
  "status": 409,
  "detail": "Seance overlaps existing booking for this medium",
  "instance": "/api/seances"
}
```

## Concurrency & Transaction Handling

### Challenge

SQLite is single-writer; at the application level, two simultaneous requests for overlapping seance slots with the same medium must result in exactly **one 201 Created** and **one 409 Conflict**.

### Solution

**Approach: Explicit BEGIN IMMEDIATE Transaction**

The overlap check and seance creation are wrapped in a single EF Core transaction with SQLite's `BEGIN IMMEDIATE` semantics:

1. **Service layer method:**
   ```csharp
   public async Task<Seance> BookSeanceAsync(int mediumId, string clientName, DateTime startsAt, DateTime endsAt, CancellationToken cancellationToken)
   {
       using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
       try
       {
           // 1. Validate medium exists and is active
           var medium = await _dbContext.Mediums.FindAsync([mediumId], cancellationToken);
           if (medium == null) throw new NotFoundException(...);
           if (!medium.IsActive) throw new ConflictException(...);
           
           // 2. Check for overlaps (all Scheduled seances for this medium)
           var overlap = await _dbContext.Seances
               .Where(s => s.MediumId == mediumId && s.Status == "Scheduled")
               .AnyAsync(s => s.StartsAt < endsAt && s.EndsAt > startsAt, cancellationToken);
           if (overlap) throw new ConflictException(...);
           
           // 3. Create seance
           var seance = new Seance { ... };
           _dbContext.Seances.Add(seance);
           await _dbContext.SaveChangesAsync(cancellationToken);
           
           await transaction.CommitAsync(cancellationToken);
           return seance;
       }
       catch
       {
           await transaction.RollbackAsync(cancellationToken);
           throw;
       }
   }
   ```

2. **Why this works:**
   - `BEGIN IMMEDIATE` acquires a **write lock** immediately, preventing other writers from entering the transaction body
   - SQLite's file-level locking ensures only one process can hold the write lock at a time
   - If a second request arrives while the first is in the transaction, it waits for the lock; once the first commits or rolls back, the second can proceed
   - If the second request finds an overlap after the first has committed, it throws `409 Conflict`

3. **Limitations:**
   - EF Core on SQLite: `BeginTransactionAsync()` maps to `SqliteConnection.BeginTransaction(deferred: false)`, which issues `BEGIN IMMEDIATE`. Do NOT pass `deferred: true` / `UseTransaction` with a deferred transaction. The isolation level argument is irrelevant on SQLite.
   - Waiting writers get `SQLITE_BUSY`; Microsoft.Data.Sqlite retries until the command timeout (default 30 s), so short transactions queue instead of failing.
   - **Scope of the guarantee:** the lock is a file lock on the database, so it holds for any number of processes/containers on the same host sharing the same DB file on a local filesystem. It does NOT hold for a DB file on a network filesystem (NFS/SMB) or for separate DB copies — that requires PostgreSQL with an exclusion constraint (`EXCLUDE USING gist (medium_id WITH =, tstzrange(starts_at, ends_at) WITH &&) WHERE status = 'Scheduled'`), out of scope here.
   - Keep the transaction short: no external calls inside it.

4. **Testing:**
   - Concurrency test spawns 20 tasks that attempt to book overlapping seances for the same medium
   - Assert exactly 1 succeeds (201) and 19 fail (409)

## Service Layer Design

### MediumsService

Located in `src/SeanceScheduler.Web/Features/Mediums/MediumsService.cs`:

```csharp
public class MediumsService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<MediumsService> _logger;

    public async Task<IReadOnlyList<MediumDto>> GetMediumsAsync(CancellationToken cancellationToken)
    public async Task<MediumDto?> GetMediumByIdAsync(int id, CancellationToken cancellationToken)
    public async Task<MediumDto> CreateMediumAsync(string name, string? specialty, CancellationToken cancellationToken)
    public async Task<MediumDto> DeactivateMediumAsync(int id, CancellationToken cancellationToken)
}
```

**Key behaviors:**
- `GetMediumsAsync`: Returns all mediums ordered by name
- `GetMediumByIdAsync`: Returns a single medium or null
- `CreateMediumAsync`: Validates name uniqueness (case-insensitive), trims inputs, throws `ConflictException` on duplicate
- `DeactivateMediumAsync`: Throws 404 if medium not found

### SeancesService

Located in `src/SeanceScheduler.Web/Features/Seances/SeancesService.cs`:

```csharp
public class SeancesService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SeancesService> _logger;

    public async Task<IReadOnlyList<SeanceDto>> GetSeancesAsync(int? mediumId, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    public async Task<SeanceDto> BookSeanceAsync(int mediumId, string clientName, DateTime startsAt, DateTime endsAt, CancellationToken cancellationToken)
    public async Task<SeanceDto> CancelSeanceAsync(int id, CancellationToken cancellationToken)
}
```

**Key behaviors:**
- `GetSeancesAsync`: Filters by mediumId, from, to; returns only Scheduled seances; ordered by StartsAt, then Id
- `BookSeanceAsync`: Validates all business rules, checks overlap within a transaction, throws appropriate exceptions
- `CancelSeanceAsync`: Sets Status to "Cancelled" and CancelledAt; throws 409 if already cancelled

### Exception Types

```csharp
public class NotFoundException : Exception { }
public class ConflictException : Exception { }
public class ValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; set; }
}
```

## UI Design (Blazor Static SSR)

### Pages

**`Components/Pages/Mediums.razor`** (`/mediums`)
- Display: List of all mediums (name, specialty, IsActive status)
- Form: Create medium (name, specialty)
- Actions: Deactivate button for each active medium
- Styling: Dark theme, spooky copy ("Summon a Medium", "Conjured Mediums")
- Validation: Display error messages for name (required, length, uniqueness)
- Redirect: On success, POST → GET redirect to `/mediums`
- Conflict handling: 409 response keeps form values and shows conflict message

**`Components/Pages/Schedule.razor`** (`/schedule`)
- Display: Upcoming Scheduled seances grouped by date, showing medium name, client name, start/end times
- Filter form: GET-based, no antiforgery, to filter by medium
- Actions: Cancel button (POST form) for each seance
- Styling: Dark theme, table or card layout
- Spooky copy: e.g., "The Summoned Appointments", "Conjured Schedule"

**`Components/Pages/ScheduleNew.razor`** (`/schedule/new`)
- Form: Book seance with fields: Medium (dropdown), ClientName, StartsAt, EndsAt
- Validation: Server-side via `[SupplyParameterFromForm]` on a handler method
- Antiforgery: Use `@formname` and Blazor's built-in antiforgery protection
- Validation messages: Display linked via `aria-describedby` for accessibility
- Redirect: On 201 success, redirect to `/schedule`
- Conflict handling: On 409, keep form values, display conflict message inline, no redirect
- Styling: Dark theme, WCAG AA contrast (4.5:1 for body text)

### Form Component Pattern

```razor
@page "/schedule/new"
@using SeanceScheduler.Web.Features.Seances
@rendermode InteractiveServer

<PageTitle>Book a Seance</PageTitle>

<form method="post" @formname="BookSeance">
    <AntiforgeryToken />
    
    <label for="mediumId">Select a Medium</label>
    <select id="mediumId" name="mediumId" aria-describedby="mediumId-error">
        @foreach (var medium in Mediums)
        {
            <option value="@medium.Id">@medium.Name</option>
        }
    </select>
    <div id="mediumId-error">@MediumIdError</div>
    
    <!-- Similar for ClientName, StartsAt, EndsAt -->
    
    <button type="submit">Book Seance</button>
</form>

@code
{
    [SupplyParameterFromForm]
    public CreateSeanceRequest? Request { get; set; }
    
    private async Task OnValidSubmit()
    {
        // POST to /api/seances via service call
        // On success, NavigationManager.NavigateTo("/schedule");
        // On 409, capture error and re-render
    }
}
```

### Navigation

Add links in `Components/Layout/MainLayout.razor`:
- Mediums → `/mediums`
- Schedule → `/schedule`
- Book a Seance → `/schedule/new`

### Styling Notes

- **Colors:** Dark backgrounds `#0f0f1a`, sidebar `#1a1a2e`, accents `#e94560` (from `wwwroot/css/app.css`)
- **Text color:** `#eaeaea` (light gray)
- **Contrast:** Ensure 4.5:1 ratio for body text
- **Spooky copy:** UI labels only (e.g., "The Spirits are Waiting", "Conjure a Seance"), NOT in code

## Testing Strategy

### Unit Tests (Service Layer)

**SeancesServiceTests.cs:**

1. **Duration validation:**
   - `Booking_With_29_Minutes_Returns_ValidationError`
   - `Booking_With_30_Minutes_Succeeds`
   - `Booking_With_240_Minutes_Succeeds`
   - `Booking_With_241_Minutes_Returns_ValidationError`
   - `Booking_Duration_Not_Multiple_Of_15_Returns_ValidationError`

2. **15-minute boundary:**
   - `Booking_StartsAt_On_Boundary_Succeeds` (e.g., 14:00, 14:15, 14:30, 14:45)
   - `Booking_StartsAt_Off_Boundary_Returns_ValidationError` (e.g., 14:01, 14:17)

3. **Future time:**
   - `Booking_In_Past_Returns_ValidationError`
   - `Booking_In_Future_Succeeds`

4. **Back-to-back seances:**
   - `Booking_BackToBack_With_Existing_Seance_Succeeds` (new ends at 14:30, existing starts at 14:30)

5. **Overlap detection:**
   - `Booking_Overlapping_Start_Returns_409_Conflict`
   - `Booking_Overlapping_End_Returns_409_Conflict`
   - `Booking_Fully_Contained_Overlap_Returns_409_Conflict`
   - `Booking_Exact_Same_Time_Returns_409_Conflict`

6. **Cancelled seances:**
   - `Booking_Does_Not_Check_Cancelled_Seances` (a cancelled seance does not block)

7. **Inactive medium:**
   - `Booking_With_Inactive_Medium_Returns_409_Conflict`

8. **Unknown medium:**
   - `Booking_With_Unknown_Medium_Returns_404_NotFound`

9. **Concurrency:**
   - `Concurrent_Overlapping_Bookings_Yield_One_201_And_N_Minus_1_409` (spawn 20 tasks, assert 1 success)

**MediumsServiceTests.cs:**

1. **Unique name (case-insensitive):**
   - `Creating_Medium_With_Duplicate_Name_Same_Case_Returns_409`
   - `Creating_Medium_With_Duplicate_Name_Different_Case_Returns_409`

2. **Name validation:**
   - `Creating_Medium_With_Empty_Name_Returns_ValidationError`
   - `Creating_Medium_With_Name_Exceeding_100_Chars_Returns_ValidationError`

3. **Specialty:**
   - `Creating_Medium_With_Valid_Specialty_Succeeds`
   - `Creating_Medium_With_Specialty_Exceeding_200_Chars_Returns_ValidationError`

4. **Whitespace trimming:**
   - `Creating_Medium_Trims_Whitespace`

5. **Deactivation:**
   - `Deactivating_Active_Medium_Succeeds`
   - `Deactivating_Non_Existent_Medium_Returns_404`

### Integration Tests (Endpoints)

**SeancesEndpointTests.cs:**

1. **POST /api/seances:**
   - `CreateSeance_Valid_Request_Returns_201_Created`
   - `CreateSeance_Invalid_Duration_Returns_400_BadRequest`
   - `CreateSeance_Off_Boundary_Returns_400_BadRequest`
   - `CreateSeance_In_Past_Returns_400_BadRequest`
   - `CreateSeance_Unknown_Medium_Returns_404_NotFound`
   - `CreateSeance_Inactive_Medium_Returns_409_Conflict`
   - `CreateSeance_Overlapping_Existing_Returns_409_Conflict`

2. **GET /api/seances:**
   - `GetSeances_No_Filters_Returns_All_Scheduled`
   - `GetSeances_Filter_By_MediumId_Returns_Only_For_Medium`
   - `GetSeances_Filter_By_DateRange_Returns_Only_In_Range`
   - `GetSeances_Invalid_Query_Params_Returns_400_BadRequest`

3. **POST /api/seances/{id}/cancel:**
   - `CancelSeance_Valid_Id_Returns_200_OK`
   - `CancelSeance_Already_Cancelled_Returns_409_Conflict`
   - `CancelSeance_Non_Existent_Returns_404_NotFound`

**MediumsEndpointTests.cs:**

1. **POST /api/mediums:**
   - `CreateMedium_Valid_Request_Returns_201_Created`
   - `CreateMedium_Duplicate_Name_Returns_409_Conflict`
   - `CreateMedium_Invalid_Name_Returns_400_BadRequest`

2. **GET /api/mediums:**
   - `GetMediums_Returns_All_Ordered_By_Name`

3. **GET /api/mediums/{id}:**
   - `GetMedium_Valid_Id_Returns_200_OK`
   - `GetMedium_Non_Existent_Returns_404_NotFound`

4. **POST /api/mediums/{id}/deactivate:**
   - `DeactivateMedium_Valid_Id_Returns_200_OK`
   - `DeactivateMedium_Non_Existent_Returns_404_NotFound`

### UI Tests (Blazor Form)

**MediumsPageTests.cs:**

1. `Creating_Medium_With_Valid_Data_Succeeds_And_Redirects`
2. `Creating_Medium_With_Duplicate_Name_Shows_Conflict_And_Keeps_Form_Values`
3. `Creating_Medium_With_Missing_Name_Shows_Validation_Error`

**ScheduleNewPageTests.cs:**

1. `Booking_Seance_With_Valid_Data_Succeeds_And_Redirects_To_Schedule`
2. `Booking_Seance_With_Overlap_Shows_Conflict_And_Keeps_Form_Values`
3. `Booking_Seance_Form_Missing_AntiforgeryToken_Returns_400`
4. `Booking_Seance_Form_Validation_Messages_Display_And_Link_Via_AriaDescribedBy`

**SchedulePageTests.cs:**

1. `Schedule_Page_Groups_Seances_By_Date`
2. `Schedule_Page_Filter_By_Medium_Works`
3. `Cancelling_Seance_From_Schedule_Updates_Display`

### E2E Tests (Playwright)

**SeanceScheduler.E2E/BookingFlowTests.cs:**

1. **Setup:** WebApplicationFactory with Kestrel on port 0, spawn Chromium
2. `BookSeance_Happy_Path` → Book, verify appears in schedule
3. `BookSeance_Conflict_Shows_Message` → Attempt overlap, see 409 message
4. `CancelSeance_Happy_Path` → Cancel, verify removed from schedule
5. `NotFound_Page_Displays` → Navigate to non-existent URL, verify 404 page

## Migration

**Migration name:** `AddSeanceBooking`

**Command:**
```bash
dotnet ef migrations add AddSeanceBooking --project src/SeanceScheduler.Web
```

**Generated files:**
- `Migrations/[timestamp]_AddSeanceBooking.cs` (Up/Down methods)
- `Migrations/[timestamp]_AddSeanceBooking.Designer.cs` (auto-generated snapshot)
- `Migrations/ApplicationDbContextModelSnapshot.cs` (updated snapshot)

**Never hand-edit Designer or Snapshot files.**

## Summary

The seance booking feature is a complete vertical slice: data models, service layer with transaction-based concurrency control, RESTful API endpoints, Blazor SSR UI with form validation, comprehensive tests (unit, integration, UI, e2e), and a migration. The design enforces all business rules at the service layer, relies on SQLite's `BEGIN IMMEDIATE` write lock for correctness (any number of processes sharing one local DB file), and provides a consistent, accessible, spooky-themed UI.
