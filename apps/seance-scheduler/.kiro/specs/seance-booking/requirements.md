# SeanceScheduler: Seance Booking Feature — Requirements

## Overview

SeanceScheduler enables a small spiritualist parlour to schedule seances with available mediums without double-booking. Users can register mediums, view the schedule, and book or cancel seances while the system ensures no medium is double-booked.

## User Stories

### Medium Management

**US-1: Register a Medium**
- As a parlour administrator
- I want to register a new medium with their name and specialty
- So that I can track available practitioners

**Acceptance Criteria:**
- GIVEN I submit a form with a medium's name (1–100 characters)
- WHEN the system processes the registration
- THE SYSTEM SHALL create a medium record with that name, optional specialty (max 200 characters), `IsActive=true`, and a UTC creation timestamp
- THE SYSTEM SHALL trim whitespace from name and specialty
- THE SYSTEM SHALL enforce unique case-insensitive medium names; duplicate submissions SHALL return 409 Conflict

**US-2: Deactivate a Medium**
- As a parlour administrator
- I want to mark a medium as inactive
- So that I can retire practitioners without deleting their history

**Acceptance Criteria:**
- GIVEN a request to deactivate an active medium
- WHEN the system processes the deactivation
- THE SYSTEM SHALL set `IsActive` to `false` for that medium
- THE SYSTEM SHALL allow inactive mediums to remain in the schedule as historical records; new seances SHALL NOT be bookable with inactive mediums (409 Conflict)

**US-3: List and View Mediums**
- As a parlour administrator or customer
- I want to see all available mediums and their details
- So that I can understand who is available to perform seances

**Acceptance Criteria:**
- GIVEN a request to list mediums
- WHEN the system retrieves the list
- THE SYSTEM SHALL return all mediums (active or inactive) ordered by name
- GIVEN a request for a specific medium by ID
- WHEN the medium exists
- THE SYSTEM SHALL return that medium's details with status 200 OK
- GIVEN a request for a non-existent medium ID
- WHEN the request is processed
- THE SYSTEM SHALL respond with 404 Not Found

### Seance Booking

**US-4: Book a Seance with a Medium**
- As a customer
- I want to book a seance appointment with an available medium
- So that I can schedule a spiritual session

**Acceptance Criteria:**
- GIVEN I provide a medium ID, client name (1–100 characters), and time range (start and end times in UTC)
- WHEN the system validates the booking
- THE SYSTEM SHALL check the duration is 30 to 240 minutes and is a multiple of 15 minutes
- THE SYSTEM SHALL verify the medium exists and is active; if not, respond 404 (unknown) or 409 (inactive)
- THE SYSTEM SHALL verify no existing Scheduled seance overlaps the requested time for that medium using half-open intervals [StartsAt, EndsAt); back-to-back seances are allowed; if there is an overlap, respond 409 Conflict
- THE SYSTEM SHALL verify the start time falls on a 15-minute boundary (e.g., 14:00, 14:15, 14:30, 14:45)
- THE SYSTEM SHALL verify the start time is in the future (relative to injected TimeProvider); if in the past, respond 400 Bad Request
- GIVEN all validations pass
- THE SYSTEM SHALL create a Seance record with status `Scheduled`, `ClientName`, `MediumId`, UTC times, and UTC creation timestamp
- THE SYSTEM SHALL respond with 201 Created and the seance record

**US-5: Prevent Double-Booking Through Concurrency**
- As a system operator
- I want to ensure that two simultaneous conflicting booking requests result in exactly one success and one conflict
- So that the schedule remains correct under concurrent load

**Acceptance Criteria:**
- GIVEN multiple concurrent requests for overlapping seance slots with the same medium
- WHEN requests are processed simultaneously (not sequentially)
- THE SYSTEM SHALL guarantee that exactly one request receives 201 Created and all others receive 409 Conflict
- THE SYSTEM SHALL provide this guarantee even with multiple processes (not relying on in-process locking alone)

**US-6: Query Seances**
- As a parlour administrator or customer
- I want to view upcoming seances with optional filtering
- So that I can review the schedule and check availability

**Acceptance Criteria:**
- GIVEN a request to list seances with optional filters (`mediumId`, `from` date, `to` date)
- WHEN the system retrieves seances
- THE SYSTEM SHALL return seances ordered by `StartsAt` ascending, then by `Id` ascending
- THE SYSTEM SHALL apply all filters: if `mediumId` is provided, only seances for that medium; if `from` or `to` are provided, only seances within that date range
- GIVEN a request for seances with invalid query parameters
- WHEN the system processes the request
- THE SYSTEM SHALL respond with 400 Bad Request and validation messages

**US-7: Cancel a Seance**
- As a customer or administrator
- I want to cancel a booked seance
- So that I can free up the medium's time for other clients

