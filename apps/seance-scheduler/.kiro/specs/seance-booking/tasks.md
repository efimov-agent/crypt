# SeanceScheduler: Seance Booking Feature — Implementation Tasks

## Phase 1: Data Models & Migrations

- [x] 1. Create Medium entity model
  - [x] 1.1 Add `src/SeanceScheduler.Web/Data/Models/Medium.cs`
  - [x] 1.2 Define properties: Id, Name, Specialty, IsActive, CreatedAt
  - [x] 1.3 Configure UTC datetime conversion
  - [x] 1.4 Add navigation property: `Seances: ICollection<Seance>`
  - _Requirements: 1.0, Data Model_

- [x] 2. Create Seance entity model
  - [x] 2.1 Add `src/SeanceScheduler.Web/Data/Models/Seance.cs`
  - [x] 2.2 Define properties: Id, MediumId, ClientName, StartsAt, EndsAt, Status, CreatedAt, CancelledAt
  - [x] 2.3 Configure UTC datetime conversion for all DateTime fields
  - [x] 2.4 Add navigation property: `Medium: Medium`
  - [x] 2.5 Validate Status is serialized as string enum ("Scheduled" or "Cancelled")
  - _Requirements: 1.0, Data Model_

- [x] 3. Update ApplicationDbContext
  - [x] 3.1 Add `DbSet<Medium> Mediums`
  - [x] 3.2 Add `DbSet<Seance> Seances`
  - [x] 3.3 Configure Medium entity: unique index on Name (case-insensitive COLLATE NOCASE), index on IsActive
  - [x] 3.4 Configure Seance entity: foreign key MediumId → Medium.Id with DeleteBehavior.Restrict, NON-unique index on (MediumId, StartsAt), Status as string (max 16), UTC value converter for all DateTime properties (SQLite returns Kind=Unspecified)
  - [x] 3.5 Ensure all DateTime properties use UTC conversion
  - _Requirements: 1.0, Data Model_

- [x] 4. Create and apply migration
  - [x] 4.1 Run `dotnet ef migrations add AddSeanceBooking --project src/SeanceScheduler.Web`
  - [x] 4.2 Verify migration file created in `Migrations/[timestamp]_AddSeanceBooking.cs`
  - [x] 4.3 Verify Designer.cs and ModelSnapshot.cs auto-generated (do NOT hand-edit)
  - [x] 4.4 Build solution to verify no errors
  - [x] 4.5 Document migration in CHANGELOG or commit message
  - _Requirements: 1.0, Migration_

## Phase 2: Service & Exception Layer

- [x] 5. Create exception types
  - [x] 5.1 Add `src/SeanceScheduler.Web/Features/Common/NotFoundException.cs`
  - [x] 5.2 Add `src/SeanceScheduler.Web/Features/Common/ConflictException.cs`
  - [x] 5.3 Add `src/SeanceScheduler.Web/Features/Common/ValidationException.cs` with Errors dict
  - _Requirements: Error Handling_

- [x] 6. Create MediumsService
  - [x] 6.1 Add `src/SeanceScheduler.Web/Features/Mediums/MediumsService.cs`
  - [x] 6.2 Implement `GetMediumsAsync(CancellationToken)` → returns all, ordered by Name
  - [x] 6.3 Implement `GetMediumByIdAsync(int, CancellationToken)` → returns Medium or null
  - [x] 6.4 Implement `CreateMediumAsync(string, string?, CancellationToken)` → validates unique name (case-insensitive), trims inputs, throws ConflictException on duplicate, creates record with IsActive=true and UTC CreatedAt
  - [x] 6.5 Implement `DeactivateMediumAsync(int, CancellationToken)` → sets IsActive=false, throws NotFoundException if not found
  - [x] 6.6 Add ILogger<MediumsService> logging for all operations
  - _Requirements: 1.0, 1.1, 1.2, 1.3, 1.4_

