# Ice Arena API & Component Documentation

This guide documents every public type, method, and component exposed by the Ice Arena solution (WinForms clients plus the JSON server). It is intended for developers extending the product, writing automated tests, or integrating additional tooling.

---

## 1. Architecture Overview

- **WinForms client (`IceArena.Client`)** – entry form `Form1` handles authentication and launches either the customer portal (`ClientForm`) or the admin workspace (`AdminForm`). Each functional area (users, schedule, analytics, support, bookings) lives in its own Form/UserControl.
- **Server (`IceArena.Server`)** – a lightweight TCP listener (JSON over sockets) implemented in `Program.cs`. Clients send `{ "Command": "<name>", ... }` payloads; the server routes to command handlers that query SQL Server (`Ice_Arena` DB).
- **Shared contracts** – lightweight DTOs in `DataModels.cs`, `BookingForm.cs`, and on the server (`User`).
- **Security** – passwords are encrypted client-side via `EncryptionHelper` before transport; the server decrypts via `ServerEncryptionHelper` and stores hashes (SHA256 + Base64).

---

## 2. Data Contracts & Lightweight Components

### 2.1 `Booking`, `Ticket`, `Review` (`IceArena.Client/DataModels.cs`)

| Type    | Public Members | Notes |
|---------|----------------|-------|
| `Booking` | `Id`, `UserId`, `BookingDate`, `Status`, `Tickets` (`List<Ticket>`), `Day`, `Date`, `TimeSlot`, `NeedSkates`, `SkateSize`, `SkateType`, `ScheduleId` plus computed properties `TotalCost`, `AdultTickets`, `ChildTickets`, `SeniorTickets`, `TotalTickets`. | Acts as the in-memory aggregate for reservations returned by both customer & admin screens. Computed props assume `Tickets` is populated. |
| `Ticket` | `Id`, `BookingId`, `Type`, `Quantity`, `Price` | Used in booking grids and ticket creation RPCs. |
| `Review` | `Id`, `UserId`, `Rating`, `Text`, `Date`, `IsApproved` | Consumed by profile & support features. |

**Usage Example**

```csharp
var booking = new Booking {
    Id = 42,
    Date = DateTime.Today,
    TimeSlot = "10:00-10:45",
    Tickets = new List<Ticket> {
        new Ticket { Type = "Adult", Quantity = 2, Price = 6m }
    }
};
Console.WriteLine(booking.TotalCost); // 12.00
```

### 2.2 `TicketDto` (`BookingForm.cs`)

Minimal DTO (`Type`, `Quantity`, `Price`) serialized when creating bookings. Use it when crafting manual requests to `create_booking`.

### 2.3 `SupportUser` (`SupportTab.cs`)

Represents a chat participant (`Id`, `Email`, `ToString()` returns email). Used as `ListBox` items inside the admin support tab.

### 2.4 `User` (server-side, `Program.cs`)

`Id`, `Email`, `PasswordHash`, `Role`. Returned to authentication handlers; not shared with the client directly but useful when extending server commands.

### 2.5 UI Primitives

| Component | File | Purpose / How to Use |
|-----------|------|----------------------|
| `ModernButton` | `BookingForm.cs` | Custom rounded `Button` with `BorderRadius` and `HoverColor`. Instantiate instead of `Button` when you need consistent styling. |
| `BufferedFlowLayoutPanel` | `SupportFormcs.cs` | Double-buffered `FlowLayoutPanel` to avoid flicker in chat histories. Drop in place of the standard control. |
| `GraphicsExtensions` / `GraphicsExtension` | `RegisterForm.cs`, `README.cs` (login Form) | Static helpers for drawing rounded rectangles; call `GraphicsExtensions.DrawRoundedRectangle(...)` inside `Paint` handlers. |

---

## 3. Security & Utility Helpers

### 3.1 `EncryptionHelper` (`IceArena.Client/EncryptionHelper.cs`)

- **Methods**: `Encrypt(string plainText)`, `Decrypt(string cipherText)`.
- **Implementation**: AES-CBC with fixed 32-byte key and 16-byte IV.
- **Usage**:

```csharp
var cipher = EncryptionHelper.Encrypt(password);
var request = new { Command = "login", Email = email, Password = cipher };
```

> **Note:** Always check `null` return values—encryption failures are logged to console but not thrown.

### 3.2 `ServerEncryptionHelper` (`IceArena.Server/Program.cs`)

Mirrors the client helper; the server decrypts incoming secrets before hashing and storing passwords.

---

## 4. Client-Side Services

### 4.1 `DatabaseService` (`DatabaseService.cs`)

Wrapper around `ClientForm.SendServerRequest` to centralize booking-related RPCs.