**Acceptance Criteria:**
- GIVEN a request to cancel a seance with a valid ID
- WHEN the seance status is `Scheduled`
- THE SYSTEM SHALL update the seance status to `Cancelled` and set `CancelledAt` to the current UTC time
- THE SYSTEM SHALL respond with 200 OK and the updated seance record
- GIVEN a request to cancel a seance that is already `Cancelled`
- WHEN the request is processed
- THE SYSTEM SHALL respond with 409 Conflict
- GIVEN a request to cancel a non-existent seance
- WHEN the request is processed
- THE SYSTEM SHALL respond with 404 Not Found

## UI Requirements

### Mediums Page (`/mediums`)

**US-8: Manage Mediums via UI**
- As a parlour administrator
- I want a page to view, add, and deactivate mediums
- So that I can manage the roster without using the API directly

**Acceptance Criteria:**
- GIVEN I visit `/mediums`
- WHEN the page loads
- THE SYSTEM SHALL display a list of all mediums with their names, specialties, and active status
- THE SYSTEM SHALL provide a "Create Medium" form with fields for name and specialty
- THE SYSTEM SHALL provide a "Deactivate" button (or similar) for each active medium
- THE SYSTEM SHALL show spooky UI copy (e.g., "Summon a Medium" for the form heading, "Conjured Mediums" for the list title)
- THE SYSTEM SHALL validate form input and display error messages (name required, length constraints, unique name conflict)
- GIVEN form submission with valid data
- WHEN the submission succeeds
- THE SYSTEM SHALL redirect to `/mediums` and display a success message
- GIVEN duplicate name submission
- WHEN the system detects the conflict
- THE SYSTEM SHALL keep the form values, display the conflict message, and not redirect

### Schedule Pages (`/schedule` and `/schedule/new`)

**US-9: View Upcoming Seances by Day**
- As a customer or administrator
- I want to see upcoming seances grouped by day
- So that I can understand the medium's availability at a glance

**Acceptance Criteria:**
- GIVEN I visit `/schedule`
- WHEN the page loads
- THE SYSTEM SHALL display seances grouped by date (e.g., "Monday, Dec 2" with a list of seances for that day)
- THE SYSTEM SHALL show the medium name, client name, start and end times for each seance
- THE SYSTEM SHALL provide a filter form (without antiforgery requirement, GET-based) to filter by medium
- THE SYSTEM SHALL show only Scheduled seances (exclude Cancelled)
- THE SYSTEM SHALL show cancel buttons for each seance

**US-10: Book a Seance via UI**
- As a customer
- I want to book a seance using a form
- So that I can schedule an appointment without calling the API directly

**Acceptance Criteria:**
- GIVEN I visit `/schedule/new`
- WHEN the page loads
- THE SYSTEM SHALL display a booking form with fields for medium (dropdown), client name, start date/time, and end date/time
- THE SYSTEM SHALL use Blazor's `@formname` directive and validate on the server with `[SupplyParameterFromForm]`
- THE SYSTEM SHALL protect the form with antiforgery tokens
- THE SYSTEM SHALL link validation error messages to form fields using `aria-describedby` and display them accessibly
- GIVEN form submission with valid data
- WHEN the submission succeeds
- THE SYSTEM SHALL create the seance and redirect to `/schedule` (Post-Redirect-Get pattern)
- GIVEN form submission that fails with 409 Conflict (double-booking)
- WHEN the error is detected
- THE SYSTEM SHALL keep the form values in the response, display the conflict message clearly, and not redirect
- THE SYSTEM SHALL ensure the UI meets WCAG AA dark-theme contrast requirements

**US-11: Cancel a Seance via UI**
- As a customer or administrator
- I want to cancel a seance with a form button
- So that I can free up time without using the API

**Acceptance Criteria:**
- GIVEN I am viewing a seance on `/schedule`
- WHEN I click the cancel button
- THE SYSTEM SHALL display a confirmation (or submit via POST form with antiforgery)
- THE SYSTEM SHALL send a POST request to cancel the seance
- GIVEN the cancellation succeeds
- WHEN the system processes the response
- THE SYSTEM SHALL refresh the page or display the updated schedule
- GIVEN the cancellation fails (e.g., seance already cancelled)
- WHEN the error is received
- THE SYSTEM SHALL display an error message and keep the user on the page

### Styling and Navigation

**US-12: Dark-Themed, Spooky UI**
- As a user
- I want the seance booking pages to follow the app's Halloween theme
- So that the experience is immersive and consistent

**Acceptance Criteria:**
- GIVEN any page in the mediums or seance features
- WHEN the page renders
- THE SYSTEM SHALL use the dark color scheme (backgrounds `#0f0f1a`, sidebar `#1a1a2e`, accents `#e94560`)
- THE SYSTEM SHALL ensure all text meets WCAG AA contrast (4.5:1 for body text)
- THE SYSTEM SHALL include navigation links in the layout to `/mediums`, `/schedule`, and `/schedule/new`
- THE SYSTEM SHALL use spooky copy in UI labels and messages (e.g., "Summon a Medium", "The spirits are calling", "Conjured Seances") but NOT in code

## Data Model

### Mediums Entity