- [x] 7. Create SeancesService
  - [x] 7.1 Add `src/SeanceScheduler.Web/Features/Seances/SeancesService.cs`
  - [x] 7.2 Inject ApplicationDbContext, TimeProvider, ILogger<SeancesService>
  - [x] 7.3 Implement `GetSeancesAsync(int?, DateTime?, DateTime?, CancellationToken)` with optional mediumId, from, to filters; return only Scheduled seances ordered by StartsAt then Id; throw ValidationException if params invalid
  - [x] 7.4 Implement `BookSeanceAsync(int, string, DateTime, DateTime, CancellationToken)` with comprehensive validation:
    - [x] 7.4.1 Validate duration 30..240 minutes and multiple of 15
    - [x] 7.4.2 Validate StartsAt on 15-minute boundary (minutes ∈ {0, 15, 30, 45})
    - [x] 7.4.3 Validate StartsAt in future (relative to TimeProvider.GetUtcNow())
    - [ ] 7.4.4 Validate EndsAt in future
    - [ ] 7.4.5 Fetch medium; throw NotFoundException if not found
    - [ ] 7.4.6 Throw ConflictException if medium IsActive=false
    - [ ] 7.4.7 Check overlap within BEGIN IMMEDIATE transaction (see 7.4.9)
    - [ ] 7.4.8 Use half-open interval logic: overlap if (s.StartsAt < endsAt && s.EndsAt > startsAt); back-to-back is allowed
    - [ ] 7.4.9 Open the transaction with `Database.BeginTransactionAsync()` (Microsoft.Data.Sqlite issues BEGIN IMMEDIATE) BEFORE the medium lookup (7.4.5/7.4.6) so deactivation cannot race the booking; pure input validation (7.4.1–7.4.4) runs before the transaction; check only Scheduled seances, create Seance with Status="Scheduled" and UTC CreatedAt
    - [ ] 7.4.10 Return created Seance
  - [ ] 7.5 Implement `CancelSeanceAsync(int, CancellationToken)` atomically: `ExecuteUpdateAsync` with `WHERE Id = id AND Status = Scheduled` setting Status=Cancelled and CancelledAt=TimeProvider UTC now; if 0 rows affected → 404 when the seance does not exist, otherwise 409 (already cancelled); then read the seance with AsNoTracking and return it (never return a stale tracked entity)
  - [ ] 7.6 Add logging for all operations
  - _Requirements: 2.0, 2.1, 2.2, 2.3, 2.4, Concurrency_

## Phase 3: DTOs & Request Validation

- [ ] 8. Create request DTOs
  - [ ] 8.1 Add `src/SeanceScheduler.Web/Features/Mediums/CreateMediumRequest.cs` as sealed record with Name? and Specialty?
  - [ ] 8.2 Implement `Validate()` → Dict<string, string[]> with errors for missing name, too long (>100), specialty too long (>200)
  - [ ] 8.3 Add `src/SeanceScheduler.Web/Features/Seances/CreateSeanceRequest.cs` as sealed record with MediumId, ClientName?, StartsAt, EndsAt
  - [ ] 8.4 Implement `Validate()` → Dict<string, string[]> with errors for missing/too-long ClientName, invalid duration, off-boundary start, past start, invalid date format
  - _Requirements: 1.0, 2.0_

- [ ] 9. Create response DTOs
  - [ ] 9.1 Add `src/SeanceScheduler.Web/Features/Mediums/MediumDto.cs` as sealed record (Id, Name, Specialty?, IsActive, CreatedAt)
  - [ ] 9.2 Add `src/SeanceScheduler.Web/Features/Seances/SeanceDto.cs` as sealed record (Id, MediumId, ClientName, StartsAt, EndsAt, Status, CreatedAt, CancelledAt?)
  - [ ] 9.3 Add mapping helpers if needed (e.g., `ToDto()` extension methods)
  - _Requirements: 1.0, 2.0_

## Phase 4: API Endpoints

- [ ] 10. Create Mediums API endpoints
  - [ ] 10.1 Add `src/SeanceScheduler.Web/Features/Mediums/MediumsEndpoints.cs`
  - [ ] 10.2 Implement `MapMediumsEndpoints(this WebApplication app)`
  - [ ] 10.3 Add `GET /api/mediums` → returns all MediumDto ordered by Name
  - [ ] 10.4 Add `GET /api/mediums/{id:int}` → returns MediumDto or 404 ProblemDetails
  - [ ] 10.5 Add `POST /api/mediums` → accept CreateMediumRequest, validate, call service, return 201 with Location header or 400/409 ProblemDetails
  - [ ] 10.6 Add `POST /api/mediums/{id:int}/deactivate` → call service, return 200 or 404 ProblemDetails
  - [ ] 10.7 Add ILogger logging for each endpoint
  - _Requirements: 1.0, 1.1, 1.2, 1.3, 1.4, Error Handling_

