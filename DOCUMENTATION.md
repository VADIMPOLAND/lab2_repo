# Ice Arena Booking System - API & Component Documentation

> **Version:** 1.0  
> **Last Updated:** December 2025  
> **Platform:** Windows Forms (.NET)

## Table of Contents

1. [System Overview](#1-system-overview)
2. [Architecture](#2-architecture)
3. [Data Models](#3-data-models)
4. [Server API Reference](#4-server-api-reference)
5. [Client Components](#5-client-components)
6. [Admin Panel Components](#6-admin-panel-components)
7. [Utility Classes](#7-utility-classes)
8. [Usage Examples](#8-usage-examples)
9. [Configuration](#9-configuration)

---

## 1. System Overview

The **Ice Arena Booking System** is a client-server application for managing ice skating rink bookings at ПолесГУ (Polessu) University. The system allows:

- **Clients**: Browse schedules, book sessions, manage bookings, leave reviews, contact support
- **Administrators**: Manage users, view analytics, handle bookings, manage schedules, respond to support requests

### Key Features

- 🎫 Online session booking with ticket types (Adult, Child, Senior)
- 📅 Schedule management with availability tracking
- 👥 User management with role-based access (Client, Admin)
- 📊 Analytics and reporting dashboard
- 💬 Real-time support chat system
- 🔐 AES-256 encrypted password transmission

---

## 2. Architecture

### 2.1 System Components

```
┌─────────────────┐         TCP/JSON         ┌─────────────────┐
│  Client App     │ ◄──────────────────────► │  Server App     │
│  (WinForms)     │       Port 8888          │  (Console)      │
└─────────────────┘                          └─────────────────┘
                                                      │
                                                      ▼
                                             ┌─────────────────┐
                                             │  SQL Server     │
                                             │  (Ice_Arena DB) │
                                             └─────────────────┘
```

### 2.2 Communication Protocol

All client-server communication uses **JSON over TCP** on port **8888**.

**Request Format:**
```json
{
    "Command": "command_name",
    "Parameter1": "value1",
    "Parameter2": "value2"
}
```

**Response Format:**
```json
{
    "Success": true,
    "Data": { ... },
    "Error": "error_message_if_failed"
}
```

---

## 3. Data Models

### 3.1 Booking

Represents a user's booking for an ice skating session.

```csharp
namespace IceArena.Client
{
    public class Booking
    {
        public int Id { get; set; }              // Unique booking identifier
        public int UserId { get; set; }          // Associated user ID
        public DateTime BookingDate { get; set; } // When booking was created
        public string Status { get; set; }       // "Booked", "Confirmed", "Cancelled", "Completed"
        public List<Ticket> Tickets { get; set; } // Associated tickets
        public string Day { get; set; }          // Day of week (e.g., "MONDAY")
        public DateTime Date { get; set; }       // Session date
        public string TimeSlot { get; set; }     // Time range (e.g., "10:00-10:45")
        public bool NeedSkates { get; set; }     // Whether skate rental is needed
        public string SkateSize { get; set; }    // Skate size if renting
        public string SkateType { get; set; }    // "Фигурные" or "Хоккейные"
        public int ScheduleId { get; set; }      // Associated schedule slot ID
        
        // Computed properties
        public decimal TotalCost { get; }        // Sum of ticket prices × quantities
        public int AdultTickets { get; }         // Count of adult tickets
        public int ChildTickets { get; }         // Count of child tickets
        public int SeniorTickets { get; }        // Count of senior tickets
        public int TotalTickets { get; }         // Total ticket count
    }
}
```

**Example Usage:**
```csharp
var booking = new Booking
{
    UserId = 123,
    ScheduleId = 456,
    Date = DateTime.Today.AddDays(1),
    TimeSlot = "14:00-14:45",
    Status = "Booked"
};
```

### 3.2 Ticket

Represents tickets within a booking.

```csharp
public class Ticket
{
    public int Id { get; set; }           // Unique ticket identifier
    public int BookingId { get; set; }    // Parent booking ID
    public string Type { get; set; }      // "Adult", "Child", "Senior"
    public int Quantity { get; set; }     // Number of tickets
    public decimal Price { get; set; }    // Price per ticket (BYN)
}
```

**Standard Pricing:**
| Type   | Price (BYN) |
|--------|-------------|
| Adult  | 6.00        |
| Child  | 4.00        |
| Senior | 4.00        |

### 3.3 Review

User reviews for the ice arena.

```csharp
public class Review
{
    public int Id { get; set; }           // Unique review identifier
    public int UserId { get; set; }       // Author's user ID
    public int Rating { get; set; }       // 1-5 star rating
    public string Text { get; set; }      // Review content
    public DateTime Date { get; set; }    // Submission date
    public bool IsApproved { get; set; }  // Admin approval status
}
```

### 3.4 User (Server-side)

```csharp
public class User
{
    public int Id { get; set; }           // Unique user identifier
    public string Email { get; set; }     // User email (login)
    public string PasswordHash { get; set; } // SHA256 hashed password
    public string Role { get; set; }      // "Client" or "Admin"
}
```

---

## 4. Server API Reference

### 4.1 Authentication Commands

#### `login` - User Authentication

Authenticates a user with email and encrypted password.

**Request:**
```json
{
    "Command": "login",
    "Email": "user@example.com",
    "Password": "AES_ENCRYPTED_PASSWORD"
}
```

**Success Response:**
```json
{
    "Success": true,
    "Role": "Client",
    "UserId": 123,
    "Email": "user@example.com"
}
```

**Error Response:**
```json
{
    "Success": false,
    "Error": "Неверный пароль"
}
```

**Example (C#):**
```csharp
string encryptedPassword = EncryptionHelper.Encrypt("mypassword123");
var request = new {
    Command = "login",
    Email = "user@example.com",
    Password = encryptedPassword
};
var response = await clientForm.SendServerRequest(request);
```

---

#### `register` - User Registration

Creates a new user account.

**Request:**
```json
{
    "Command": "register",
    "Email": "newuser@example.com",
    "Password": "AES_ENCRYPTED_PASSWORD",
    "Role": "Client"
}
```

**Success Response:**
```json
{
    "Success": true,
    "Message": "Регистрация успешна",
    "UserId": 456,
    "Role": "Client"
}
```

**Validation Rules:**
- Email must be valid format
- Email must be unique
- Password is required (encrypted on transmission)

---

### 4.2 Schedule Commands

#### `get_schedule` - Get Schedule

Retrieves available schedule slots.

**Request:**
```json
{
    "Command": "get_schedule"
}
```

**Response:**
```json
{
    "Success": true,
    "Schedule": [
        {
            "Id": 1,
            "Date": "2025-12-05",
            "TimeSlot": "10:00-10:45",
            "BreakSlot": "45 мин",
            "DayOfWeek": "Пятница",
            "Capacity": 50,
            "AvailableSeats": 35,
            "Status": "ДОСТУПНО"
        }
    ]
}
```

---

#### `add_schedule` - Add Schedule Slot (Admin)

Creates a new schedule slot.

**Request:**
```json
{
    "Command": "add_schedule",
    "Date": "2025-12-10",
    "TimeSlot": "16:00-16:45",
    "BreakSlot": "45 мин",
    "Capacity": 50,
    "Status": "ДОСТУПНО"
}
```

---

#### `update_schedule` - Update Schedule (Admin)

Modifies an existing schedule slot.

**Request:**
```json
{
    "Command": "update_schedule",
    "Id": 123,
    "Date": "2025-12-10",
    "TimeSlot": "17:00-17:45",
    "BreakSlot": "45 мин",
    "Capacity": 60,
    "Status": "ДОСТУПНО"
}
```

---

#### `delete_schedule` - Delete Schedule (Admin)

Removes a schedule slot.

**Request:**
```json
{
    "Command": "delete_schedule",
    "Id": 123
}
```

---

### 4.3 Booking Commands

#### `book_session` - Quick Book Session

Creates a simple single-ticket booking.

**Request:**
```json
{
    "Command": "book_session",
    "UserId": 123,
    "ScheduleId": 456
}
```

**Response:**
```json
{
    "Success": true,
    "Message": "Бронирование успешно создано",
    "BookingId": 789
}
```

---

#### `create_booking` - Create Booking with Tickets

Creates a booking with multiple tickets.

**Request:**
```json
{
    "Command": "create_booking",
    "UserId": 123,
    "ScheduleId": 456,
    "TicketsCount": 3,
    "Tickets": [
        {"Type": "Adult", "Quantity": 2, "Price": 6.00},
        {"Type": "Child", "Quantity": 1, "Price": 4.00}
    ]
}
```

**Response:**
```json
{
    "Success": true,
    "Message": "Бронирование успешно создано",
    "BookingId": 789
}
```

---

#### `get_user_bookings` - Get User's Bookings

Retrieves all bookings for a specific user.

**Request:**
```json
{
    "Command": "get_user_bookings",
    "UserId": 123
}
```

**Response:**
```json
{
    "Success": true,
    "Bookings": [
        {
            "BookingId": 789,
            "Date": "2025-12-05",
            "TimeSlot": "10:00-10:45",
            "BreakSlot": "45 мин",
            "DayOfWeek": "Пятница",
            "Status": "Booked",
            "BookingDate": "2025-12-03 14:30"
        }
    ]
}
```

---

#### `cancel_booking` - Cancel Booking

Cancels an existing booking and returns seats to the pool.

**Request:**
```json
{
    "Command": "cancel_booking",
    "BookingId": 789
}
```

**Response:**
```json
{
    "Success": true,
    "Message": "Бронирование успешно отменено"
}
```

---

### 4.4 Review Commands

#### `get_reviews` - Get Approved Reviews

Retrieves all approved public reviews.

**Request:**
```json
{
    "Command": "get_reviews"
}
```

**Response:**
```json
{
    "Success": true,
    "Reviews": [
        {
            "Id": 1,
            "UserEmail": "user@example.com",
            "Rating": 5,
            "Text": "Отличный лёд!",
            "Date": "2025-12-01 15:30",
            "IsApproved": true
        }
    ]
}
```

---

#### `add_review` - Add Review

Submits a new review.

**Request:**
```json
{
    "Command": "add_review",
    "UserId": 123,
    "Rating": 5,
    "Text": "Замечательное место для семейного отдыха!"
}
```

**Validation:**
- Rating must be 1-5
- Text is required

---

#### `get_user_reviews` - Get User's Reviews

Retrieves reviews submitted by a specific user.

**Request:**
```json
{
    "Command": "get_user_reviews",
    "UserId": 123
}
```

---

### 4.5 User Commands

#### `get_user_info` - Get User Information

Retrieves user profile data.

**Request:**
```json
{
    "Command": "get_user_info",
    "UserId": 123
}
```

**Response:**
```json
{
    "Success": true,
    "User": {
        "Id": 123,
        "Email": "user@example.com",
        "Role": "Client",
        "RegDate": "2025-01-15"
    }
}
```

---

#### `get_user_profile` - Get User Profile

Same as `get_user_info`, alternative endpoint.

---

### 4.6 Analytics Commands (Admin)

#### `get_arena_metrics` - Get Arena Metrics

Retrieves operational metrics for the arena.

**Request:**
```json
{
    "Command": "get_arena_metrics"
}
```

**Response:**
```json
{
    "Success": true,
    "Metrics": [
        {
            "Date": "2025-12-03",
            "Income": 1500.00,
            "Attendance": 125,
            "Electricity": 450.50,
            "Notes": "Normal operations"
        }
    ]
}
```

---

### 4.7 Support Commands

#### `get_support_chat` - Get Support Chat History

Retrieves chat messages for a user's support conversation.

**Request:**
```json
{
    "Command": "get_support_chat",
    "UserId": 123
}
```

---

#### `send_support_message` - Send Support Message (Client)

Sends a message from client to support.

**Request:**
```json
{
    "Command": "send_support_message",
    "UserId": 123,
    "Email": "user@example.com",
    "Message": "У меня вопрос о бронировании"
}
```

---

#### `get_active_support_chats` - Get Active Chats (Admin)

Retrieves list of users with active support conversations.

**Request:**
```json
{
    "Command": "get_active_support_chats"
}
```

---

#### `send_support_message_as_admin` - Reply to Support (Admin)

Sends a support response from admin.

**Request:**
```json
{
    "Command": "send_support_message_as_admin",
    "TargetUserId": 123,
    "Message": "Спасибо за обращение! Ваша бронь подтверждена."
}
```

---

### 4.8 Utility Commands

#### `test` - Server Health Check

Verifies server is running.

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
    "Timestamp": "2025-12-03T14:30:00"
}
```

---

## 5. Client Components

### 5.1 Form1 - Login Form

Main entry point for the application. Handles user authentication.

**Public Methods:**
```csharp
public void ShowAuthForm()
```
Shows the authentication form and resets input fields.

**Usage:**
```csharp
// Called when user logs out
authForm.ShowAuthForm();
```

**Features:**
- Email/password authentication
- "Continue as Guest" option (read-only mode)
- Direct admin access (admin/admin)
- Registration link

---

### 5.2 RegisterForm - Registration

User account creation form.

**Validation Rules:**
- Valid email format required
- Password minimum 6 characters
- Password confirmation must match

**Example Flow:**
```csharp
// Opens registration dialog
new RegisterForm().ShowDialog();
```

---

### 5.3 ClientForm - Main Client Interface

Primary interface for authenticated users.

**Constructor:**
```csharp
public ClientForm(string username, int userId, bool isGuestMode = false)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| username | string | Display name (email) |
| userId | int | User's database ID |
| isGuestMode | bool | If true, booking features disabled |

**Public Properties:**
```csharp
public List<Booking> UserBookings { get; private set; }
public List<Review> UserReviews { get; private set; }
public bool IsGuestMode { get; set; }
```

**Public Methods:**
```csharp
public async Task<JsonElement> SendServerRequest(object request)
```
Sends a JSON request to the server and returns the response.

**Example:**
```csharp
var clientForm = new ClientForm("user@example.com", 123, false);
var response = await clientForm.SendServerRequest(new { Command = "get_schedule" });
```

---

### 5.4 BookingForm - Booking Creation

Ticket selection and booking confirmation dialog.

**Constructor:**
```csharp
public BookingForm(
    string day,           // Day of week
    string date,          // Date string (dd.MM.yyyy)
    string time,          // Time slot
    ClientForm parent,    // Parent form reference
    int userId,           // User ID
    object dbService,     // Database service (legacy)
    int scheduleId,       // Schedule slot ID
    int availableSeats    // Available seats
)
```

**Features:**
- Ticket type selection (Adult, Child, Senior)
- Quantity counters with availability check
- Optional skate rental
- Real-time price calculation

**Usage:**
```csharp
var dbService = new DatabaseService(clientForm);
using (var bookingForm = new BookingForm(
    "ПОНЕДЕЛЬНИК", "05.12.2025", "14:00-14:45",
    clientForm, userId, dbService, scheduleId, 35))
{
    if (bookingForm.ShowDialog() == DialogResult.OK)
    {
        // Booking confirmed
        await RefreshSchedule();
    }
}
```

---

### 5.5 ProfileForm - User Profile

Displays user information, bookings, and reviews.

**Constructor:**
```csharp
public ProfileForm(
    string username,         // Display name
    int userId,              // User ID
    List<Review> reviews,    // User's reviews
    ClientForm parent        // Parent form
)
```

**Features:**
- Booking history with status indicators
- Total cost summary
- Review submission
- Support ticket access

---

### 5.6 SupportForm - Client Support Chat

Real-time chat interface for customer support.

**Constructor:**
```csharp
public SupportForm(int userId, string username, ClientForm parent)
```

**Features:**
- Real-time message updates (3-second polling)
- Message bubble UI
- Enter-to-send functionality

---

## 6. Admin Panel Components

### 6.1 AdminForm - Admin Dashboard

Main administrative interface with tabbed navigation.

**Constructor:**
```csharp
public AdminForm()
```

**Public Methods:**
```csharp
public async Task<JsonElement> SendServerRequest(object request)
```

**Tabs:**
- 👥 User Management (`UsersTab`)
- 📊 Analytics (`AnalyticsTab`)
- 🎫 Bookings (`BookingsTab`)
- 📅 Schedule (`ScheduleTab`)
- 🛠 Support (`SupportTab`)

---

### 6.2 UsersTab - User Management

CRUD operations for user accounts.

**Features:**
- User list with search/filter
- Add/Edit/Delete users
- Registration statistics
- Weekly registration breakdown

**Database Connection:**
```csharp
// Direct SQL Server connection for admin operations
private const string ConnectionString = 
    "Server=DESKTOP-I80K0OH\\SQLEXPRESS;Database=Ice_Arena;...";
```

---

### 6.3 AnalyticsTab - Arena Analytics

Business intelligence dashboard.

**Features:**
- Income tracking
- Attendance metrics
- Energy consumption monitoring
- Interactive charts (ZedGraph)
- CSV export functionality

**Report Types:**
- Посещаемость (Attendance)
- Доход (Income)
- Энергопотребление (Energy)

**Periods:**
- 1 день (Today)
- 3 дня (3 days)
- Неделя (Week)
- Месяц (Month)
- Все данные (All time)

---

### 6.4 BookingsTab - Booking Management

Administrative booking control.

**Features:**
- View all bookings with filters
- Status management (Confirm/Complete/Cancel)
- Delete bookings
- Statistics sidebar
- CSV export

**Status Workflow:**
```
Booked → Confirmed → Completed
   ↓         ↓
Cancelled  Cancelled
```

---

### 6.5 ScheduleTab - Schedule Management

Session schedule configuration.

**Features:**
- Weekly schedule view
- Add/Edit/Delete slots
- Capacity management
- Status toggling (ДОСТУПНО/НЕДОСТУПНО)

**Public Methods:**
```csharp
public async Task<JsonElement> SendServerRequest(object request)
```

---

### 6.6 SupportTab - Admin Support Chat

Response interface for customer support.

**Setup:**
```csharp
// Must call SetParent after construction
supportTab.SetParent(adminForm);
```

**Features:**
- Active conversations list
- Real-time message sync
- Admin reply functionality

---

## 7. Utility Classes

### 7.1 EncryptionHelper

AES-256 encryption for secure password transmission.

**Static Methods:**

```csharp
public static string Encrypt(string plainText)
```
Encrypts a plain text string using AES-256.

**Parameters:**
- `plainText`: The string to encrypt

**Returns:** Base64-encoded encrypted string, or `null` on error

---

```csharp
public static string Decrypt(string cipherText)
```
Decrypts an AES-256 encrypted string.

**Parameters:**
- `cipherText`: Base64-encoded encrypted string

**Returns:** Decrypted plain text, or `null` on error

**Example:**
```csharp
// Encrypt password before sending to server
string encrypted = EncryptionHelper.Encrypt("myPassword123");

// Server decrypts and hashes for comparison
string decrypted = EncryptionHelper.Decrypt(encrypted);
string hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(decrypted)));
```

> ⚠️ **Security Note:** The encryption key is hardcoded for demonstration. In production, use secure key management.

---

### 7.2 DatabaseService

Client-side database operations wrapper.

**Constructor:**
```csharp
public DatabaseService(ClientForm parent)
```

**Methods:**

```csharp
public async Task<int> GetAvailableSeats(int scheduleId)
```
Returns available seat count for a schedule slot.

```csharp
public async Task<bool> DecreaseAvailableSeats(int scheduleId, int count)
```
Reduces available seats after booking.

```csharp
public async Task IncreaseAvailableSeats(int scheduleId, int count)
```
Restores seats after cancellation.

```csharp
public async Task<int> CreateBooking(int userId, int scheduleId, DateTime bookingDate, string status)
```
Creates a new booking record.

```csharp
public async Task<List<(int ticketId, int quantity)>> CreateTickets(int bookingId, List<Ticket> tickets)
```
Creates ticket records for a booking.

```csharp
public async Task CreateRental(int ticketId, string skateSize, string skateType)
```
Records skate rental information.

---

### 7.3 GraphicsExtension

UI helper for drawing rounded rectangles.

```csharp
public static void DrawRoundedRectangle(
    this Graphics g, 
    Pen pen, 
    int x, int y, 
    int w, int h, 
    int r)
```

**Example:**
```csharp
protected override void OnPaint(PaintEventArgs e)
{
    using (Pen p = new Pen(Color.LightGray, 1))
    {
        e.Graphics.DrawRoundedRectangle(p, 0, 0, Width - 1, Height - 1, 20);
    }
}
```

---

### 7.4 ModernButton

Custom button with rounded corners and hover effects.

**Properties:**
```csharp
public int BorderRadius { get; set; }  // Corner radius (default: 20)
public Color HoverColor { get; set; }  // Mouse-over color
```

**Example:**
```csharp
var btn = new ModernButton
{
    Text = "SUBMIT",
    BackColor = Color.FromArgb(46, 204, 113),
    HoverColor = Color.FromArgb(39, 174, 96),
    Size = new Size(150, 45),
    BorderRadius = 10
};
```

---

### 7.5 BufferedFlowLayoutPanel

Double-buffered FlowLayoutPanel for flicker-free rendering.

```csharp
public partial class BufferedFlowLayoutPanel : FlowLayoutPanel
{
    public BufferedFlowLayoutPanel()
    {
        this.DoubleBuffered = true;
        this.SetStyle(
            ControlStyles.OptimizedDoubleBuffer | 
            ControlStyles.AllPaintingInWmPaint | 
            ControlStyles.UserPaint, true);
        this.UpdateStyles();
    }
}
```

---

## 8. Usage Examples

### 8.1 Complete Booking Flow

```csharp
// 1. User logs in
string encrypted = EncryptionHelper.Encrypt(password);
var loginResponse = await SendServerRequest(new {
    Command = "login",
    Email = "user@example.com",
    Password = encrypted
});

if (loginResponse.GetProperty("Success").GetBoolean())
{
    int userId = loginResponse.GetProperty("UserId").GetInt32();
    
    // 2. Load schedule
    var scheduleResponse = await SendServerRequest(new { Command = "get_schedule" });
    
    // 3. Create booking
    var bookingResponse = await SendServerRequest(new {
        Command = "create_booking",
        UserId = userId,
        ScheduleId = 123,
        TicketsCount = 3,
        Tickets = new[] {
            new { Type = "Adult", Quantity = 2, Price = 6.00m },
            new { Type = "Child", Quantity = 1, Price = 4.00m }
        }
    });
    
    // 4. Confirm booking created
    if (bookingResponse.GetProperty("Success").GetBoolean())
    {
        int bookingId = bookingResponse.GetProperty("BookingId").GetInt32();
        MessageBox.Show($"Booking #{bookingId} created successfully!");
    }
}
```

### 8.2 Admin: Change Booking Status

```csharp
// Get bookings with specific status
var bookings = await LoadBookingsFromDatabase("Booked");

// Update status to Confirmed
var response = await ExecuteSqlCommand(
    "UPDATE Bookings SET Status = @Status WHERE Id = @Id",
    new SqlParameter("@Status", "Confirmed"),
    new SqlParameter("@Id", bookingId)
);
```

### 8.3 Adding a Review

```csharp
var response = await parentForm.SendServerRequest(new {
    Command = "add_review",
    UserId = currentUserId,
    Rating = 5,
    Text = "Отличное место для семейного отдыха!"
});

if (response.GetProperty("Success").GetBoolean())
{
    int reviewId = response.GetProperty("ReviewId").GetInt32();
    MessageBox.Show("Отзыв добавлен!");
    await RefreshReviews();
}
```

### 8.4 Support Chat Integration

**Client Side:**
```csharp
// Send support message
await parentForm.SendServerRequest(new {
    Command = "send_support_message",
    UserId = userId,
    Email = userEmail,
    Message = "Помогите с бронированием"
});

// Retrieve chat history
var history = await parentForm.SendServerRequest(new {
    Command = "get_support_chat",
    UserId = userId
});
```

**Admin Side:**
```csharp
// Get active chats
var chats = await adminForm.SendServerRequest(new { 
    Command = "get_active_support_chats" 
});

// Reply to user
await adminForm.SendServerRequest(new {
    Command = "send_support_message_as_admin",
    TargetUserId = selectedUserId,
    Message = "Ваш запрос обработан!"
});
```

---

## 9. Configuration

### 9.1 Server Configuration

```csharp
// Program.cs (Server)
private const int Port = 8888;
private const string Ip = "127.0.0.1";

private const string ConnectionString =
    "Data Source=DESKTOP-I80K0OH\\SQLEXPRESS;" +
    "Initial Catalog=Ice_Arena;" +
    "Integrated Security=True;" +
    "TrustServerCertificate=True;";
```

### 9.2 Client Configuration

```csharp
// BookingForm.cs
private const string SERVER_IP = "127.0.0.1";
private const int SERVER_PORT = 8888;

// Pricing
private const decimal ADULT_PRICE = 6.00m;
private const decimal CHILD_PRICE = 4.00m;
private const decimal SENIOR_PRICE = 4.00m;
```

### 9.3 Database Schema (Required Tables)

```sql
-- Users table
CREATE TABLE Users (
    Id INT IDENTITY PRIMARY KEY,
    Email NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(50) DEFAULT 'Client',
    RegDate DATETIME DEFAULT GETDATE()
);

-- Schedule table
CREATE TABLE Schedule (
    Id INT IDENTITY PRIMARY KEY,
    Date DATE NOT NULL,
    TimeSlot NVARCHAR(50) NOT NULL,
    BreakSlot NVARCHAR(50) DEFAULT '45 мин',
    DayOfWeek NVARCHAR(50),
    Capacity INT DEFAULT 50,
    AvailableSeats INT DEFAULT 50,
    Status NVARCHAR(50) DEFAULT 'ДОСТУПНО'
);

-- Bookings table
CREATE TABLE Bookings (
    Id INT IDENTITY PRIMARY KEY,
    UserId INT FOREIGN KEY REFERENCES Users(Id),
    ScheduleId INT FOREIGN KEY REFERENCES Schedule(Id),
    Status NVARCHAR(50) DEFAULT 'Booked',
    BookingDate DATETIME DEFAULT GETDATE()
);

-- Tickets table
CREATE TABLE Tickets (
    Id INT IDENTITY PRIMARY KEY,
    BookingId INT FOREIGN KEY REFERENCES Bookings(Id),
    Type NVARCHAR(50),
    Quantity INT,
    Price DECIMAL(10,2)
);

-- Reviews table
CREATE TABLE Reviews (
    Id INT IDENTITY PRIMARY KEY,
    UserId INT FOREIGN KEY REFERENCES Users(Id),
    Rating TINYINT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Text NVARCHAR(MAX),
    Date DATETIME DEFAULT GETDATE(),
    IsApproved BIT DEFAULT 1
);

-- ArenaMetrics table
CREATE TABLE ArenaMetrics (
    Id INT IDENTITY PRIMARY KEY,
    Date DATE NOT NULL,
    Income DECIMAL(10,2) DEFAULT 0,
    Attendance INT DEFAULT 0,
    Electricity DECIMAL(10,2) DEFAULT 0,
    Notes NVARCHAR(MAX)
);
```

---

## Appendix A: Error Codes

| Error Message (Russian) | Meaning |
|------------------------|---------|
| Отсутствует Email или Password | Missing credentials |
| Email и пароль не могут быть пустыми | Empty credentials |
| Неверный пароль | Wrong password |
| Пользователь с таким email не найден | User not found |
| Пользователь с таким email уже существует | Duplicate email |
| Некорректный формат email | Invalid email format |
| Нет свободных мест на этот сеанс | No available seats |
| Бронирование не найдено | Booking not found |
| Отсутствует команда | Missing command |
| Неизвестная команда | Unknown command |

---

## Appendix B: Color Palette

The application uses a consistent color scheme:

| Usage | Color | Hex |
|-------|-------|-----|
| Primary | Indigo | `#4F46E5` |
| Primary Dark | Dark Indigo | `#4338CA` |
| Secondary | Emerald | `#10B981` |
| Success | Green | `#2ECC71` |
| Warning | Yellow | `#F1C40F` |
| Danger | Red | `#E74C3C` |
| Info | Blue | `#3498DB` |
| Text Dark | Dark Gray | `#1F2937` |
| Text Light | Gray | `#6B7280` |
| Background | Light Gray | `#F0F2F5` |

---

## License

© 2025 Polessu Ice Arena. All rights reserved.

For support: support@polessu.by