| Method | Description / Parameters | Typical Usage |
|--------|-------------------------|---------------|
| `DatabaseService(ClientForm parent)` | Stores parent form reference so every call reuses the TCP helper. | Instantiate once in forms that need booking CRUD. |
| `Task<int> GetAvailableSeats(int scheduleId)` | Requests `get_schedule`, scans for the matching slot, and returns `AvailableSeats` (falls back to `Capacity` or 50). | `int seats = await db.GetAvailableSeats(slotId);` |
| `Task<bool> DecreaseAvailableSeats(int scheduleId, int count)` | Calls `decrease_available_seats`. Requires server support for the command. | Use immediately after creating tickets. |
| `Task IncreaseAvailableSeats(int scheduleId, int count)` | Calls `increase_available_seats`. | Use when canceling. |
| `Task<int> CreateBooking(int userId, int scheduleId, DateTime bookingDate, string status)` | Sends `create_booking`. Returns server-generated `BookingId` or 0. | After seat validation. |
| `Task<List<(int ticketId, int quantity)>> CreateTickets(int bookingId, List<Ticket> tickets)` | Sends `create_tickets` payload, returns newly created IDs with quantities. | Called from `BookingForm` after booking is created. |
| `Task CreateRental(int ticketId, string skateSize, string skateType)` | Sends `create_rental`. | Extend to attach rental metadata per ticket. |

### 4.2 Socket Helpers

| Owner | Method | Notes |
|-------|--------|-------|
| `ClientForm` | `Task<JsonElement> SendServerRequest(object request)` | Core TCP helper for the customer UI. Serializes anonymous object payloads and returns raw `JsonElement`. Handles reconnects per call. |
| `AdminForm` | `Task<JsonElement> SendServerRequest(object request)` | Identical implementation but scoped to admin features. |
| `ScheduleTab` | `Task<JsonElement> SendServerRequest(object request)` | Adds a 5-second connection timeout before deferring to the same socket workflow. |

**Usage Pattern**

```csharp
var response = await clientForm.SendServerRequest(new { Command = "get_schedule" });
if (response.TryGetProperty("Success", out var s) && s.GetBoolean()) {
    // consume payload
}
```

Handle failures by checking `ValueKind` and `TryGetProperty` before accessing nested members.

---

## 5. Client Shell & Customer-Facing Screens

### 5.1 `Form1` (Login Shell)

- **Constructors**: `Form1()`.
- **Role**: Entry point that paints the gradient background, handles guest login (`BtnGuest_Click`), server availability checks, and admin short-circuit (`admin/admin` credentials).
- **Key Interactions**:
  - Launches `RegisterForm` from the "Регистрация" button.
  - Encrypts passwords before sending `{ Command = "login" }`.
  - Calls `ShowAuthForm()` when child forms close to reset fields.

**Usage:** instantiate `Form1` as the application’s `MainForm`.

### 5.2 `RegisterForm`

- **Constructor**: `RegisterForm()`. Builds a single-card UI with role, email, password, and confirm fields.
- **Workflow**:
  1. Validate email/password rules.
  2. Encrypt password via `EncryptionHelper`.
  3. Call `register` command over TCP.
  4. On success, show confirmation and close dialog.
- **Public Static Helper**: `GraphicsExtensions.DrawRoundedRectangle` for consistent card outlines.

**Example:**

```csharp
using (var form = new RegisterForm()) {
    form.ShowDialog();
}
```

### 5.3 `ClientForm`

- **Constructor**: `ClientForm(string username, int userId, bool isGuestMode = false)`.
- **Key Public Members**:
  - `List<Booking> UserBookings { get; private set; }`
  - `List<Review> UserReviews { get; private set; }`
  - `bool IsGuestMode { get; set; }`
  - `Task<List<Booking>> LoadUserBookingsFromServer()`
  - `Task<JsonElement> SendServerRequest(object request)`
- **Behavior**:
  - Modernizes the booking grid UI, toggles action column depending on guest mode.
  - Handles booking and cancellation via cell clicks.
  - Provides shortcuts to profile (`ProfileForm`), support, and booking forms.

**Usage Tips**:
- Call `LoadScheduleFromServer` after any booking change to refresh the grid.
- Share the `ClientForm` instance with helper dialogs (e.g., pass it into `DatabaseService`, `BookingForm`, `SupportForm`) so they can reuse TCP helpers and current bookings.

### 5.4 `BookingForm`

- **Constructor**: `BookingForm(string day, string date, string time, ClientForm parent, int userId, object dbService, int scheduleId, int availableSeats)`.
- **Responsibilities**:
  - Hosts ticket counters (`numAdult`, `numChild`, `numSenior`), totals, optional skate rental fields.
  - Builds `TicketDto` list and issues `create_booking` and `create_tickets` via TCP (direct `TcpClient` usage).
  - Enforces seat limits and session time checks (e.g., cannot book past sessions).

**How to Launch**