- [ ] 11. Create Seances API endpoints
  - [ ] 11.1 Add `src/SeanceScheduler.Web/Features/Seances/SeancesEndpoints.cs`
  - [ ] 11.2 Implement `MapSeancesEndpoints(this WebApplication app)`
  - [ ] 11.3 Add `GET /api/seances` with query filters (mediumId, from, to) → returns List<SeanceDto> ordered by StartsAt/Id or 400 ProblemDetails
  - [ ] 11.4 Add `POST /api/seances` → accept CreateSeanceRequest, validate, call service, return 201 with Location header or 400/404/409 ProblemDetails
  - [ ] 11.5 Add `POST /api/seances/{id:int}/cancel` → call service, return 200 or 404/409 ProblemDetails
  - [ ] 11.6 Add ILogger logging for each endpoint
  - _Requirements: 2.0, 2.1, 2.2, 2.3, 2.4, Error Handling_

- [ ] 12. Register services and endpoints in Program.cs
  - [ ] 12.1 Add `builder.Services.AddScoped<MediumsService>()`
  - [ ] 12.2 Add `builder.Services.AddScoped<SeancesService>()`
  - [ ] 12.3 Inject and register TimeProvider (use `builder.Services.AddSingleton(TimeProvider.System)`)
  - [ ] 12.4 Call `app.MapMediumsEndpoints()` and `app.MapSeancesEndpoints()` after routing setup
  - _Requirements: 1.0, 2.0_

## Phase 5: Service Layer Tests

- [x] 13. Create MediumsService unit tests
  - [x] 13.1 Add `tests/SeanceScheduler.Tests/Features/MediumsServiceTests.cs`
  - [x] 13.2 Test `GetMediumsAsync()` returns all mediums ordered by name
  - [x] 13.3 Test `CreateMediumAsync()` with valid data succeeds
  - [x] 13.4 Test `CreateMediumAsync()` trims whitespace from name and specialty
  - [x] 13.5 Test `CreateMediumAsync()` with duplicate name (same case) throws ConflictException
  - [x] 13.6 Test `CreateMediumAsync()` with duplicate name (different case) throws ConflictException
  - [x] 13.7 Test `CreateMediumAsync()` with empty/null name throws ValidationException
  - [x] 13.8 Test `CreateMediumAsync()` with name >100 chars throws ValidationException
  - [x] 13.9 Test `CreateMediumAsync()` with specialty >200 chars throws ValidationException
  - [x] 13.10 Test `DeactivateMediumAsync()` on active medium succeeds and sets IsActive=false
  - [x] 13.11 Test `DeactivateMediumAsync()` on non-existent medium throws NotFoundException
  - _Requirements: 1.0, 1.1, 1.2, 1.3, 1.4_

- [x] 14. Create SeancesService unit tests — Part A: Validation
  - [x] 14.1 Add `tests/SeanceScheduler.Tests/Features/SeancesServiceTests.cs`
  - [x] 14.2 Test `BookSeanceAsync()` with 29-minute duration throws ValidationException
  - [x] 14.3 Test `BookSeanceAsync()` with 30-minute duration succeeds
  - [x] 14.4 Test `BookSeanceAsync()` with 240-minute duration succeeds
  - [x] 14.5 Test `BookSeanceAsync()` with 241-minute duration throws ValidationException
  - [x] 14.6 Test `BookSeanceAsync()` with duration not multiple of 15 throws ValidationException
  - [x] 14.7 Test `BookSeanceAsync()` with StartsAt on 15-min boundary succeeds (test 14:00, 14:15, 14:30, 14:45)
  - [x] 14.8 Test `BookSeanceAsync()` with StartsAt off boundary throws ValidationException
  - [x] 14.9 Test `BookSeanceAsync()` with StartsAt in past throws ValidationException
  - [x] 14.10 Test `BookSeanceAsync()` with StartsAt in future succeeds
  - _Requirements: 2.0, 2.1, 2.2, 2.3, 2.4_

