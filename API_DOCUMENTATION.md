# Ice Arena - Comprehensive API Documentation

## Table of Contents
1. [Server API](#server-api)
2. [Client Components](#client-components)
3. [Data Models](#data-models)
4. [Utility Classes](#utility-classes)
5. [Usage Examples](#usage-examples)

---

## Server API

### Overview
The server (`IceArena.Server`) is a TCP-based JSON API server running on `127.0.0.1:8888`. It handles all client requests and database operations.

### Connection Details
- **IP Address**: `127.0.0.1`
- **Port**: `8888`
- **Protocol**: TCP with JSON messages
- **Database**: SQL Server (Ice_Arena database)

### API Commands

#### 1. `login`
Authenticates a user and returns user information.

**Request:**
```json
{
  "Command": "login",
  "Email": "user@example.com",
  "Password": "<encrypted_password>"
}
```

**Response (Success):**
```json
{
  "Success": true,
  "Role": "Client",
  "UserId": 123,
  "Email": "user@example.com"
}
```

**Response (Error):**
```json
{
  "Success": false,
  "Error": "Неверный пароль"
}
```

**Usage:**
- Password must be encrypted using `EncryptionHelper.Encrypt()` before sending
- Server decrypts, hashes with SHA256, and compares with stored hash
- Returns user role (Client, Admin) and user ID on success

---

#### 2. `register`
Registers a new user account.

**Request:**
```json
{
  "Command": "register",
  "Email": "newuser@example.com",
  "Password": "<encrypted_password>"
}
```

**Response (Success):**
```json
{
  "Success": true,
  "Message": "Регистрация успешна",
  "UserId": 124,
  "Role": "Client"
}
```

**Response (Error):**
```json
{
  "Success": false,
  "Error": "Пользователь с таким email уже существует"
}
```

**Usage:**
- Email validation is performed server-side
- Password must be encrypted before sending
- Default role is "Client"
- Email must be unique

---

#### 3. `get_schedule`
Retrieves the ice arena schedule.

**Request:**
```json
{
  "Command": "get_schedule"
}
```

**Response (Success):**
```json
{
  "Success": true,
  "Schedule": [
    {
      "Id": 1,
      "Date": "2025-01-15",
      "TimeSlot": "10:00-10:45",
      "BreakSlot": "10:45-11:00",
      "DayOfWeek": "Понедельник",
      "Capacity": 50,
      "AvailableSeats": 35,
      "Status": "ДОСТУПНО"
    }
  ]
}
```

**Usage:**
- Returns all schedule entries from today onwards
- Ordered by date and time slot
- Includes capacity and availability information

---

#### 4. `book_session`
Creates a booking for a single session (legacy method).

**Request:**
```json
{
  "Command": "book_session",
  "UserId": 123,
  "ScheduleId": 1
}
```

**Response (Success):**
```json
{
  "Success": true,
  "Message": "Бронирование успешно создано",
  "BookingId": 456
}
```

**Usage:**
- Automatically decrements available seats by 1
- Uses database transaction for atomicity
- Returns new booking ID

---

#### 5. `create_booking`
Creates a booking with multiple tickets (recommended).

**Request:**
```json
{
  "Command": "create_booking",
  "UserId": 123,
  "ScheduleId": 1,
  "TicketsCount": 3
}
```

**Response (Success):**
```json
{
  "Success": true,
  "Message": "Бронирование успешно создано",
  "BookingId": 457
}
```

**Usage:**
- Supports booking multiple tickets at once
- Validates available seats before booking
- Updates available seats count atomically

---

#### 6. `get_user_bookings`
Retrieves all bookings for a specific user.

**Request:**
```json
{
  "Command": "get_user_bookings",
  "UserId": 123
}
```

**Response (Success):**
```json
{
  "Success": true,
  "Bookings": [
    {
      "BookingId": 456,
      "Date": "2025-01-15",
      "TimeSlot": "10:00-10:45",
      "BreakSlot": "10:45-11:00",
      "DayOfWeek": "Понедельник",
      "Status": "Booked",
      "BookingDate": "2025-01-10 14:30"
    }
  ]
}
```

**Usage:**
- Returns bookings ordered by date (descending)
- Includes booking status and creation date
- Used in user profile and booking management

---

#### 7. `cancel_booking`
Cancels an existing booking.

**Request:**
```json
{
  "Command": "cancel_booking",
  "BookingId": 456
}
```

**Response (Success):**
```json
{
  "Success": true,
  "Message": "Бронирование успешно отменено"
}
```

**Response (Error):**
```json
{
  "Success": false,
  "Error": "Бронирование не найдено"
}
```

**Usage:**
- Sets booking status to "Cancelled"
- Automatically increments available seats
- Uses transaction for data consistency

---

#### 8. `get_reviews`
Retrieves all approved reviews.

**Request:**
```json
{
  "Command": "get_reviews"
}
```

**Response (Success):**
```json
{
  "Success": true,
  "Reviews": [
    {
      "Id": 1,
      "UserEmail": "user@example.com",
      "Rating": 5,
      "Text": "Отличная арена!",
      "Date": "2025-01-10 15:20",
      "IsApproved": true
    }
  ]
}
```

**Usage:**
- Returns only approved reviews (IsApproved = 1)
- Ordered by date (newest first)
- Includes user email for display

---

#### 9. `add_review`
Adds a new review.

**Request:**
```json
{
  "Command": "add_review",
  "UserId": 123,
  "Rating": 5,
  "Text": "Отличная арена!"
}
```

**Response (Success):**
```json
{
  "Success": true,
  "Message": "Отзыв успешно добавлен",
  "ReviewId": 789
}
```

**Response (Error):**
```json
{
  "Success": false,
  "Error": "Рейтинг должен быть от 1 до 5"
}
```

**Usage:**
- Rating must be between 1 and 5
- Reviews are automatically approved (IsApproved = 1)
- Returns new review ID

---

#### 10. `get_user_info`
Retrieves user information by ID.

**Request:**
```json
{
  "Command": "get_user_info",
  "UserId": 123
}
```

**Response (Success):**
```json
{
  "Success": true,
  "User": {
    "Id": 123,
    "Email": "user@example.com",
    "Role": "Client",
    "RegDate": "2025-01-01"
  }
}
```

**Usage:**
- Returns basic user profile information
- Includes registration date
- Used for profile display

---

#### 11. `get_user_profile`
Alternative method to get user profile (same as `get_user_info`).

**Request:**
```json
{
  "Command": "get_user_profile",
  "UserId": 123
}
```

**Response:** Same as `get_user_info`

---

#### 12. `get_user_reviews`
Retrieves all reviews by a specific user.

**Request:**
```json
{
  "Command": "get_user_reviews",
  "UserId": 123
}
```

**Response (Success):**
```json
{
  "Success": true,
  "Reviews": [
    {
      "Id": 1,
      "UserId": 123,
      "Rating": 5,
      "Text": "Отличная арена!",
      "Date": "2025-01-10 15:20",
      "IsApproved": true
    }
  ]
}
```

**Usage:**
- Returns all reviews for the specified user
- Ordered by date (newest first)
- Includes both approved and pending reviews

---

#### 13. `get_arena_metrics`
Retrieves arena analytics metrics (admin only).

**Request:**
```json
{
  "Command": "get_arena_metrics"
}
```

**Response (Success):**
```json
{
  "Success": true,
  "Metrics": [
    {
      "Date": "2025-01-15",
      "Income": 1500.00,
      "Attendance": 120,
      "Electricity": 250.00,
      "Notes": "Regular day"
    }
  ]
}
```

**Usage:**
- Returns last 30 days of metrics
- Includes income, attendance, electricity costs
- Used in admin analytics dashboard

---

#### 14. `test`
Server health check endpoint.

**Request:**
```json
{
  "Command": "test"
}
```

**Response:**
```json
{
  "Success": true,
  "Message": "Сервер работает",
  "Timestamp": "2025-01-15T10:30:00"
}
```

**Usage:**
- Simple connectivity test
- Returns server status and current timestamp

---

## Client Components

### Forms

#### `Form1` (Login Form)
Main authentication form for user login and registration.

**Namespace:** `IceArena.Client`

**Public Methods:**
- `ShowAuthForm()` - Displays the authentication form and resets input fields

**Public Properties:**
- None

**Usage Example:**
```csharp
var loginForm = new Form1();
Application.Run(loginForm);
```

**Features:**
- Modern UI with gradient background
- Email/password authentication
- Guest mode access
- Registration form integration
- Server connectivity check

---

#### `ClientForm`
Main client interface for viewing schedule and managing bookings.

**Namespace:** `IceArena.Client`

**Constructor:**
```csharp
public ClientForm(string username, int userId, bool isGuestMode = false)
```

**Public Methods:**
- `SendServerRequest(object request)` - Sends JSON request to server and returns response
- `LoadUserBookingsFromServer()` - Loads user's bookings from server

**Public Properties:**
- `UserBookings` - List of user's bookings
- `UserReviews` - List of user's reviews
- `IsGuestMode` - Indicates if user is in guest mode

**Usage Example:**
```csharp
var clientForm = new ClientForm("user@example.com", 123, false);
clientForm.Show();
```

**Features:**
- Schedule display with availability
- Booking management
- Profile access
- Guest mode support
- Real-time schedule updates

---

#### `AdminForm`
Administrative panel for managing users, bookings, schedule, and analytics.

**Namespace:** `IceArena.Client`

**Constructor:**
```csharp
public AdminForm()
```

**Public Methods:**
- `SendServerRequest(object request)` - Sends JSON request to server

**Public Properties:**
- None

**Usage Example:**
```csharp
var adminForm = new AdminForm();
adminForm.Show();
```

**Features:**
- User management tab
- Analytics dashboard
- Booking management
- Schedule management
- Support ticket system

**Tabs:**
1. **Users Tab** - Manage user accounts
2. **Analytics Tab** - View arena metrics and statistics
3. **Bookings Tab** - View and manage all bookings
4. **Schedule Tab** - Manage ice arena schedule
5. **Support Tab** - Handle support requests

---

#### `UsersTab`
User management tab for admin panel.

**Namespace:** `IceArena.Client`

**Constructor:**
```csharp
public UsersTab()
```

**Public Methods:**
- None (internal methods only)

**Public Properties:**
- None

**Features:**
- User list display with search functionality
- Add/Edit/Delete user operations
- User statistics display
- Weekly registration statistics
- Direct database connection for admin operations

**Usage:**
Automatically instantiated by `AdminForm` when Users tab is selected.

---

#### `AnalyticsTab`
Analytics and metrics dashboard tab.

**Namespace:** `IceArena.Client`

**Constructor:**
```csharp
public AnalyticsTab()
```

**Public Methods:**
- None (internal methods only)

**Public Properties:**
- None

**Features:**
- Arena metrics display (income, attendance, electricity)
- Interactive charts using ZedGraph
- Report generation
- Data export functionality
- Add/Edit/Delete metrics
- Quick statistics panel
- Period filtering

**Usage:**
Automatically instantiated by `AdminForm` when Analytics tab is selected.

---

#### `BookingsTab`
Booking management tab for viewing all bookings.

**Namespace:** `IceArena.Client`

**Constructor:**
```csharp
public BookingsTab()
```

**Public Methods:**
- None (internal methods only)

**Public Properties:**
- None

**Features:**
- All bookings display
- Status filtering
- Booking statistics
- Export to Excel
- Confirm/Complete/Cancel booking actions
- Delete booking functionality
- Direct database connection

**Usage:**
Automatically instantiated by `AdminForm` when Bookings tab is selected.

---

#### `ScheduleTab`
Schedule management tab for admin.

**Namespace:** `IceArena.Client`

**Constructor:**
```csharp
public ScheduleTab()
```

**Public Methods:**
- None (internal methods only)

**Public Properties:**
- None

**Features:**
- Schedule display and management
- Add/Edit/Delete schedule slots
- Server-based schedule loading
- Refresh functionality
- Modern UI with status indicators

**Usage:**
Automatically instantiated by `AdminForm` when Schedule tab is selected.

---

#### `SupportTab`
Support ticket management tab.

**Namespace:** `IceArena.Client`

**Constructor:**
```csharp
public SupportTab()
```

**Public Methods:**
- `SetParent(AdminForm parent)` - Sets the parent admin form and initializes support system

**Public Properties:**
- None

**Features:**
- Active chat list
- Real-time messaging
- User selection
- Chat history display
- Auto-refresh (3 second interval)
- Modern chat UI with message bubbles

**Usage:**
```csharp
var supportTab = new SupportTab();
supportTab.SetParent(adminForm);
```

**Note:** Must call `SetParent()` after instantiation to initialize the support system.

---

#### `SupportForm`
Client-side support form for users to contact support.

**Namespace:** `IceArena.Client`

**Constructor:**
```csharp
public SupportForm(int userId, string username, ClientForm parent)
```

**Public Methods:**
- None (internal methods only)

**Public Properties:**
- None

**Features:**
- Real-time chat interface
- Auto-refresh (3 second interval)
- Message history display
- Modern chat UI with message bubbles
- Support ticket creation

**Usage:**
```csharp
var supportForm = new SupportForm(123, "user@example.com", parentForm);
supportForm.ShowDialog();
```

---

#### `BufferedFlowLayoutPanel`
Custom FlowLayoutPanel with double buffering for smooth rendering.

**Namespace:** `IceArena.Client`

**Constructor:**
```csharp
public BufferedFlowLayoutPanel()
```

**Public Methods:**
- None

**Public Properties:**
- None

**Usage:**
Used internally by `SupportForm` and `SupportTab` for smooth chat rendering without flickering.

---

### `SupportUser`
Model for support system users.

**Namespace:** `IceArena.Client`

**Properties:**
- `Id` (int) - User ID
- `Email` (string) - User email

**Usage:**
```csharp
var supportUser = new SupportUser
{
    Id = 123,
    Email = "user@example.com"
};
```

---

#### `BookingForm`
Form for creating new bookings with ticket selection.

**Namespace:** `IceArena.Client`

**Constructor:**
```csharp
public BookingForm(string day, string date, string time, 
                   ClientForm parent, int userId, 
                   object dbService, int scheduleId, 
                   int availableSeats)
```

**Public Methods:**
- None

**Public Properties:**
- None

**Usage Example:**
```csharp
var bookingForm = new BookingForm(
    "Понедельник", 
    "2025-01-15", 
    "10:00-10:45",
    parentForm, 
    123, 
    dbService, 
    1, 
    35
);
bookingForm.ShowDialog();
```

**Features:**
- Ticket type selection (Adult, Child, Senior)
- Quantity selection with counters
- Skate rental options
- Real-time price calculation
- Booking confirmation

**Ticket Prices:**
- Adult: 6.00 BYN
- Child: 4.00 BYN
- Senior: 4.00 BYN

---

#### `ProfileForm`
User profile management form.

**Namespace:** `IceArena.Client`

**Constructor:**
```csharp
public ProfileForm(string username, int userId, 
                   List<Review> reviews, ClientForm parent)
```

**Public Methods:**
- None

**Public Properties:**
- None

**Usage Example:**
```csharp
var profileForm = new ProfileForm(
    "user@example.com", 
    123, 
    userReviews, 
    parentForm
);
profileForm.ShowDialog();
```

**Features:**
- Booking history display
- Review management
- Statistics display
- Total cost calculation
- Booking cancellation
- Support form access

---

#### `RegisterForm`
User registration form.

**Namespace:** `IceArena.Client`

**Constructor:**
```csharp
public RegisterForm()
```

**Public Methods:**
- None

**Public Properties:**
- None

**Usage Example:**
```csharp
var registerForm = new RegisterForm();
registerForm.ShowDialog();
```

**Features:**
- Email validation
- Password strength check
- Password confirmation
- Role selection (Client, Trainer)
- Modern UI design

---

### Services

#### `DatabaseService`
Service for database operations (client-side helper).

**Namespace:** Global (no namespace)

**Constructor:**
```csharp
public DatabaseService(ClientForm parent)
```

**Public Methods:**

##### `GetAvailableSeats(int scheduleId)`
Gets available seats for a schedule slot.

**Parameters:**
- `scheduleId` (int) - Schedule slot ID

**Returns:** `Task<int>` - Number of available seats

**Usage:**
```csharp
var dbService = new DatabaseService(parentForm);
int seats = await dbService.GetAvailableSeats(1);
```

---

##### `DecreaseAvailableSeats(int scheduleId, int count)`
Decreases available seats count.

**Parameters:**
- `scheduleId` (int) - Schedule slot ID
- `count` (int) - Number of seats to decrease

**Returns:** `Task<bool>` - Success status

---

##### `IncreaseAvailableSeats(int scheduleId, int count)`
Increases available seats count.

**Parameters:**
- `scheduleId` (int) - Schedule slot ID
- `count` (int) - Number of seats to increase

**Returns:** `Task` - Async operation

---

##### `CreateBooking(int userId, int scheduleId, DateTime bookingDate, string status)`
Creates a new booking.

**Parameters:**
- `userId` (int) - User ID
- `scheduleId` (int) - Schedule slot ID
- `bookingDate` (DateTime) - Booking creation date
- `status` (string) - Booking status

**Returns:** `Task<int>` - New booking ID (0 on failure)

---

##### `CreateTickets(int bookingId, List<Ticket> tickets)`
Creates tickets for a booking.

**Parameters:**
- `bookingId` (int) - Booking ID
- `tickets` (List<Ticket>) - List of tickets to create

**Returns:** `Task<List<(int ticketId, int quantity)>>` - Created ticket IDs and quantities

---

##### `CreateRental(int ticketId, string skateSize, string skateType)`
Creates a skate rental.

**Parameters:**
- `ticketId` (int) - Ticket ID
- `skateSize` (string) - Skate size
- `skateType` (string) - Skate type (Фигурные, Хоккейные)

**Returns:** `Task` - Async operation

---

## Data Models

### `Booking`
Represents a booking made by a user.

**Namespace:** `IceArena.Client`

**Properties:**
- `Id` (int) - Booking ID
- `UserId` (int) - User who made the booking
- `BookingDate` (DateTime) - When the booking was created
- `Status` (string) - Booking status (Booked, Cancelled)
- `Tickets` (List<Ticket>) - List of tickets in this booking
- `Day` (string) - Day of week
- `Date` (DateTime) - Session date
- `TimeSlot` (string) - Session time slot
- `NeedSkates` (bool) - Whether skates are needed
- `SkateSize` (string) - Skate size if needed
- `SkateType` (string) - Skate type if needed
- `ScheduleId` (int) - Schedule slot ID

**Computed Properties:**
- `TotalCost` (decimal) - Total cost of all tickets
- `AdultTickets` (int) - Number of adult tickets
- `ChildTickets` (int) - Number of child tickets
- `SeniorTickets` (int) - Number of senior tickets
- `TotalTickets` (int) - Total number of tickets

**Usage Example:**
```csharp
var booking = new Booking
{
    UserId = 123,
    ScheduleId = 1,
    Date = DateTime.Parse("2025-01-15"),
    TimeSlot = "10:00-10:45",
    Status = "Booked"
};
```

---

### `Ticket`
Represents a ticket in a booking.

**Namespace:** `IceArena.Client`

**Properties:**
- `Id` (int) - Ticket ID
- `BookingId` (int) - Associated booking ID
- `Type` (string) - Ticket type (Adult, Child, Senior)
- `Quantity` (int) - Number of tickets
- `Price` (decimal) - Price per ticket

**Usage Example:**
```csharp
var ticket = new Ticket
{
    BookingId = 456,
    Type = "Adult",
    Quantity = 2,
    Price = 6.00m
};
```

---

### `Review`
Represents a user review.

**Namespace:** `IceArena.Client`

**Properties:**
- `Id` (int) - Review ID
- `UserId` (int) - User who wrote the review
- `Rating` (int) - Rating (1-5)
- `Text` (string) - Review text
- `Date` (DateTime) - Review date
- `IsApproved` (bool) - Whether review is approved

**Usage Example:**
```csharp
var review = new Review
{
    UserId = 123,
    Rating = 5,
    Text = "Отличная арена!",
    Date = DateTime.Now,
    IsApproved = true
};
```

---

### `User` (Server-side)
Server-side user model.

**Namespace:** `IceArena.Server`

**Properties:**
- `Id` (int) - User ID
- `Email` (string) - User email
- `PasswordHash` (string) - Hashed password
- `Role` (string) - User role (Client, Admin)

---

### `TicketDto`
Data transfer object for ticket creation.

**Namespace:** `IceArena.Client`

**Properties:**
- `Type` (string) - Ticket type
- `Quantity` (int) - Quantity
- `Price` (decimal) - Price per ticket

**Usage:**
Used when sending ticket information to the server.

---

## Utility Classes

### `EncryptionHelper`
Provides encryption/decryption functionality for passwords.

**Namespace:** `IceArena.Client`

**Methods:**

#### `Encrypt(string plainText)`
Encrypts a plain text string.

**Parameters:**
- `plainText` (string) - Text to encrypt

**Returns:** `string` - Base64-encoded encrypted text (null on error)

**Usage:**
```csharp
string encrypted = EncryptionHelper.Encrypt("mypassword");
```

**Algorithm:** AES-256 with fixed key and IV

---

#### `Decrypt(string cipherText)`
Decrypts an encrypted string.

**Parameters:**
- `cipherText` (string) - Base64-encoded encrypted text

**Returns:** `string` - Decrypted text (null on error)

**Usage:**
```csharp
string decrypted = EncryptionHelper.Decrypt(encrypted);
```

---

### `ServerEncryptionHelper`
Server-side encryption helper (same functionality as client).

**Namespace:** `IceArena.Server`

**Methods:**
- `Encrypt(string plainText)` - Encrypts text
- `Decrypt(string cipherText)` - Decrypts text

**Note:** Uses the same encryption key as client for compatibility.

---

### `ModernButton`
Custom button control with rounded corners and hover effects.

**Namespace:** `IceArena.Client`

**Properties:**
- `BorderRadius` (int) - Corner radius (default: 20)
- `HoverColor` (Color) - Color on hover

**Usage:**
```csharp
var btn = new ModernButton
{
    Text = "Click Me",
    BackColor = Color.Blue,
    HoverColor = Color.DarkBlue,
    BorderRadius = 10
};
```

---

### `GraphicsExtension`
Extension methods for graphics operations.

**Namespace:** `IceArena.Client`

**Methods:**

#### `DrawRoundedRectangle(Graphics g, Pen pen, int x, int y, int w, int h, int r)`
Draws a rounded rectangle.

**Parameters:**
- `g` (Graphics) - Graphics context
- `pen` (Pen) - Pen for drawing
- `x, y` (int) - Position
- `w, h` (int) - Width and height
- `r` (int) - Corner radius

**Usage:**
```csharp
using (var pen = new Pen(Color.Black))
{
    e.Graphics.DrawRoundedRectangle(pen, 10, 10, 100, 50, 5);
}
```

---

## Usage Examples

### Complete Login Flow

```csharp
// 1. Encrypt password
string password = "mypassword";
string encryptedPassword = EncryptionHelper.Encrypt(password);

// 2. Create login request
var request = new
{
    Command = "login",
    Email = "user@example.com",
    Password = encryptedPassword
};

// 3. Send to server
using (var client = new TcpClient())
{
    await client.ConnectAsync("127.0.0.1", 8888);
    using (var stream = client.GetStream())
    {
        string json = JsonSerializer.Serialize(request);
        byte[] data = Encoding.UTF8.GetBytes(json);
        await stream.WriteAsync(data, 0, data.Length);

        // 4. Read response
        byte[] buffer = new byte[4096];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        string responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        
        var response = JsonSerializer.Deserialize<JsonElement>(responseJson);
        if (response.TryGetProperty("Success", out var success) && success.GetBoolean())
        {
            int userId = response.GetProperty("UserId").GetInt32();
            string role = response.GetProperty("Role").GetString();
            // Handle successful login
        }
    }
}
```

---

### Creating a Booking

```csharp
// 1. Prepare booking request
var request = new
{
    Command = "create_booking",
    UserId = 123,
    ScheduleId = 1,
    TicketsCount = 2
};

// 2. Send request
var response = await clientForm.SendServerRequest(request);

// 3. Check result
if (response.TryGetProperty("Success", out var success) && success.GetBoolean())
{
    int bookingId = response.GetProperty("BookingId").GetInt32();
    MessageBox.Show($"Booking created: {bookingId}");
}
```

---

### Loading Schedule

```csharp
// 1. Request schedule
var request = new { Command = "get_schedule" };
var response = await clientForm.SendServerRequest(request);

// 2. Parse schedule
if (response.TryGetProperty("Success", out var success) && success.GetBoolean())
{
    if (response.TryGetProperty("Schedule", out var scheduleArray))
    {
        foreach (var item in scheduleArray.EnumerateArray())
        {
            int id = item.GetProperty("Id").GetInt32();
            string date = item.GetProperty("Date").GetString();
            string timeSlot = item.GetProperty("TimeSlot").GetString();
            int availableSeats = item.GetProperty("AvailableSeats").GetInt32();
            
            // Process schedule item
        }
    }
}
```

---

### Adding a Review

```csharp
var request = new
{
    Command = "add_review",
    UserId = 123,
    Rating = 5,
    Text = "Great ice arena!"
};

var response = await clientForm.SendServerRequest(request);
if (response.TryGetProperty("Success", out var s) && s.GetBoolean())
{
    MessageBox.Show("Review added successfully!");
}
```

---

### Canceling a Booking

```csharp
var request = new
{
    Command = "cancel_booking",
    BookingId = 456
};

var response = await clientForm.SendServerRequest(request);
if (response.TryGetProperty("Success", out var s) && s.GetBoolean())
{
    MessageBox.Show("Booking canceled!");
}
```

---

## Error Handling

All API responses follow a consistent format:

**Success:**
```json
{
  "Success": true,
  "Message": "Operation successful",
  ...
}
```

**Error:**
```json
{
  "Success": false,
  "Error": "Error message description"
}
```

**Common Error Messages:**
- "Отсутствует Email или Password" - Missing required fields
- "Неверный пароль" - Invalid password
- "Пользователь с таким email уже существует" - Email already registered
- "Нет свободных мест на этот сеанс" - No available seats
- "Бронирование не найдено" - Booking not found
- "Ошибка сервера при входе" - Server error during login

---

## Security Notes

1. **Password Encryption**: All passwords are encrypted client-side using AES-256 before transmission
2. **Password Hashing**: Server hashes passwords with SHA256 before storing in database
3. **Connection**: Server runs on localhost (127.0.0.1) - not exposed to network
4. **Validation**: Server validates all inputs and email formats
5. **Transactions**: Critical operations use database transactions for consistency

---

## Database Schema

### Tables Referenced:
- `Users` - User accounts
- `Schedule` - Ice arena schedule slots
- `Bookings` - User bookings
- `Reviews` - User reviews
- `ArenaMetrics` - Analytics data

---

## Best Practices

1. **Always encrypt passwords** before sending to server
2. **Check Success property** in all API responses
3. **Handle exceptions** when connecting to server
4. **Validate user input** before sending requests
5. **Use async/await** for all server operations
6. **Check available seats** before creating bookings
7. **Handle timeouts** when connecting to server (5 second default)

---

## Version Information

- **Server Version**: 1.0
- **Client Version**: 1.0
- **Protocol Version**: JSON over TCP
- **Last Updated**: 2025-01-15

---

## Support

For issues or questions:
- Check server console for error messages
- Verify database connection
- Ensure server is running on port 8888
- Check network connectivity
- Review error messages in API responses