| Field       | Type      | Constraints                      |
|-------------|-----------|----------------------------------|
| `Id`        | int       | Primary key, auto-increment      |
| `Name`      | string    | 1–100, trimmed, unique (case-insensitive), required |
| `Specialty` | string?   | 0–200, optional, trimmed         |
| `IsActive`  | bool      | Required, default `true`         |
| `CreatedAt` | DateTime  | UTC, immutable, server-set       |

### Seances Entity

| Field        | Type      | Constraints                                                 |
|--------------|-----------|-------------------------------------------------------------|
| `Id`         | int       | Primary key, auto-increment                                 |
| `MediumId`   | int       | Foreign key to `Mediums.Id`, required                       |
| `ClientName` | string    | 1–100, trimmed, required                                    |
| `StartsAt`   | DateTime  | UTC, on 15-minute boundary, in future, required             |
| `EndsAt`     | DateTime  | UTC, in future, > StartsAt, required                        |
| `Status`     | string    | Enum: `"Scheduled"` or `"Cancelled"`, default `"Scheduled"` |
| `CreatedAt`  | DateTime  | UTC, immutable, server-set                                  |
| `CancelledAt`| DateTime? | UTC, set only when Status = `"Cancelled"`, nullable        |

**Relationships:**
- Seance.MediumId → Medium.Id (one-to-many; a medium has many seances)

## API Specification

### Mediums Endpoints

**GET /api/mediums**
- Returns a list of all mediums (active and inactive), ordered by name
- Response: 200 OK, `List<MediumDto>`

**GET /api/mediums/{id:int}**
- Returns a single medium by ID
- Response: 200 OK, `MediumDto` on success; 404 Not Found if not found

**POST /api/mediums**
- Creates a new medium
- Request: `{ "name": "string", "specialty": "string?" }`
- Response: 201 Created with `Location` header; 400 Bad Request on validation failure; 409 Conflict if name is not unique (case-insensitive)

**POST /api/mediums/{id:int}/deactivate**
- Deactivates an active medium
- Response: 200 OK with updated medium; 404 Not Found if medium does not exist

### Seances Endpoints

**GET /api/seances**
- Lists seances with optional filtering
- Query parameters: `mediumId` (int?), `from` (DateTime?), `to` (DateTime?)
- Response: 200 OK, `List<SeanceDto>` ordered by StartsAt, then Id; 400 Bad Request on invalid parameters

**POST /api/seances**
- Creates a new seance
- Request: `{ "mediumId": int, "clientName": "string", "startsAt": DateTime, "endsAt": DateTime }`
- Response: 201 Created with `Location` header; 400 Bad Request on validation; 404 Not Found if medium does not exist; 409 Conflict if medium inactive or seance overlaps existing

**POST /api/seances/{id:int}/cancel**
- Cancels a seance
- Response: 200 OK with updated seance; 404 Not Found if seance does not exist; 409 Conflict if already cancelled

## Error Handling

All errors follow [RFC 7807 Problem Details](https://tools.ietf.org/html/rfc7807) format in JSON:

```json
{
  "type": "https://example.com/errors/conflict",
  "title": "Conflict",
  "status": 409,
  "detail": "Medium is inactive or seance overlaps existing booking",
  "instance": "/api/seances"
}
```

**Common Error Codes:**
- `400 Bad Request`: Validation failed (invalid duration, not on 15-min boundary, in past, name too long, etc.)
- `404 Not Found`: Resource not found (medium, seance)
- `409 Conflict`: Business rule violated (duplicate name, inactive medium, overlapping seance, already cancelled)

## Testing Strategy

### Service Layer Tests

- Validate duration constraints (29, 30, 240, 241 minutes)
- Verify 15-minute boundary enforcement
- Confirm past start time rejection
- Ensure back-to-back seances are allowed
- Test overlap detection (start, end, full containment scenarios)
- Confirm cancelled seances do not block new bookings
- Validate unique name enforcement (case-insensitive)
- Verify inactive medium rejection

### Endpoint Tests

- Full happy path for create, list, and cancel operations
- Validation error responses (400)
- Not found responses (404)
- Conflict responses (409)
- Concurrency test: 20+ simultaneous overlapping requests yield exactly 1 success and N-1 failures

### UI Tests (Blazor Form)

- Successful seance booking with redirect
- 409 conflict message shown with form values retained
- Missing antiforgery token returns 400
- Form field validation messages display and link via `aria-describedby`

### E2E Tests (Playwright)

- Book a seance, verify it appears in schedule
- Attempt to book conflicting time, see conflict message
- Cancel a seance, verify removal from schedule
- Navigate to 404 page, verify not-found message

## Constraints

- **Concurrency:** SQLite uses SERIALIZABLE isolation by default; overlap checks must run inside a single transaction with explicit write-lock acquisition (e.g., `BEGIN IMMEDIATE`) to prevent race conditions. The write lock is a database file lock, so this holds across processes sharing one local DB file; network filesystems or separate DB copies are out of scope (PostgreSQL exclusion constraint would be the path).
- **Timezone:** All times stored and returned in UTC; client displays responsible for localization.
- **Future work:** Authentication, authorization (admin vs. customer roles), email notifications, reporting.