- [x] 15. Create SeancesService unit tests — Part B: Business Rules
  - [x] 15.1 Test `BookSeanceAsync()` with unknown medium throws NotFoundException
  - [x] 15.2 Test `BookSeanceAsync()` with inactive medium throws ConflictException
  - [x] 15.3 Test `BookSeanceAsync()` back-to-back with existing seance succeeds (new ends at 14:30, existing starts at 14:30)
  - [x] 15.4 Test `BookSeanceAsync()` with overlap at start throws ConflictException
  - [x] 15.5 Test `BookSeanceAsync()` with overlap at end throws ConflictException
  - [x] 15.6 Test `BookSeanceAsync()` with full containment overlap throws ConflictException
  - [x] 15.7 Test `BookSeanceAsync()` with existing seance fully contained throws ConflictException
  - [x] 15.8 Test `BookSeanceAsync()` does not check Cancelled seances (can book over a Cancelled seance)
  - _Requirements: 2.0, 2.1, 2.2, 2.3, 2.4_

- [x] 16. Create SeancesService unit tests — Part C: Concurrency
  - [x] 16.1 Test `CancelSeanceAsync()` on Scheduled seance succeeds and sets Status="Cancelled" and CancelledAt=UtcNow
  - [x] 16.2 Test `CancelSeanceAsync()` on already-Cancelled seance throws ConflictException
  - [x] 16.3 Test `CancelSeanceAsync()` on non-existent seance throws NotFoundException
  - [x] 16.4 Test `GetSeancesAsync()` with no filters returns all Scheduled seances ordered by StartsAt/Id
  - [x] 16.5 Test `GetSeancesAsync()` filters by mediumId correctly
  - [x] 16.6 Test `GetSeancesAsync()` filters by from/to date range correctly
  - [x] 16.7 Test concurrent overlapping bookings for same medium: spawn 20 tasks, each with its OWN DI scope/DbContext (or via HttpClient), against a file-based SQLite DB (not a shared in-memory connection, which would serialize everything and prove nothing); assert exactly 1 succeeds and 19 get ConflictException/409, and the DB holds exactly 1 Scheduled seance
  - [x] 16.8 Test two concurrent cancels of the same seance: exactly one succeeds, the other gets 409
  - _Requirements: 2.0, 2.1, 2.2, 2.3, 2.4, Concurrency_

## Phase 6: Endpoint Integration Tests

- [x] 17. Create Mediums endpoint tests
  - [x] 17.1 Add `tests/SeanceScheduler.Tests/Features/MediumsEndpointTests.cs` using TestDatabaseFixture
  - [x] 17.2 Test `GET /api/mediums` returns 200 OK with list of mediums
  - [x] 17.3 Test `GET /api/mediums/{id}` with valid id returns 200 OK
  - [x] 17.4 Test `GET /api/mediums/{id}` with non-existent id returns 404
  - [x] 17.5 Test `POST /api/mediums` with valid data returns 201 Created with Location header
  - [x] 17.6 Test `POST /api/mediums` with duplicate name returns 409 Conflict ProblemDetails
  - [x] 17.7 Test `POST /api/mediums` with empty name returns 400 BadRequest with validation error
  - [x] 17.8 Test `POST /api/mediums` with name >100 chars returns 400 BadRequest
  - [x] 17.9 Test `POST /api/mediums/{id}/deactivate` returns 200 OK and sets IsActive=false
  - [x] 17.10 Test `POST /api/mediums/{id}/deactivate` with non-existent id returns 404
  - _Requirements: 1.0, 1.1, 1.2, 1.3, 1.4, Error Handling_

- [x] 18. Create Seances endpoint tests — Part A: Happy Paths
  - [x] 18.1 Add `tests/SeanceScheduler.Tests/Features/SeancesEndpointTests.cs` using TestDatabaseFixture
  - [x] 18.2 Test `POST /api/seances` with valid data returns 201 Created with Location header and SeanceDto
  - [x] 18.3 Test `GET /api/seances` returns 200 OK with list of Scheduled seances ordered by StartsAt/Id
  - [x] 18.4 Test `GET /api/seances` with mediumId filter works correctly
  - [x] 18.5 Test `GET /api/seances` with from/to date filters works correctly
  - [x] 18.6 Test `POST /api/seances/{id}/cancel` on Scheduled seance returns 200 OK with updated seance Status="Cancelled"
  - _Requirements: 2.0, 2.1, 2.2, 2.3, 2.4, Error Handling_

