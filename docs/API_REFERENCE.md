# IceArena Public API & Component Reference

This document describes every public-facing API, helper, and component that lives in this repository. It is meant to be the single place you can go to understand **what** the project exposes, **how** to call it, and **which UI/class consumes it.**

- Server-side logic lives in `Program.cs` under the `IceArena.Server` namespace and exposes a JSON-over-TCP protocol.
- Client-side logic lives in the `IceArena.Client` namespace and is composed of WinForms screens, services, and helpers.
- The admin tooling (`AdminForm` and the various `*Tab` controls) talks directly to SQL Server, while the customer-facing client talks exclusively to the TCP API.

> **Transport**  
> Unless otherwise noted, every API call is a UTF-8 JSON payload sent over a raw TCP socket to `127.0.0.1:8888`. The request object **must** have a `"Command"` property. The server replies with a JSON object that always includes a `Success` flag and may include additional fields (e.g., `Error`, `Bookings`, `Schedule`, etc.).

---

## 1. Server JSON Commands (`Program.cs`)

### 1.1  Authentication & Profiles

| Command | Purpose | Request Schema | Response Schema |
|---------|---------|----------------|-----------------|
| `login` | Verify credentials, return role and ID. | `{ "Command":"login", "Email":"user@site", "Password":"<AES payload>" }` | `{ "Success":true, "Role":"Client \| Admin", "UserId":1, "Email":"user@site" }` |
| `register` | Create a new client account. | `{ "Command":"register", "Email":"user@site", "Password":"<AES payload>", "Role":"Client" }` | `{ "Success":true, "UserId":123, "Role":"Client", "Message":"Регистрация успешна" }` |
| `get_user_info` | Retrieve basic profile data. | `{ "Command":"get_user_info", "UserId":1 }` | `{ "Success":true, "User":{ "Id":1, "Email":"...", "Role":"...", "RegDate":"yyyy-MM-dd" } }` |
| `get_user_profile` | Same payload as `get_user_info`, intended for profile screens. | Identical to above. | Identical to above. |
| `get_user_reviews` | Fetch the caller’s reviews. | `{ "Command":"get_user_reviews", "UserId":1 }` | `{ "Success":true, "Reviews":[ {"Id":10,"Rating":5,"Text":"...",...} ] }` |

**Usage (client side):**

```csharp
string encrypted = EncryptionHelper.Encrypt(password);
var response = await clientForm.SendServerRequest(new {
    Command = "login",
    Email = email,
    Password = encrypted
});
```

The AES payload must be created with `EncryptionHelper` on the client; `ServerEncryptionHelper` (server-side) will decrypt it before hashing it with SHA256.

### 1.2  Schedule & Booking Lifecycle

| Command | Purpose | Notes |
|---------|---------|-------|
| `get_schedule` | Returns upcoming ice sessions from `Schedule` table with `Id`, `Date`, `TimeSlot`, `Capacity`, `AvailableSeats`, `Status`. | Used by `ClientForm`, `ScheduleTab`, and `DatabaseService`. |
| `book_session` | Reserve a single seat. | Expects `{ "UserId":1, "ScheduleId":5 }`. Decrements `AvailableSeats` by 1 within a SQL transaction. |
| `create_booking` | Creates a booking record (and, on the server, decrements seats by a specified count). | **Current server implementation expects** `TicketsCount` (int). The client (`BookingForm.cs`) currently sends a `Tickets` array; align these before deployment. |
| `get_user_bookings` | Lists basic booking history (date, slot, status). | Returns `BookingId`, `Date`, `TimeSlot`, `BreakSlot`, `Status`, `BookingDate`. Used by `ClientForm`, `ProfileForm`. |
| `cancel_booking` | Sets status to `Cancelled` and returns a seat to the schedule. | Requires `{ "BookingId":123 }`. |
| `get_user_bookings_with_tickets` | **Client expectation only.** `ProfileForm` uses it to build ticket-level rows; implement this on the server using a join with `Tickets`. |
| `decrease_available_seats` / `increase_available_seats` | **Client expectation only.** `DatabaseService` sends these commands for manual seat adjustments. Implement matching handlers server-side if you need that feature. |
| `create_tickets`, `create_rental` | **Client expectation only.** `DatabaseService.CreateTickets/CreateRental` assume server endpoints exist. |