```csharp
var dbService = new DatabaseService(clientForm);
using var form = new BookingForm(day, date, timeSlot, clientForm, userId, dbService, scheduleId, seats);
if (form.ShowDialog() == DialogResult.OK) {
    await clientForm.LoadUserBookingsFromServer();
}
```

### 5.5 `ProfileForm`

- **Constructor**: `ProfileForm(string username, int userId, List<Review> reviews, ClientForm parent)`.
- **What it does**:
  - Fetches bookings (expects `get_user_bookings_with_tickets` server command) and reviews (`get_user_reviews`).
  - Renders statistical sidebar, booking grid with delete button, reviews timeline, and a feedback composer.
  - Launches `SupportForm` directly from the sidebar.
- **Extensibility**: Add new widgets by plugging panels into `mainContainer`. All server interactions go through the parent `ClientForm`.

### 5.6 `SupportForm`

- **Constructor**: `SupportForm(int userId, string username, ClientForm parent)`.
- **Features**:
  - Bi-directional chat over TCP using commands like `get_support_chat` and `send_support_message`.
  - Auto-refresh timer (3 seconds) for new messages.
  - `BufferedFlowLayoutPanel` eliminates redraw flicker.
- **Usage**: instantiate modal from profile or other customer screens:

```csharp
using var support = new SupportForm(currentUserId, currentUserName, clientForm);
support.ShowDialog();
```

---

## 6. Admin Workspace Components

### 6.1 `AdminForm`

- **Constructor**: `AdminForm()`.
- **Key Method**: `Task<JsonElement> SendServerRequest(object request)` – shared by all tabs.
- **Behavior**:
  - Creates tabs for Users, Analytics, Bookings, Schedule, and Support.
  - Passes itself to `SupportTab.SetParent` so support chat can invoke RPCs.

### 6.2 `UsersTab`

- **Constructor**: `UsersTab()`.
- **Capabilities**:
  - SQL-driven grid to list users (connection string points to `Ice_Arena`).
  - Search box, add/edit/delete actions launching `AddEditUserForm`.
  - Weekly stats, registration counters, and day-by-day breakdown.

### 6.3 `AddEditUserForm`

- **Constructor**: `AddEditUserForm(string email = null)` – `email` determines add vs edit mode.
- **Event**: `public event Action UserSaved;` – subscribe to refresh grids after save/delete.
- **Actions**:
  - Create users (validates email uniqueness, password length).
  - Edit role and optionally password.
  - Delete user and cascade delete bookings.
  - Display up to 15 recent bookings in the sidebar activity feed.

**Usage Example**

```csharp
var dialog = new AddEditUserForm(existingEmail);
dialog.UserSaved += LoadUsers;
dialog.ShowDialog();
```

### 6.4 `BookingsTab`

- **Constructor**: `BookingsTab()`.
- **Features**:
  - `DataGridView` showing booking records joined with tickets (via SQL).
  - Status filter, confirm/complete/cancel/delete actions mapped to SQL updates.
  - CSV export (`SaveFileDialog`), stats cards for totals, statuses, revenue, last 7 days.

### 6.5 `AnalyticsTab`

- **Constructor**: `AnalyticsTab()`.
- **Public Surface**: relies solely on constructor, but exposes actions through UI buttons for load/refresh/export.
- **Functionality**:
  - Loads arena metrics via SQL (income, attendance, electricity, notes).
  - Charts selected metric with `ZedGraphControl`.
  - Opens `AddEditMetricForm` for manual entry.

### 6.6 `AddEditMetricForm`

- **Constructor**: `AddEditMetricForm(DateTime? date = null)`.
- **Behavior**: When `date` provided, loads existing record and locks the `DateTimePicker`. Uses SQL `MERGE` to insert/update metrics; includes hint tooltips and form clearing.

### 6.7 `ScheduleTab`

- **Constructor**: `ScheduleTab()`.
- **Public API**: `Task<JsonElement> SendServerRequest(object request)` – to fetch/update schedules.
- **Features**:
  - Weekly schedule grid with color-coded status, action buttons (add/edit/delete/refresh).
  - Dialog builder `CreateStyledEditForm` for editing schedule rows.

### 6.8 `SupportTab`

- **Constructor**: `SupportTab()`.
- **Method**: `SetParent(AdminForm parent)` – must be called immediately after instantiation to enable server RPCs.
- **Behavior**:
  - Left panel: user list (`SupportUser`), refresh timer, owner-drawn rows.
  - Right panel: mirrored chat UI for administrators; uses same chat commands as `SupportForm` but marks admin vs user messages accordingly.

---

## 7. Server JSON API (`IceArena.Server/Program.cs`)

All commands are invoked by sending a UTF-8 JSON payload over TCP (`127.0.0.1:8888`). Responses are JSON objects with at least a `Success` boolean.