- [x] 19. Create Seances endpoint tests — Part B: Error Cases
  - [x] 19.1 Test `POST /api/seances` with 29-minute duration returns 400 BadRequest
  - [x] 19.2 Test `POST /api/seances` with off-boundary StartsAt returns 400 BadRequest
  - [x] 19.3 Test `POST /api/seances` with past start time returns 400 BadRequest
  - [x] 19.4 Test `POST /api/seances` with unknown mediumId returns 404 NotFound
  - [x] 19.5 Test `POST /api/seances` with inactive medium returns 409 Conflict
  - [x] 19.6 Test `POST /api/seances` with overlapping time returns 409 Conflict ProblemDetails
  - [x] 19.7 Test `POST /api/seances` with invalid query params returns 400 BadRequest
  - [x] 19.8 Test `POST /api/seances/{id}/cancel` with non-existent id returns 404 NotFound
  - [x] 19.9 Test `POST /api/seances/{id}/cancel` on already-Cancelled seance returns 409 Conflict
  - _Requirements: 2.0, 2.1, 2.2, 2.3, 2.4, Error Handling_

## Phase 7: Blazor Pages & UI

- [ ] 20. Create Mediums Razor page
  - [ ] 20.1 Add `src/SeanceScheduler.Web/Components/Pages/Mediums.razor` at `/mediums` route
  - [ ] 20.2 Display list of all mediums (name, specialty, IsActive) with dark theme styling
  - [ ] 20.3 Add "Create Medium" form with fields: Name, Specialty
  - [ ] 20.4 Implement form handler with `[SupplyParameterFromForm]` and antiforgery
  - [ ] 20.5 Call `POST /api/mediums` and handle response
  - [ ] 20.6 On 201 success, redirect to `/mediums` (PRG pattern)
  - [ ] 20.7 On 409 conflict, keep form values and display error message inline
  - [ ] 20.8 Display validation error messages linked via `aria-describedby` for accessibility
  - [ ] 20.9 Add "Deactivate" button for each active medium; POST to `/api/mediums/{id}/deactivate` and refresh
  - [ ] 20.10 Use spooky UI copy: form title "Summon a New Medium", list title "Conjured Mediums", etc.
  - [ ] 20.11 Ensure WCAG AA contrast (4.5:1) for all text
  - _Requirements: 1.0, 1.1, 1.2, 1.3, 1.4, US-8, 3.0, UI Styling and Navigation_

- [ ] 21. Create Schedule Razor page
  - [ ] 21.1 Add `src/SeanceScheduler.Web/Components/Pages/Schedule.razor` at `/schedule` route
  - [ ] 21.2 Fetch seances from `GET /api/seances` on page load
  - [ ] 21.3 Group seances by date (e.g., "Monday, Dec 2") and display each group
  - [ ] 21.4 For each seance, display: medium name, client name, start/end times in readable format
  - [ ] 21.5 Add filter form (GET-based, no antiforgery) to filter seances by medium via `?mediumId=`
  - [ ] 21.6 Add cancel button for each seance; POST to `/api/seances/{id}/cancel` and refresh
  - [ ] 21.7 Show error messages if cancellation fails (e.g., 409 already cancelled)
  - [ ] 21.8 Use spooky UI copy: page title "The Summoned Appointments", column headers "Spirit Medium", etc.
  - [ ] 21.9 Ensure dark theme styling and WCAG AA contrast
  - _Requirements: 2.0, 2.1, 2.2, 2.3, 2.4, US-9, 3.0, UI Styling and Navigation_

- [ ] 22. Create Schedule New Razor page
  - [ ] 22.1 Add `src/SeanceScheduler.Web/Components/Pages/ScheduleNew.razor` at `/schedule/new` route
  - [ ] 22.2 Fetch available mediums from `GET /api/mediums` on page load; show only IsActive=true
  - [ ] 22.3 Implement booking form with fields: Medium (dropdown), ClientName, StartsAt (date+time), EndsAt (date+time)
  - [ ] 22.4 Use `@formname("BookSeance")` and `[SupplyParameterFromForm]` for server-side validation
  - [ ] 22.5 Include `<AntiforgeryToken />` in form
  - [ ] 22.6 Implement form handler calling `POST /api/seances`
  - [ ] 22.7 On 201 success, redirect to `/schedule` (PRG pattern)
  - [ ] 22.8 On 409 conflict (overlapping seance), keep form values and display conflict message inline WITHOUT redirect
  - [ ] 22.9 On 400 validation error, display validation messages for each field
  - [ ] 22.10 Link validation error messages to form fields via `aria-describedby`; use unique IDs for each field-error pair
  - [ ] 22.11 Style form with dark theme, ensure WCAG AA contrast (4.5:1)
  - [ ] 22.12 Use spooky UI copy: page title "Book a Seance", button text "Summon This Seance", etc.
  - [ ] 22.13 Test manual form submission and verify aria-describedby is correctly set
  - _Requirements: 2.0, 2.1, 2.2, 2.3, 2.4, US-10, 3.0, UI Styling and Navigation_