**Sample booking request** (once the server accepts ticket lists):

```json
{
  "Command": "create_booking",
  "UserId": 42,
  "ScheduleId": 7,
  "Tickets": [
    { "Type": "Adult", "Quantity": 2, "Price": 6.0 },
    { "Type": "Child", "Quantity": 1, "Price": 4.0 }
  ]
}
```

### 1.3  Reviews & Analytics

| Command | Purpose | Example Response |
|---------|---------|------------------|
| `get_reviews` | Returns public, approved reviews. | `{ "Success":true, "Reviews":[{"UserEmail":"...","Rating":5,"Text":"...",...}] }` |
| `add_review` | Submit a review. Requires `UserId`, `Rating` (1–5), `Text`. | `{ "Success":true, "ReviewId":99 }` |
| `get_arena_metrics` | Fetch up to 30 rows from `ArenaMetrics`. | `{ "Success":true, "Metrics":[{"Date":"2025-01-01","Income":1234.56,...}] }` |

### 1.4  Health & Misc

| Command | Purpose |
|---------|---------|
| `test` | Returns `{ "Success":true, "Message":"Сервер работает", "Timestamp":"..." }`. |

### 1.5  Support & Chat (Client Expectations)

`SupportTab` and `SupportForm` call the following commands, which you must implement on the server if you want live chat to function:

- `get_active_support_chats`
- `get_chat_history`
- `send_support_message_as_admin`
- `get_support_chat`
- `send_support_message`

Each should accept `TargetUserId` or `UserId` and return/message arrays shaped similarly to the `SupportTab` code (messages contain `Message`, `IsFromUser`, `Date/Timestamp`).

---

## 2. Cryptography & Data Contracts

### 2.1  `EncryptionHelper` (`IceArena.Client`)

| Member | Description | Usage |
|--------|-------------|-------|
| `Encrypt(string plainText)` | AES-256-CBC encrypts a string with fixed key/IV. Returns Base64 payload or `null` on error. | Use before sending passwords to the server. |
| `Decrypt(string cipherText)` | Reverse operation. Mostly used in diagnostics or potential future offline features. | |

### 2.2  `ServerEncryptionHelper` (`Program.cs`)

Mirrors `EncryptionHelper` so that `Program.HandleLogin/HandleRegister` can decrypt passwords before hashing them.

### 2.3  Data Models (`DataModels.cs`)

| Class | Key Members |
|-------|-------------|
| `Booking` | `Id`, `UserId`, `BookingDate`, `Status`, `Tickets` (list), `ScheduleId`, computed `TotalCost`, ticket counters (`AdultTickets`, etc.). |
| `Ticket` | `Id`, `BookingId`, `Type`, `Quantity`, `Price`. |
| `Review` | `Id`, `UserId`, `Rating`, `Text`, `Date`, `IsApproved`. |
| `TicketDto` (`BookingForm.cs`) | DTO used when sending bookings to the server (`Type`, `Quantity`, `Price`). |

These objects are used both by client logic (binding grids) and by `DatabaseService`.

---

## 3. Networking Helpers & Services

### 3.1  `ClientForm.SendServerRequest` (`ClientForm.cs`)

Signature: `public async Task<JsonElement> SendServerRequest(object request)`

- Serialises any anonymous object request to JSON and sends it to `127.0.0.1:8888`.
- Reads the full response stream into a `JsonElement`.
- Returns an empty `{}` JSON element if any exception occurs.

**Usage pattern:**

```csharp
var response = await SendServerRequest(new { Command = "get_schedule" });
if (response.TryGetProperty("Success", out var success) && success.GetBoolean()) {
    // ...
}
```

### 3.2  `AdminForm.SendServerRequest`

Same contract as the client method, but scoped to admin tooling (e.g., analytics, support chat).

### 3.3  `DatabaseService` (`DatabaseService.cs`)

