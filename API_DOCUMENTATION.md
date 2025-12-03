# Ice Arena Booking System - API Documentation

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Server API](#server-api)
4. [Client Services](#client-services)
5. [Data Models](#data-models)
6. [UI Components](#ui-components)
7. [Security & Encryption](#security--encryption)
8. [Usage Examples](#usage-examples)
9. [Error Handling](#error-handling)

---

## Overview

The Ice Arena Booking System is a Windows Forms-based client-server application for managing ice rink bookings, user registrations, reviews, and arena analytics. The system uses TCP/IP sockets for communication and AES encryption for password security.

### Key Features
- User authentication and registration
- Session booking with ticket management
- Ice skate rental integration
- Review and rating system
- Admin panel for analytics and management
- Real-time seat availability tracking

---

## Architecture

### Components
- **Server**: TCP server running on `127.0.0.1:8888`
- **Client**: Windows Forms application
- **Database**: SQL Server (Ice_Arena database)
- **Communication**: JSON over TCP/IP
- **Security**: AES encryption for passwords

### Technology Stack
- .NET Framework / .NET Core
- Windows Forms
- Microsoft.Data.SqlClient
- System.Text.Json
- System.Security.Cryptography

---

## Server API

The server exposes a JSON-based API over TCP. All requests follow this format:

```json
{
  "Command": "command_name",
  "Parameter1": "value1",
  "Parameter2": "value2"
}
```

### 1. Authentication Endpoints

#### **login**
Authenticates a user and returns their role and user ID.

**Request:**
```json
{
  "Command": "login",
  "Email": "user@example.com",
  "Password": "encrypted_password_base64"
}
```

**Response:**
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

**Usage Example:**
```csharp
// Client-side implementation
string email = "user@example.com";
string password = "mypassword";
string encryptedPassword = EncryptionHelper.Encrypt(password);

var request = new
{
    Command = "login",
    Email = email,
    Password = encryptedPassword
};

using (var client = new TcpClient())
{
    await client.ConnectAsync("127.0.0.1", 8888);
    using (var stream = client.GetStream())
    {
        string json = JsonSerializer.Serialize(request);
        byte[] data = Encoding.UTF8.GetBytes(json);
        await stream.WriteAsync(data, 0, data.Length);
        
        byte[] buffer = new byte[4096];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        
        var result = JsonSerializer.Deserialize<JsonElement>(response);
        if (result.GetProperty("Success").GetBoolean())
        {
            int userId = result.GetProperty("UserId").GetInt32();
            string role = result.GetProperty("Role").GetString();
            // Handle successful login
        }
    }
}
```

---

#### **register**
Registers a new user account.

**Request:**
```json
{
  "Command": "register",
  "Email": "newuser@example.com",
  "Password": "encrypted_password_base64",
  "Role": "Client"
}
```

**Response:**
```json
{
  "Success": true,
  "Message": "Регистрация успешна",
  "UserId": 124,
  "Role": "Client"
}
```

**Error Codes:**
- `"Пользователь с таким email уже существует"` - Duplicate email
- `"Некорректный формат email"` - Invalid email format
- `"Email и пароль не могут быть пустыми"` - Empty fields

---

### 2. Schedule Endpoints

#### **get_schedule**
Retrieves the arena schedule with availability information.

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
      "Date": "2025-12-03",
      "TimeSlot": "10:00-10:45",
      "BreakSlot": "10:45-11:00",
      "DayOfWeek": "Wednesday",
      "Capacity": 50,
      "AvailableSeats": 35,
      "Status": "ДОСТУПНО"
    },
    {
      "Id": 2,
      "Date": "2025-12-03",
      "TimeSlot": "12:00-12:45",
      "BreakSlot": "12:45-13:00",
      "DayOfWeek": "Wednesday",
      "Capacity": 50,
      "AvailableSeats": 0,
      "Status": "НЕТ МЕСТ"
    }
  ]
}
```

**Usage Example:**
```csharp
// Load schedule in ClientForm
private async Task LoadScheduleFromServer()
{
    var response = await SendServerRequest(new { Command = "get_schedule" });
    
    if (response.TryGetProperty("Success", out var success) && success.GetBoolean())
    {
        if (response.TryGetProperty("Schedule", out var scheduleArray))
        {
            foreach (var item in scheduleArray.EnumerateArray())
            {
                int scheduleId = item.GetProperty("Id").GetInt32();
                DateTime slotDate = DateTime.Parse(item.GetProperty("Date").GetString());
                string timeSlot = item.GetProperty("TimeSlot").GetString();
                int availableSeats = item.GetProperty("AvailableSeats").GetInt32();
                
                // Display in DataGridView
                dgvSchedule.Rows.Add(
                    slotDate.DayOfWeek.ToString(),
                    slotDate.ToString("dd.MM.yyyy"),
                    timeSlot,
                    item.GetProperty("Capacity").GetInt32(),
                    availableSeats,
                    availableSeats > 0 ? "ДОСТУПНО" : "НЕТ МЕСТ"
                );
            }
        }
    }
}
```

---

### 3. Booking Endpoints

#### **create_booking**
Creates a new booking with tickets.

**Request:**
```json
{
  "Command": "create_booking",
  "UserId": 123,
  "ScheduleId": 1,
  "Tickets": [
    {
      "Type": "Adult",
      "Quantity": 2,
      "Price": 6.00
    },
    {
      "Type": "Child",
      "Quantity": 1,
      "Price": 4.00
    }
  ]
}
```

**Response:**
```json
{
  "Success": true,
  "Message": "Бронирование успешно создано",
  "BookingId": 456
}
```

**Error Response:**
```json
{
  "Success": false,
  "Error": "Недостаточно свободных мест. Доступно: 1, запрошено: 3"
}
```

**Complete Usage Example:**
```csharp
// From BookingForm.cs
private async void BtnConfirm_Click(object sender, EventArgs e)
{
    int totalTickets = (int)(numAdult.Value + numChild.Value + numSenior.Value);
    
    if (totalTickets == 0)
    {
        MessageBox.Show("Выберите хотя бы один билет!");
        return;
    }
    
    var ticketsList = new List<TicketDto>();
    
    if (numAdult.Value > 0)
        ticketsList.Add(new TicketDto 
        { 
            Type = "Adult", 
            Quantity = (int)numAdult.Value, 
            Price = 6.00m 
        });
        
    if (numChild.Value > 0)
        ticketsList.Add(new TicketDto 
        { 
            Type = "Child", 
            Quantity = (int)numChild.Value, 
            Price = 4.00m 
        });
        
    if (numSenior.Value > 0)
        ticketsList.Add(new TicketDto 
        { 
            Type = "Senior", 
            Quantity = (int)numSenior.Value, 
            Price = 4.00m 
        });
    
    var request = new
    {
        Command = "create_booking",
        UserId = this.userId,
        ScheduleId = this.scheduleId,
        Tickets = ticketsList
    };
    
    using (TcpClient client = new TcpClient())
    {
        await client.ConnectAsync("127.0.0.1", 8888);
        using (NetworkStream stream = client.GetStream())
        {
            string jsonRequest = JsonSerializer.Serialize(request);
            byte[] data = Encoding.UTF8.GetBytes(jsonRequest);
            await stream.WriteAsync(data, 0, data.Length);
            
            byte[] buffer = new byte[4096];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string jsonResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            
            using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
            {
                if (doc.RootElement.GetProperty("Success").GetBoolean())
                {
                    MessageBox.Show("✅ Бронирование успешно создано!");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }
    }
}
```

---

#### **get_user_bookings**
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
      "BookingId": 456,
      "Date": "2025-12-03",
      "TimeSlot": "10:00-10:45",
      "BreakSlot": "10:45-11:00",
      "DayOfWeek": "Wednesday",
      "Status": "Booked",
      "BookingDate": "2025-12-02 15:30"
    }
  ]
}
```

---

#### **cancel_booking**
Cancels an existing booking and returns the seat to availability.

**Request:**
```json
{
  "Command": "cancel_booking",
  "BookingId": 456
}
```

**Response:**
```json
{
  "Success": true,
  "Message": "Бронирование успешно отменено"
}
```

**Usage Example:**
```csharp
private async Task CancelBookingAsync(string day, string date, string time, int scheduleId)
{
    var booking = UserBookings.FirstOrDefault(b => b.ScheduleId == scheduleId);
    if (booking == null) return;
    
    if (MessageBox.Show("Отменить бронирование?", "Подтверждение", 
        MessageBoxButtons.YesNo) == DialogResult.Yes)
    {
        var response = await SendServerRequest(new 
        { 
            Command = "cancel_booking", 
            BookingId = booking.Id 
        });
        
        if (response.TryGetProperty("Success", out var s) && s.GetBoolean())
        {
            UserBookings.RemoveAll(b => b.Id == booking.Id);
            await LoadScheduleFromServer();
            MessageBox.Show("Бронирование отменено.", "Успех");
        }
    }
}
```

---

### 4. Review Endpoints

#### **get_reviews**
Gets all approved reviews.

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
      "Text": "Отличная арена!",
      "Date": "2025-12-01 14:30",
      "IsApproved": true
    }
  ]
}
```

---

#### **add_review**
Submits a new review.

**Request:**
```json
{
  "Command": "add_review",
  "UserId": 123,
  "Rating": 5,
  "Text": "Отличное обслуживание и чистый лед!"
}
```

**Response:**
```json
{
  "Success": true,
  "Message": "Отзыв успешно добавлен",
  "ReviewId": 789
}
```

**Validation:**
- Rating must be between 1 and 5
- Text must not be empty

**Usage Example:**
```csharp
private async void BtnAddReview_Click(object sender, EventArgs e)
{
    string reviewText = txtNewReview.Text.Trim();
    
    if (reviewText.Length < 3)
    {
        MessageBox.Show("Введите отзыв (минимум 3 символа).");
        return;
    }
    
    var response = await parentForm.SendServerRequest(new
    {
        Command = "add_review",
        UserId = userId,
        Rating = cmbRating.SelectedIndex + 1,
        Text = reviewText
    });
    
    if (response.TryGetProperty("Success", out var s) && s.GetBoolean())
    {
        txtNewReview.Clear();
        MessageBox.Show("Отзыв добавлен!");
        await LoadUserReviewsAsync();
    }
}
```

---

#### **get_user_reviews**
Gets all reviews for a specific user.

**Request:**
```json
{
  "Command": "get_user_reviews",
  "UserId": 123
}
```

**Response:**
```json
{
  "Success": true,
  "Reviews": [
    {
      "Id": 789,
      "UserId": 123,
      "Rating": 5,
      "Text": "Отличное обслуживание!",
      "Date": "2025-12-02 10:15",
      "IsApproved": true
    }
  ]
}
```

---

### 5. User Profile Endpoints

#### **get_user_profile**
Retrieves user profile information.

**Request:**
```json
{
  "Command": "get_user_profile",
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
    "RegDate": "2025-11-01"
  }
}
```

---

#### **get_user_info**
Alternative endpoint for user information (same as get_user_profile).

**Request:**
```json
{
  "Command": "get_user_info",
  "UserId": 123
}
```

---

### 6. Admin Endpoints

#### **get_arena_metrics**
Retrieves arena performance metrics (admin only).

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
      "Date": "2025-12-01",
      "Income": 1250.50,
      "Attendance": 45,
      "Electricity": 320.00,
      "Notes": "Regular day"
    },
    {
      "Date": "2025-12-02",
      "Income": 890.00,
      "Attendance": 32,
      "Electricity": 280.50,
      "Notes": ""
    }
  ]
}
```

---

## Client Services

### DatabaseService

The `DatabaseService` class handles database operations through server requests.

#### Class Definition
```csharp
public class DatabaseService
{
    private ClientForm parentForm;
    
    public DatabaseService(ClientForm parent)
    {
        parentForm = parent;
    }
}
```

#### Methods

##### GetAvailableSeats
Gets the number of available seats for a schedule.

```csharp
public async Task<int> GetAvailableSeats(int scheduleId)
```

**Parameters:**
- `scheduleId` (int): The schedule ID to check

**Returns:**
- `Task<int>`: Number of available seats (default 50 if error)

**Example:**
```csharp
var dbService = new DatabaseService(this);
int available = await dbService.GetAvailableSeats(scheduleId);

if (available > 0)
{
    // Allow booking
}
else
{
    MessageBox.Show("Нет свободных мест");
}
```

---

##### DecreaseAvailableSeats
Decreases the available seats for a schedule.

```csharp
public async Task<bool> DecreaseAvailableSeats(int scheduleId, int count)
```

**Parameters:**
- `scheduleId` (int): The schedule ID
- `count` (int): Number of seats to decrease

**Returns:**
- `Task<bool>`: True if successful, false otherwise

**Example:**
```csharp
bool success = await dbService.DecreaseAvailableSeats(scheduleId, 3);
if (success)
{
    Console.WriteLine("Seats decreased successfully");
}
```

---

##### IncreaseAvailableSeats
Increases the available seats (for cancellations).

```csharp
public async Task IncreaseAvailableSeats(int scheduleId, int count)
```

**Parameters:**
- `scheduleId` (int): The schedule ID
- `count` (int): Number of seats to return

**Example:**
```csharp
await dbService.IncreaseAvailableSeats(scheduleId, 2);
```

---

##### CreateBooking
Creates a new booking record.

```csharp
public async Task<int> CreateBooking(int userId, int scheduleId, 
    DateTime bookingDate, string status)
```

**Parameters:**
- `userId` (int): The user ID
- `scheduleId` (int): The schedule ID
- `bookingDate` (DateTime): Booking timestamp
- `status` (string): Booking status (e.g., "Booked")

**Returns:**
- `Task<int>`: The new booking ID, or 0 if failed

**Example:**
```csharp
int bookingId = await dbService.CreateBooking(
    userId: 123,
    scheduleId: 1,
    bookingDate: DateTime.Now,
    status: "Booked"
);

if (bookingId > 0)
{
    Console.WriteLine($"Booking created with ID: {bookingId}");
}
```

---

##### CreateTickets
Creates ticket records for a booking.

```csharp
public async Task<List<(int ticketId, int quantity)>> CreateTickets(
    int bookingId, List<Ticket> tickets)
```

**Parameters:**
- `bookingId` (int): The booking ID
- `tickets` (List<Ticket>): List of tickets to create

**Returns:**
- `Task<List<(int, int)>>`: List of (ticketId, quantity) pairs

**Example:**
```csharp
var tickets = new List<Ticket>
{
    new Ticket { Type = "Adult", Quantity = 2, Price = 6.00m },
    new Ticket { Type = "Child", Quantity = 1, Price = 4.00m }
};

var createdTickets = await dbService.CreateTickets(bookingId, tickets);

foreach (var (ticketId, quantity) in createdTickets)
{
    Console.WriteLine($"Ticket {ticketId}: {quantity} items");
}
```

---

##### CreateRental
Creates a skate rental record.

```csharp
public async Task CreateRental(int ticketId, string skateSize, string skateType)
```

**Parameters:**
- `ticketId` (int): The ticket ID
- `skateSize` (string): Skate size (e.g., "40 размер")
- `skateType` (string): Skate type (e.g., "Хоккейные", "Фигурные")

**Example:**
```csharp
if (chkSkates.Checked)
{
    await dbService.CreateRental(
        ticketId: firstTicketId,
        skateSize: "40 размер",
        skateType: "Хоккейные"
    );
}
```

---

## Data Models

### Booking

Represents a booking session.

```csharp
public class Booking
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime BookingDate { get; set; }
    public string Status { get; set; }
    public List<Ticket> Tickets { get; set; } = new List<Ticket>();
    public string Day { get; set; }
    public DateTime Date { get; set; }
    public string TimeSlot { get; set; }
    public bool NeedSkates { get; set; }
    public string SkateSize { get; set; }
    public string SkateType { get; set; }
    public int ScheduleId { get; set; }
    
    // Computed properties
    public decimal TotalCost => Tickets?.Sum(t => t.Price * t.Quantity) ?? 0;
    public int AdultTickets => Tickets?.FirstOrDefault(t => t.Type == "Adult")?.Quantity ?? 0;
    public int ChildTickets => Tickets?.FirstOrDefault(t => t.Type == "Child")?.Quantity ?? 0;
    public int SeniorTickets => Tickets?.FirstOrDefault(t => t.Type == "Senior")?.Quantity ?? 0;
    public int TotalTickets => Tickets?.Sum(t => t.Quantity) ?? 0;
}
```

**Example Usage:**
```csharp
var booking = new Booking
{
    UserId = 123,
    Date = DateTime.Parse("2025-12-03"),
    TimeSlot = "10:00-10:45",
    Status = "Booked",
    Tickets = new List<Ticket>
    {
        new Ticket { Type = "Adult", Quantity = 2, Price = 6.00m },
        new Ticket { Type = "Child", Quantity = 1, Price = 4.00m }
    }
};

Console.WriteLine($"Total Cost: {booking.TotalCost} BYN");
Console.WriteLine($"Total Tickets: {booking.TotalTickets}");
```

---

### Ticket

Represents a single ticket type within a booking.

```csharp
public class Ticket
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public string Type { get; set; }      // "Adult", "Child", "Senior"
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
```

**Ticket Types:**
- **Adult**: 18-64 years, 6.00 BYN
- **Child**: Under 17, 4.00 BYN
- **Senior**: 65+, 4.00 BYN

**Example:**
```csharp
var adultTicket = new Ticket
{
    Type = "Adult",
    Quantity = 2,
    Price = 6.00m
};

decimal subtotal = adultTicket.Price * adultTicket.Quantity; // 12.00 BYN
```

---

### Review

Represents a user review.

```csharp
public class Review
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int Rating { get; set; }        // 1-5
    public string Text { get; set; }
    public DateTime Date { get; set; }
    public bool IsApproved { get; set; }
}
```

**Example:**
```csharp
var review = new Review
{
    UserId = 123,
    Rating = 5,
    Text = "Отличная арена, чистый лед!",
    Date = DateTime.Now,
    IsApproved = true
};
```

---

### TicketDto

Data transfer object for ticket creation requests.

```csharp
public class TicketDto
{
    public string Type { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
```

---

## UI Components

### Form1 (Login Form)

Main authentication form.

#### Key Methods

```csharp
public void ShowAuthForm()
```
Shows the authentication form and resets fields.

**Example:**
```csharp
var loginForm = new Form1();
loginForm.Show();

// After logout:
loginForm.ShowAuthForm();
```

---

### ClientForm

Main client interface for viewing schedule and making bookings.

#### Constructor
```csharp
public ClientForm(string username, int userId, bool isGuestMode = false)
```

**Parameters:**
- `username` (string): Display name
- `userId` (int): User ID
- `isGuestMode` (bool): If true, booking features are disabled

**Example:**
```csharp
// Regular user login
var clientForm = new ClientForm("john@example.com", 123, false);
clientForm.Show();

// Guest mode
var guestForm = new ClientForm("Гость", 0, true);
guestForm.Show();
```

---

#### SendServerRequest
Sends a request to the server and returns the JSON response.

```csharp
public async Task<JsonElement> SendServerRequest(object request)
```

**Parameters:**
- `request` (object): Request object to serialize

**Returns:**
- `Task<JsonElement>`: Parsed JSON response

**Example:**
```csharp
var response = await clientForm.SendServerRequest(new
{
    Command = "get_schedule"
});

if (response.TryGetProperty("Success", out var success) && success.GetBoolean())
{
    // Handle successful response
}
```

---

### BookingForm

Form for creating new bookings.

#### Constructor
```csharp
public BookingForm(string day, string date, string time, 
    ClientForm parent, int userId, object dbService, 
    int scheduleId, int availableSeats)
```

**Parameters:**
- `day` (string): Day of week
- `date` (string): Date (yyyy-MM-dd)
- `time` (string): Time slot (HH:mm-HH:mm)
- `parent` (ClientForm): Parent form reference
- `userId` (int): User ID
- `dbService` (object): Database service instance
- `scheduleId` (int): Schedule ID to book
- `availableSeats` (int): Current seat availability

**Example:**
```csharp
var dbService = new DatabaseService(this);

using (var bookingForm = new BookingForm(
    day: "WEDNESDAY",
    date: "2025-12-03",
    time: "10:00-10:45",
    parent: this,
    userId: currentUserId,
    dbService: dbService,
    scheduleId: 1,
    availableSeats: 35
))
{
    if (bookingForm.ShowDialog() == DialogResult.OK)
    {
        // Booking successful, refresh data
        await LoadScheduleFromServer();
    }
}
```

---

### ProfileForm

User profile and booking history form.

#### Constructor
```csharp
public ProfileForm(string username, int userId, 
    List<Review> reviews, ClientForm parent)
```

**Parameters:**
- `username` (string): User's display name
- `userId` (int): User ID
- `reviews` (List<Review>): User's reviews
- `parent` (ClientForm): Parent form for server requests

**Example:**
```csharp
var profileForm = new ProfileForm(
    username: currentUser,
    userId: currentUserId,
    reviews: userReviews,
    parent: this
);

profileForm.ShowDialog();
```

---

### AdminForm

Administrative panel for managing the arena.

#### Constructor
```csharp
public AdminForm()
```

#### SendServerRequest
Sends server requests from admin context.

```csharp
public async Task<JsonElement> SendServerRequest(object request)
```

**Example:**
```csharp
var adminForm = new AdminForm();
adminForm.Show();

// Get metrics
var response = await adminForm.SendServerRequest(new
{
    Command = "get_arena_metrics"
});
```

---

### RegisterForm

User registration form.

**Example:**
```csharp
var registerForm = new RegisterForm();
registerForm.ShowDialog();

// Form handles registration internally
```

---

## Security & Encryption

### EncryptionHelper

Provides AES encryption for sensitive data.

#### Encrypt
Encrypts plaintext using AES.

```csharp
public static string Encrypt(string plainText)
```

**Parameters:**
- `plainText` (string): Text to encrypt

**Returns:**
- `string`: Base64-encoded encrypted text, or null on error

**Example:**
```csharp
string password = "mySecurePassword123";
string encrypted = EncryptionHelper.Encrypt(password);

// Send encrypted password to server
var request = new
{
    Command = "login",
    Email = "user@example.com",
    Password = encrypted
};
```

---

#### Decrypt
Decrypts Base64-encoded ciphertext.

```csharp
public static string Decrypt(string cipherText)
```

**Parameters:**
- `cipherText` (string): Base64-encoded encrypted text

**Returns:**
- `string`: Decrypted plaintext, or null on error

**Example:**
```csharp
string encrypted = "r3WvH7g3k9Zx...";
string decrypted = EncryptionHelper.Decrypt(encrypted);
Console.WriteLine($"Original: {decrypted}");
```

---

### Security Notes

1. **Password Storage**: Passwords are:
   - Encrypted with AES during transit
   - Hashed with SHA256 before database storage
   - Never stored in plaintext

2. **Encryption Keys**: 
   - Keys are hardcoded (suitable for development only)
   - Production systems should use secure key management

3. **Transport Security**:
   - Current implementation uses unencrypted TCP
   - Consider TLS/SSL for production

**Example Security Flow:**
```csharp
// Client-side
string password = "userPassword";
string encrypted = EncryptionHelper.Encrypt(password);
// Send 'encrypted' to server

// Server-side
string decrypted = ServerEncryptionHelper.Decrypt(encrypted);
byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(decrypted));
string passwordHash = Convert.ToBase64String(hash);
// Store 'passwordHash' in database
```

---

## Usage Examples

### Complete Booking Flow

```csharp
// 1. User logs in
var loginForm = new Form1();
loginForm.Show();

// After successful login, ClientForm opens
var clientForm = new ClientForm("user@example.com", 123, false);

// 2. Load schedule
private async Task LoadScheduleFromServer()
{
    var response = await SendServerRequest(new { Command = "get_schedule" });
    
    if (response.TryGetProperty("Success", out var success) && success.GetBoolean())
    {
        if (response.TryGetProperty("Schedule", out var scheduleArray))
        {
            foreach (var item in scheduleArray.EnumerateArray())
            {
                int scheduleId = item.GetProperty("Id").GetInt32();
                DateTime slotDate = DateTime.Parse(item.GetProperty("Date").GetString());
                int availableSeats = item.GetProperty("AvailableSeats").GetInt32();
                
                // Display in UI
                dgvSchedule.Rows.Add(/* ... */);
            }
        }
    }
}

// 3. User clicks on a time slot to book
private async Task ShowBookingFormAsync(string day, string date, 
    string time, int scheduleId)
{
    int available = await GetAvailableSeatsForSchedule(scheduleId);
    
    if (available <= 0)
    {
        MessageBox.Show("Нет свободных мест");
        return;
    }
    
    var dbService = new DatabaseService(this);
    using (var bookingForm = new BookingForm(
        day, date, time, this, currentUserId, 
        dbService, scheduleId, available))
    {
        if (bookingForm.ShowDialog() == DialogResult.OK)
        {
            // Refresh bookings and schedule
            UserBookings = await LoadUserBookingsFromServer();
            await LoadScheduleFromServer();
        }
    }
}

// 4. In BookingForm, user selects tickets and confirms
private async void BtnConfirm_Click(object sender, EventArgs e)
{
    var ticketsList = new List<TicketDto>();
    
    if (numAdult.Value > 0)
        ticketsList.Add(new TicketDto 
        { 
            Type = "Adult", 
            Quantity = (int)numAdult.Value, 
            Price = 6.00m 
        });
    
    var request = new
    {
        Command = "create_booking",
        UserId = this.userId,
        ScheduleId = this.scheduleId,
        Tickets = ticketsList
    };
    
    // Send to server...
    // On success, close form with DialogResult.OK
}
```

---

### Admin Analytics Flow

```csharp
// 1. Admin logs in with admin credentials
// "admin" / "admin" or database user with "Admin" role

// 2. AdminForm opens with analytics tab
var adminForm = new AdminForm();
adminForm.Show();

// 3. Load metrics
private async void LoadMetrics()
{
    var response = await SendServerRequest(new 
    { 
        Command = "get_arena_metrics" 
    });
    
    if (response.TryGetProperty("Success", out var s) && s.GetBoolean())
    {
        if (response.TryGetProperty("Metrics", out var metricsArray))
        {
            foreach (var metric in metricsArray.EnumerateArray())
            {
                string date = metric.GetProperty("Date").GetString();
                decimal income = metric.GetProperty("Income").GetDecimal();
                int attendance = metric.GetProperty("Attendance").GetInt32();
                
                // Display in chart or grid
                dataGridView.Rows.Add(date, income, attendance);
            }
        }
    }
}
```

---

### Review Submission Flow

```csharp
// From ProfileForm
private async void BtnAddReview_Click(object sender, EventArgs e)
{
    string reviewText = txtNewReview.Text.Trim();
    
    if (reviewText.Length < 3)
    {
        MessageBox.Show("Введите отзыв (минимум 3 символа).");
        return;
    }
    
    int rating = cmbRating.SelectedIndex + 1; // 1-5
    
    var response = await parentForm.SendServerRequest(new
    {
        Command = "add_review",
        UserId = userId,
        Rating = rating,
        Text = reviewText
    });
    
    if (response.TryGetProperty("Success", out var s) && s.GetBoolean())
    {
        txtNewReview.Clear();
        MessageBox.Show("Отзыв добавлен!");
        
        // Reload reviews
        await LoadUserReviewsAsync();
        UpdateReviewsList();
    }
    else
    {
        string error = response.TryGetProperty("Error", out var e) 
            ? e.GetString() 
            : "Неизвестная ошибка";
        MessageBox.Show($"Ошибка: {error}");
    }
}
```

---

## Error Handling

### Server-Side Error Responses

All server responses include a `Success` field. On error:

```json
{
  "Success": false,
  "Error": "Descriptive error message"
}
```

### Common Error Codes

| Error Message | Cause | Solution |
|--------------|-------|----------|
| `"Отсутствует Email или Password"` | Missing required fields | Include all required parameters |
| `"Неверный пароль"` | Incorrect password | Verify credentials |
| `"Пользователь с таким email уже существует"` | Duplicate registration | Use different email |
| `"Недостаточно свободных мест"` | Not enough seats | Choose different time slot |
| `"Рейтинг должен быть от 1 до 5"` | Invalid rating value | Use rating between 1-5 |
| `"Неизвестная команда"` | Invalid command | Check API documentation |

### Client-Side Error Handling

```csharp
try
{
    using (var client = new TcpClient())
    {
        var connectTask = client.ConnectAsync("127.0.0.1", 8888);
        var timeoutTask = Task.Delay(5000);
        
        if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
        {
            throw new TimeoutException("Server connection timeout");
        }
        
        // ... send request and receive response
        
        var response = JsonSerializer.Deserialize<JsonElement>(jsonResponse);
        
        if (!response.TryGetProperty("Success", out var success) 
            || !success.GetBoolean())
        {
            string error = response.TryGetProperty("Error", out var err)
                ? err.GetString()
                : "Unknown error";
            
            MessageBox.Show($"Error: {error}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        
        // Handle successful response
    }
}
catch (TimeoutException)
{
    MessageBox.Show("Connection timeout. Server may be offline.", 
        "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
catch (SocketException ex)
{
    MessageBox.Show($"Network error: {ex.Message}", 
        "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
catch (JsonException ex)
{
    MessageBox.Show($"Invalid server response: {ex.Message}", 
        "Parse Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
catch (Exception ex)
{
    MessageBox.Show($"Unexpected error: {ex.Message}", 
        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
}
```

---

## Best Practices

### 1. Always Check Server Availability

```csharp
private bool IsServerRunning()
{
    try
    {
        using (var client = new TcpClient())
        {
            var connectTask = client.ConnectAsync("127.0.0.1", 8888);
            return connectTask.Wait(2000);
        }
    }
    catch 
    { 
        return false; 
    }
}

// Before any operation
if (!IsServerRunning())
{
    MessageBox.Show("Server is not running. Please start the server.");
    return;
}
```

---

### 2. Use Encryption for Sensitive Data

```csharp
// ALWAYS encrypt passwords before transmission
string password = txtPassword.Text;
string encrypted = EncryptionHelper.Encrypt(password);

var request = new
{
    Command = "login",
    Email = email,
    Password = encrypted  // Not plaintext!
};
```

---

### 3. Validate User Input

```csharp
// Validate email format
private bool IsValidEmail(string email)
{
    try 
    {
        var addr = new System.Net.Mail.MailAddress(email);
        return addr.Address == email;
    }
    catch 
    {
        return false;
    }
}

// Validate before sending
if (!IsValidEmail(email))
{
    MessageBox.Show("Please enter a valid email address");
    return;
}

// Validate password strength
if (password.Length < 6)
{
    MessageBox.Show("Password must be at least 6 characters");
    return;
}
```

---

### 4. Handle Concurrent Requests

```csharp
private bool isLoading = false;
private readonly object loadingLock = new object();

private async Task LoadScheduleFromServer()
{
    lock (loadingLock) 
    { 
        if (isLoading) return;
        isLoading = true;
    }
    
    try
    {
        // Load data...
    }
    finally
    {
        lock (loadingLock) 
        { 
            isLoading = false; 
        }
    }
}
```

---

### 5. Clean Up Resources

```csharp
// Always use 'using' statements for IDisposable resources
using (var client = new TcpClient())
{
    using (var stream = client.GetStream())
    {
        // Perform operations
    }
} // Automatically disposed
```

---

## Testing

### Manual Testing Checklist

#### Authentication
- [ ] Register new user with valid email
- [ ] Register with duplicate email (should fail)
- [ ] Login with correct credentials
- [ ] Login with incorrect password (should fail)
- [ ] Login as guest (limited features)
- [ ] Login as admin ("admin"/"admin")

#### Booking
- [ ] View available schedule
- [ ] Book session with available seats
- [ ] Try booking with insufficient seats (should fail)
- [ ] Add skate rental to booking
- [ ] Cancel active booking
- [ ] View booking history in profile

#### Reviews
- [ ] Submit review with rating 1-5
- [ ] Submit review with invalid rating (should fail)
- [ ] View own reviews in profile
- [ ] View all approved reviews

#### Admin
- [ ] View arena metrics
- [ ] Manage users
- [ ] View all bookings
- [ ] Update schedule

---

## Troubleshooting

### Issue: "Server not running" error

**Solution:**
1. Start `IceArena.Server.exe` first
2. Verify server console shows "✅ Сервер запущен"
3. Check firewall isn't blocking port 8888

---

### Issue: "Connection timeout"

**Solution:**
1. Increase timeout duration in client code
2. Check network connectivity
3. Verify server is responsive (check CPU usage)

---

### Issue: "Database connection error"

**Solution:**
1. Verify SQL Server is running
2. Check connection string in `Program.cs`
3. Ensure `Ice_Arena` database exists
4. Verify database tables are created

---

### Issue: "Password encryption failed"

**Solution:**
1. Check encryption keys match between client and server
2. Verify System.Security.Cryptography namespace is available
3. Check for special characters causing encoding issues

---

## Configuration

### Server Configuration

File: `Program.cs`

```csharp
private const int Port = 8888;
private const string Ip = "127.0.0.1";
private const string ConnectionString =
    "Data Source=SERVER_NAME\\SQLEXPRESS;Initial Catalog=Ice_Arena;Integrated Security=True;TrustServerCertificate=True;";
```

**To change server port:**
1. Update `Port` constant
2. Update client code to match (search for "8888")
3. Restart server

**To change database:**
1. Update `ConnectionString`
2. Ensure database schema matches expected structure

---

### Client Configuration

No configuration file required. Settings are hardcoded:

- Server IP: `127.0.0.1`
- Server Port: `8888`
- Encryption keys: In `EncryptionHelper.cs`

---

## Version History

### Version 1.0
- Initial release
- User authentication and registration
- Booking system with seat management
- Review system
- Admin panel
- AES password encryption

---

## Support

For issues or questions:
- Email: support@polessu.by
- Check server console for error logs
- Review client error messages for details

---

## License

© 2025 Polessu Ice Arena
All rights reserved.

---

*End of API Documentation*