- [ ] 23. Update MainLayout and navigation
  - [ ] 23.1 Add navigation links in `src/SeanceScheduler.Web/Components/Layout/MainLayout.razor`:
    - [ ] 23.1.1 Link to `/mediums` labeled "Mediums" or "Summon a Medium"
    - [ ] 23.1.2 Link to `/schedule` labeled "Schedule" or "View the Schedule"
    - [ ] 23.1.3 Link to `/schedule/new` labeled "Book a Seance" or "Summon a Seance"
  - [ ] 23.2 Ensure links integrate with existing dark theme navigation
  - [ ] 23.3 Test navigation between pages
  - _Requirements: US-12, UI Styling and Navigation_

- [ ] 24. Add CSS for seance booking pages
  - [ ] 24.1 Review `src/SeanceScheduler.Web/wwwroot/css/app.css` for existing dark theme variables
  - [ ] 24.2 Add styles for seance list (table or card layout), form fields, buttons, error messages as needed
  - [ ] 24.3 Ensure all text has WCAG AA contrast (4.5:1 minimum for body text)
  - [ ] 24.4 Test colors with contrast checker or WAVE tool
  - _Requirements: US-12, 3.0, UI Styling and Navigation_

## Phase 8: UI Form Tests

- [ ] 25. Create Mediums page tests
  - [ ] 25.1 Add `tests/SeanceScheduler.Tests/Features/MediumsPageTests.cs` using TestDatabaseFixture
  - [ ] 25.2 Test creating a medium with valid data succeeds, form values are submitted, and page redirects to `/mediums`
  - [ ] 25.3 Test duplicate name submission shows conflict error message and keeps form values (no redirect)
  - [ ] 25.4 Test missing name shows validation error message and keeps other form values
  - [ ] 25.5 Test deactivate button on active medium makes POST to `/api/mediums/{id}/deactivate` and updates UI
  - _Requirements: US-8_

- [ ] 26. Create Schedule New page tests
  - [ ] 26.1 Add `tests/SeanceScheduler.Tests/Features/ScheduleNewPageTests.cs` using TestDatabaseFixture
  - [ ] 26.2 Test valid booking form submission creates seance, redirects to `/schedule` (PRG)
  - [ ] 26.3 Test 409 conflict response (overlapping seance) keeps form values and shows conflict message
  - [ ] 26.4 Test missing antiforgery token returns 400
  - [ ] 26.5 Test validation error (e.g., invalid duration) shows error message linked via aria-describedby
  - [ ] 26.6 Test aria-describedby correctly links error messages to form fields (verify attribute is present and matches error ID)
  - _Requirements: US-10_

- [ ] 27. Create Schedule page tests
  - [ ] 27.1 Add `tests/SeanceScheduler.Tests/Features/SchedulePageTests.cs` using TestDatabaseFixture
  - [ ] 27.2 Test seances are grouped by date and displayed correctly
  - [ ] 27.3 Test filter by medium via GET param `?mediumId=` works
  - [ ] 27.4 Test cancel button on seance calls POST to cancel and updates display
  - [ ] 27.5 Test cancelled seance no longer appears in list
  - _Requirements: US-9_

## Phase 9: E2E Tests (Playwright)

- [ ] 28. Set up Playwright E2E test infrastructure
  - [ ] 28.1 Create `tests/SeanceScheduler.E2E/` directory
  - [ ] 28.2 Create `tests/SeanceScheduler.E2E/SeanceScheduler.E2E.csproj` with Playwright NuGet reference
  - [ ] 28.3 Create `tests/SeanceScheduler.E2E/Fixtures/WebApplicationFixture.cs` using WebApplicationFactory with Kestrel listening on port 0
  - [ ] 28.4 Spawn Chromium browser via Playwright and connect to the app URL
  - [ ] 28.5 Ensure database is cleaned between tests
  - _Requirements: Testing Strategy, E2E Tests_