| Method | Purpose | Notes |
|--------|---------|-------|
| `GetAvailableSeats(int scheduleId)` | Calls `get_schedule` and returns `AvailableSeats` for the provided ID. | Falls back to `Capacity` or default of 50. |
| `DecreaseAvailableSeats(int scheduleId, int count)` | Expects `decrease_available_seats` server command. | Currently returns `false` if the response is not `Success`. |
| `IncreaseAvailableSeats(int scheduleId, int count)` | Expects `increase_available_seats`. | Fire-and-forget. |
| `CreateBooking(int userId, int scheduleId, DateTime bookingDate, string status)` | Calls `create_booking`. | Server currently ignores `BookingDate`/`Status` fields; adjust server to use them if required. |
| `CreateTickets(int bookingId, List<Ticket> tickets)` | Sends `create_tickets`. | Returns a list of `(ticketId, quantity)` tuples if the server responds with `Tickets`. |
| `CreateRental(int ticketId, string skateSize, string skateType)` | Sends `create_rental`. | Used when the user opts into skate rental. |

Give `DatabaseService` a reference to the owning `ClientForm` so it can reuse the socket helper:

```csharp
var db = new DatabaseService(clientForm);
int seats = await db.GetAvailableSeats(scheduleId);
```

---

## 4. Client Entry Points & Forms

### 4.1  `ClientForm` (`ClientForm.cs`)

Purpose: main authenticated experience for customers—shows the weekly schedule, handles bookings, opens profile/support screens.

Public surface:

| Member | Description |
|--------|-------------|
| `ClientForm(string username, int userId, bool isGuestMode=false)` | Bootstraps the UI with themeing, event wiring, and asynchronous data loading. |
| `List<Booking> UserBookings { get; private set; }` | Cache of the caller’s bookings. |
| `List<Review> UserReviews { get; private set; }` | Cache of caller reviews (populated by `ProfileForm`). |
| `bool IsGuestMode { get; set; }` | When `true`, disables booking/cancellation actions. |
| `Task<List<Booking>> LoadUserBookingsFromServer()` | Wraps `get_user_bookings`. |
| `Task<JsonElement> SendServerRequest(object request)` | Described above. |

**Usage tips:**

- Instantiate via `new ClientForm(login, userId, isGuest)` and call `Show()` or `ShowDialog()`.  
- Always await `LoadUserBookingsFromServer()` before using `UserBookings`.
- Hooks like `btnBooking.Click` call `ShowBookingForm()` (which in turn opens `BookingForm`).

### 4.2  `AdminForm` (`AdminForm.cs`)

Purpose: central hub for internal staff with tabs for users, analytics, bookings, schedule, and support.

Constructor bootstraps five tabs:

1. `UsersTab` – manages accounts via direct SQL.
2. `AnalyticsTab` – graphs `ArenaMetrics`.
3. `BookingsTab` – moderates bookings, exports CSV.
4. `ScheduleTab` – edits `Schedule` table entries.
5. `SupportTab` – live chat with customers (requires additional server endpoints).

`AdminForm.SendServerRequest` mirrors the client helper, enabling tabs to call the JSON API when necessary.

### 4.3  `RegisterForm`

Standalone window that handles user onboarding:

- Validates email/password locally (`Regex` in `IsValidEmail`).
- Encrypts the password with `EncryptionHelper`.
- Calls the `register` command.
- On success, closes itself.

Instantiate via `new RegisterForm().ShowDialog();`.

### 4.4  `ProfileForm`

Shows per-user analytics (total spend, bookings, reviews) and exposes actions:

- Constructor signature: `ProfileForm(string username, int userId, List<Review> reviews, ClientForm parent)`.
- Calls `get_user_bookings_with_tickets` (implement server support) and `get_user_reviews`.
- Provides inline review submission via `add_review`.
- Opens `SupportForm` through the sidebar button.

### 4.5  `BookingForm`

Modal dialog for selecting ticket counts and optional skate rental.

Constructor signature:  
`BookingForm(string day, string date, string time, ClientForm parent, int userId, object dbService, int scheduleId, int availableSeats)`

- Uses internal `NumericUpDown` controls for counts and custom `ModernButton` for actions.
- Sends `create_booking` (extend server to accept the `Tickets` payload described earlier).
- Validates seat availability before sending.

### 4.6  `SupportForm`

Customer-facing chat UI:

- Constructor: `SupportForm(int userId, string username, ClientForm parent)`.
- Polls `get_support_chat` every 3 seconds and sends `send_support_message`.
- Uses `BufferedFlowLayoutPanel` for smooth scrolling bubbles.

---