| Command | Purpose | Request Shape | Response Highlights |
|---------|---------|---------------|---------------------|
| `login` | Validate credentials. Password must be AES-encrypted string. | `{ "Command":"login", "Email":"user@site", "Password":"<cipher>" }` | `{ "Success":true, "Role":"Client|Admin", "UserId":123 }` |
| `register` | Create new user; password encrypted. | `{ "Command":"register", "Email":"...", "Password":"<cipher>", "Role":"Client" }` | `{ "Success":true, "UserId":123 }` |
| `get_schedule` | Fetch upcoming sessions. | `{ "Command":"get_schedule" }` | `{ "Schedule":[{ "Id":1, "Date":"2025-01-01", ... }] }` |
| `book_session` | Reserve a single seat (legacy flow). | `{ "Command":"book_session", "UserId":1, "ScheduleId":3 }` | BookingId and seat decrement side-effect. |
| `create_booking` | Reserve multiple tickets (`TicketsCount`). | `{ "Command":"create_booking", "UserId":1, "ScheduleId":3, "TicketsCount":4 }` | `{ "BookingId":55 }` |
| `get_user_bookings` | Return bookings with schedule info. | `{ "Command":"get_user_bookings", "UserId":1 }` | Array of `{ BookingId, Date, TimeSlot, Status, ... }`. |
| `get_reviews` | Return approved public reviews. | `{ "Command":"get_reviews" }` | `{ "Reviews":[ ... ] }` |
| `add_review` | Persist new review (auto-approved). | `{ "Command":"add_review", "UserId":1, "Rating":5, "Text":"..." }` | `{ "ReviewId":321 }` |
| `get_user_info` | Fetch profile summary. | `{ "Command":"get_user_info", "UserId":1 }` | `User` object. |
| `get_user_profile` | Same as `get_user_info` (alias). | ... | ... |
| `get_user_reviews` | Return all reviews authored by user. | `{ "Command":"get_user_reviews", "UserId":1 }` | `{ "Reviews":[...] }` |
| `cancel_booking` | Mark booking as cancelled and free 1 seat. | `{ "Command":"cancel_booking", "BookingId":99 }` | `{ "Success":true }` |
| `get_arena_metrics` | Top 30 metrics for analytics. | `{ "Command":"get_arena_metrics" }` | `{ "Metrics":[{ "Date":"...", "Income":0, ... }] }` |

> **Client Expectations:** `ProfileForm` also calls `get_user_bookings_with_tickets` and support features call `get_support_chat`, `send_support_message`, `get_active_support_chats`, `send_support_message_as_admin`. Implement corresponding handlers on the server for parity with the UI.

### Example Request/Response

```
Request:
{ "Command": "login", "Email": "user@example.com", "Password": "<AES ciphertext>" }

Response:
{
  "Success": true,
  "Role": "Client",
  "UserId": 42,
  "Email": "user@example.com"
}
```

---

## 8. Common Workflows

### 8.1 Creating a Booking from the Client

```csharp
var db = new DatabaseService(clientForm);
int seats = await db.GetAvailableSeats(scheduleId);
if (seats <= 0) return;

using var bookingDialog = new BookingForm(day, date, slot, clientForm, userId, db, scheduleId, seats);
if (bookingDialog.ShowDialog() == DialogResult.OK) {
    clientForm.UserBookings = await clientForm.LoadUserBookingsFromServer();
    await clientForm.SendServerRequest(new { Command = "get_schedule" }); // refresh grid
}
```

### 8.2 Adding a Metric Entry

```csharp
using var form = new AddEditMetricForm();
if (form.ShowDialog() == DialogResult.OK) {
    analyticsTab.InvokeReload(); // custom helper to call LoadMetrics/UpdateQuickStats
}
```

### 8.3 Registering via Raw JSON (e.g., integration test)

```
// Client side: encrypt password
string cipher = EncryptionHelper.Encrypt("Pa$$w0rd!");
string request = JsonSerializer.Serialize(new {
    Command = "register",
    Email = "qa@example.com",
    Password = cipher,
    Role = "Client"
});
// send over TCP to port 8888
```

---

## 9. Missing or External Dependencies

- **Server-side chat & extended booking commands** are referenced by the client (`get_support_chat`, `get_active_support_chats`, `send_support_message`, `send_support_message_as_admin`, `get_user_bookings_with_tickets`) but not yet implemented in `Program.cs`. Add handlers following the patterns already in place.
- **SQL Server**: connection strings target `DESKTOP-I80K0OH\SQLEXPRESS`; adjust to your environment.
- **ZedGraph**: ensure the WinForms client references `ZedGraph` for analytics.

---

This document should serve as the single source of truth for the Ice Arena project’s public surface. When adding new commands or UI components, extend this file to keep the team aligned.