- [ ] 29. Create E2E booking flow test
  - [ ] 29.1 Add `tests/SeanceScheduler.E2E/BookingFlowTests.cs`
  - [ ] 29.2 Test: Book a seance via UI, verify it appears in schedule list
    - [ ] 29.2.1 Navigate to `/schedule/new`
    - [ ] 29.2.2 Fill form (medium, client name, times)
    - [ ] 29.2.3 Submit form
    - [ ] 29.2.4 Verify redirect to `/schedule`
    - [ ] 29.2.5 Verify seance appears in list
  - [ ] 29.3 Test: Attempt to book overlapping seance, see conflict message
    - [ ] 29.3.1 Book a seance for 14:00–14:30 on medium M1
    - [ ] 29.3.2 Attempt to book 14:15–14:45 on same medium
    - [ ] 29.3.3 Verify 409 conflict message is displayed
    - [ ] 29.3.4 Verify form values are retained
  - [ ] 29.4 Test: Cancel a seance, verify removal from schedule
    - [ ] 29.4.1 Book a seance
    - [ ] 29.4.2 On `/schedule`, click cancel button for that seance
    - [ ] 29.4.3 Verify seance no longer appears in list
  - [ ] 29.5 Test: Navigate to non-existent URL, verify 404 page displays
    - [ ] 29.5.1 Navigate to `/no-such-page`
    - [ ] 29.5.2 Verify 404 page displays with appropriate message
  - _Requirements: Testing Strategy, E2E Tests_

## Phase 10: Integration & Verification

- [ ] 30. Build and run all tests
  - [ ] 30.1 Run `dotnet build` to verify no compilation errors
  - [ ] 30.2 Run `dotnet test` to verify all unit, integration, and UI tests pass
  - [ ] 30.3 Run E2E tests: `dotnet test tests/SeanceScheduler.E2E/`
  - [ ] 30.4 Verify test coverage includes all scenarios from requirements
  - [ ] 30.5 Verify no warnings are treated as errors (TreatWarningsAsErrors=true)
  - _Requirements: 1.0, 2.0, All_

- [ ] 31. Run app locally and verify UI
  - [ ] 31.1 Run `dotnet run --project src/SeanceScheduler.Web`
  - [ ] 31.2 Navigate to `/mediums`, create a medium, verify it appears
  - [ ] 31.3 Navigate to `/schedule/new`, book a seance with the medium
  - [ ] 31.4 Verify seance appears on `/schedule`
  - [ ] 31.5 Test cancelling seance from `/schedule`
  - [ ] 31.6 Test duplicate booking conflict (book over existing seance)
  - [ ] 31.7 Test deactivating a medium; verify new bookings fail with 409
  - _Requirements: 1.0, 2.0, All_

- [ ] 32. Verify dark theme styling and accessibility
  - [ ] 32.1 Use browser DevTools to verify colors meet WCAG AA (4.5:1) contrast
  - [ ] 32.2 Use accessibility checker (e.g., Wave, Axe) on each page
  - [ ] 32.3 Verify aria-describedby is correctly linked on form fields with errors
  - [ ] 32.4 Verify spooky copy appears in UI text only, not in code identifiers
  - _Requirements: US-12, 3.0_

- [ ] 33. Document API in OpenAPI/Swagger (optional, nice-to-have)
  - [ ] 33.1 If desired, add Swagger/OpenAPI documentation for seance booking endpoints
  - [ ] 33.2 Verify endpoints are discoverable at `/swagger` or `/api/docs`
  - _Requirements: API Specification_

- [ ] 34. Final code review and cleanup
  - [ ] 34.1 Review all code for style consistency with Items reference implementation
  - [ ] 34.2 Verify all services follow async/await pattern with CancellationToken
  - [ ] 34.3 Verify all logging uses ILogger<T>
  - [ ] 34.4 Verify all timestamps are DateTime UTC
  - [ ] 34.5 Verify nullable reference types are used (Nullable=enable)
  - [ ] 34.6 Remove any temporary test code or debug logging
  - [ ] 34.7 Verify no secrets or credentials in code
  - _Requirements: All_

## Summary

This implementation plan delivers a complete seance booking feature with:
- **Data models** for Mediums and Seances with proper relationships and constraints
- **Service layer** with comprehensive validation and transaction-based concurrency control
- **API endpoints** for all CRUD operations with error handling
- **Blazor UI pages** for mediums and seance booking with dark theme, spooky copy, and accessibility
- **Comprehensive tests** covering service layer, endpoint integration, UI forms, and E2E scenarios
- **Migration** to persist the new schema

Each task is small enough to complete in a single agent session. Never weaken assertions or skip validation tests.