## 5. Admin Tabs & Dialogs

### 5.1  `UsersTab`

- Directly connects to SQL Server via `ConnectionString` in the file (update this per environment).
- Exposes CRUD operations:
  - `AddUser()` → opens `AddEditUserForm`.
  - `EditSelectedUser()` → same form in edit mode.
  - `DeleteSelectedUser()` → deletes from `Users` table (with confirmation).
- `UpdateStats()` populates dashboard cards and a weekly registration table.

### 5.2  `ScheduleTab`

- Displays a week of sessions from `Schedule`.
- Buttons trigger `AddScheduleAsync`, `EditScheduleAsync`, `DeleteScheduleAsync`, each opening a modal form with validated inputs.
- Calls `SendServerRequest` for `get_schedule`, `add_schedule`, `update_schedule`, `delete_schedule`.

### 5.3  `BookingsTab`

- Loads booking data via SQL joins.
- Provides status transitions (`ChangeBookingStatus`) for `Booked`, `Confirmed`, `Completed`, `Cancelled`.
- Exports CSV via `ExportToExcel`.
- Update the connection string to point at your SQL Server.

### 5.4  `AnalyticsTab`

- Uses `ZedGraph` plus SQL queries to visualise income, attendance, and energy usage.
- Buttons:
  - `AddMetric()` / `EditSelectedMetric()` → opens `AddEditMetricForm`.
  - `DeleteSelectedMetric()`.
  - `GenerateReport()` → refreshes table + graph.
  - `ExportReport()` → CSV export.

### 5.5  `SupportTab`

- Displays list of active chats and renders message bubbles.
- Requires the admin-side support commands (`get_active_support_chats`, etc.).
- `SetParent(AdminForm parent)` must be called before the tab can send network requests.

### 5.6  `AddEditUserForm`

- Constructor overload: `AddEditUserForm(string email = null)`.
- When editing, populates fields from SQL and exposes a `UserSaved` event so callers can reload their lists.
- Password hashing uses SHA256 directly (hex string).

### 5.7  `AddEditMetricForm`

- Constructor: `AddEditMetricForm(DateTime? date = null)`.
- In edit mode disables date edits and loads the selected row’s data.
- `SaveMetric()` executes a SQL `MERGE` to insert or update the metric for the chosen date.

---

## 6. Custom Controls & Utilities

| Component | File | Purpose |
|-----------|------|---------|
| `ModernButton` | `BookingForm.cs` | Rounded button with hover state. Use anywhere you need a stylised CTA. |
| `BufferedFlowLayoutPanel` | `SupportFormcs.cs` | FlowLayoutPanel with double-buffering to prevent flicker (used in chat views). |
| `GraphicsExtensions.DrawRoundedRectangle` | `RegisterForm.cs` | Extension method for drawing rounded borders (used by registration UI). |

Instantiate `ModernButton` as you would a normal `Button`, then set `BackColor`, `HoverColor`, and `BorderRadius`.

---

## 7. Implementation Notes & Best Practices

1. **Connection Strings** – Both admin tabs and server code currently point at `DESKTOP-I80K0OH\SQLEXPRESS`. Update the constants in `Program.cs`, `UsersTab.cs`, `BookingsTab.cs`, `AnalyticsTab.cs`, and `AddEditMetricForm.cs` to match your environment.
2. **Seat Consistency** – If you introduce multi-ticket bookings, update both `HandleCreateBooking` (server) and `DatabaseService.CreateBooking` so they use the **same** payload (either `TicketsCount` or the richer `Tickets` array).
3. **Support Chat** – Implement the missing server commands or gate the Support tab with feature flags to avoid runtime exceptions.
4. **Error Handling** – All socket helpers default to returning `{}` on failure. Always check `response.ValueKind == JsonValueKind.Object` and `Success` before accessing nested properties.
5. **AES Keys** – The encryption key/IV are hard-coded. If you rotate them, update both `EncryptionHelper` and `ServerEncryptionHelper` simultaneously.
6. **Admin vs. Client Paths** – Admin forms frequently talk straight to SQL; client forms *only* talk to the TCP server. Keep that separation when adding new features so that security reviews stay simple.

With this reference you can extend the API (e.g., add ticket bundles or richer analytics) while knowing exactly which classes and commands you need to touch.
