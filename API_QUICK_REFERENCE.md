# Ice Arena API - Quick Reference

**Server:** `127.0.0.1:8888`  
**Protocol:** JSON over TCP  
**Version:** 1.0

---

## Authentication

### Login
```json
Request:  { "Command": "login", "Email": "user@example.com", "Password": "encrypted_base64" }
Response: { "Success": true, "Role": "Client", "UserId": 123, "Email": "user@example.com" }
```

### Register
```json
Request:  { "Command": "register", "Email": "new@example.com", "Password": "encrypted_base64", "Role": "Client" }
Response: { "Success": true, "Message": "Регистрация успешна", "UserId": 124, "Role": "Client" }
```

---

## Schedule

### Get Schedule
```json
Request:  { "Command": "get_schedule" }
Response: { 
  "Success": true, 
  "Schedule": [
    { 
      "Id": 1, 
      "Date": "2025-12-03", 
      "TimeSlot": "10:00-10:45",
      "DayOfWeek": "Wednesday",
      "Capacity": 50,
      "AvailableSeats": 35,
      "Status": "ДОСТУПНО"
    }
  ]
}
```

---

## Bookings

### Create Booking
```json
Request:  { 
  "Command": "create_booking",
  "UserId": 123,
  "ScheduleId": 1,
  "Tickets": [
    { "Type": "Adult", "Quantity": 2, "Price": 6.00 },
    { "Type": "Child", "Quantity": 1, "Price": 4.00 }
  ]
}
Response: { "Success": true, "Message": "Бронирование успешно создано", "BookingId": 456 }
```

### Get User Bookings
```json
Request:  { "Command": "get_user_bookings", "UserId": 123 }
Response: { 
  "Success": true,
  "Bookings": [
    {
      "BookingId": 456,
      "Date": "2025-12-03",
      "TimeSlot": "10:00-10:45",
      "Status": "Booked"
    }
  ]
}
```

### Cancel Booking
```json
Request:  { "Command": "cancel_booking", "BookingId": 456 }
Response: { "Success": true, "Message": "Бронирование успешно отменено" }
```

---

## Reviews

### Get Reviews
```json
Request:  { "Command": "get_reviews" }
Response: { 
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

### Add Review
```json
Request:  { "Command": "add_review", "UserId": 123, "Rating": 5, "Text": "Отличное обслуживание!" }
Response: { "Success": true, "Message": "Отзыв успешно добавлен", "ReviewId": 789 }
```

### Get User Reviews
```json
Request:  { "Command": "get_user_reviews", "UserId": 123 }
Response: { 
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

## User Profile

### Get User Profile
```json
Request:  { "Command": "get_user_profile", "UserId": 123 }
Response: { 
  "Success": true,
  "User": {
    "Id": 123,
    "Email": "user@example.com",
    "Role": "Client",
    "RegDate": "2025-11-01"
  }
}
```

### Get User Info
```json
Request:  { "Command": "get_user_info", "UserId": 123 }
Response: { 
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

## Admin

### Get Arena Metrics
```json
Request:  { "Command": "get_arena_metrics" }
Response: { 
  "Success": true,
  "Metrics": [
    {
      "Date": "2025-12-01",
      "Income": 1250.50,
      "Attendance": 45,
      "Electricity": 320.00,
      "Notes": "Regular day"
    }
  ]
}
```

---

## Seat Management

### Decrease Available Seats
```json
Request:  { "Command": "decrease_available_seats", "ScheduleId": 1, "Count": 3 }
Response: { "Success": true }
```

### Increase Available Seats
```json
Request:  { "Command": "increase_available_seats", "ScheduleId": 1, "Count": 2 }
Response: { "Success": true }
```

---

## Data Models

### Booking
```csharp
{
  Id: int,
  UserId: int,
  BookingDate: DateTime,
  Status: string,
  Tickets: List<Ticket>,
  Day: string,
  Date: DateTime,
  TimeSlot: string,
  ScheduleId: int,
  TotalCost: decimal (computed),
  TotalTickets: int (computed)
}
```

### Ticket
```csharp
{
  Id: int,
  BookingId: int,
  Type: string,      // "Adult", "Child", "Senior"
  Quantity: int,
  Price: decimal
}
```

### Review
```csharp
{
  Id: int,
  UserId: int,
  Rating: int,       // 1-5
  Text: string,
  Date: DateTime,
  IsApproved: bool
}
```

---

## Encryption

### Encrypt Password
```csharp
string encrypted = EncryptionHelper.Encrypt("plaintext_password");
// Returns: Base64-encoded encrypted string
```

### Decrypt Password
```csharp
string decrypted = EncryptionHelper.Decrypt("encrypted_base64");
// Returns: Original plaintext string
```

---

## Client Functions

### Send Server Request
```csharp
var response = await clientForm.SendServerRequest(new
{
    Command = "command_name",
    Parameter1 = "value1"
});

if (response.TryGetProperty("Success", out var success) && success.GetBoolean())
{
    // Handle success
}
```

### DatabaseService Methods

#### Get Available Seats
```csharp
var dbService = new DatabaseService(clientForm);
int available = await dbService.GetAvailableSeats(scheduleId);
```

#### Create Booking
```csharp
int bookingId = await dbService.CreateBooking(userId, scheduleId, DateTime.Now, "Booked");
```

#### Create Tickets
```csharp
var tickets = new List<Ticket> { /* ... */ };
var created = await dbService.CreateTickets(bookingId, tickets);
```

#### Create Rental
```csharp
await dbService.CreateRental(ticketId, "40 размер", "Хоккейные");
```

---

## Error Responses

All errors follow this format:
```json
{ "Success": false, "Error": "Error message description" }
```

**Common Errors:**
- `"Отсутствует Email или Password"` - Missing required fields
- `"Неверный пароль"` - Incorrect password
- `"Пользователь с таким email уже существует"` - Duplicate email
- `"Недостаточно свободных мест"` - Not enough seats
- `"Рейтинг должен быть от 1 до 5"` - Invalid rating
- `"Неизвестная команда"` - Invalid command

---

## Ticket Prices

| Type | Age | Price |
|------|-----|-------|
| Adult | 18-64 | 6.00 BYN |
| Child | Under 17 | 4.00 BYN |
| Senior | 65+ | 4.00 BYN |

---

## Status Codes

| Status | Meaning |
|--------|---------|
| ДОСТУПНО | Available for booking |
| НЕТ МЕСТ | Full, no seats available |
| Booked | Active booking |
| Cancelled | Canceled booking |

---

## Connection

### Server Configuration
```csharp
IP: "127.0.0.1"
Port: 8888
Timeout: 5000ms (5 seconds)
```

### Connection Example
```csharp
using (var client = new TcpClient())
{
    await client.ConnectAsync("127.0.0.1", 8888);
    using (var stream = client.GetStream())
    {
        // Send request
        byte[] data = Encoding.UTF8.GetBytes(jsonRequest);
        await stream.WriteAsync(data, 0, data.Length);
        
        // Read response
        byte[] buffer = new byte[4096];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
    }
}
```

---

## Testing Commands

### Test Server Connection
```json
Request:  { "Command": "test" }
Response: { "Success": true, "Message": "Сервер работает", "Timestamp": "2025-12-03T10:30:00" }
```

---

## Admin Credentials

**Default Admin Account:**
- Username: `admin`
- Password: `admin`

⚠️ **Change in production!**

---

## Database Schema

### Tables

**Users**
```sql
Id, Email, PasswordHash, Role, RegDate
```

**Schedule**
```sql
Id, Date, TimeSlot, BreakSlot, DayOfWeek, Capacity, AvailableSeats, Status
```

**Bookings**
```sql
Id, UserId, ScheduleId, Status, BookingDate
```

**Tickets**
```sql
Id, BookingId, Type, Quantity, Price
```

**Reviews**
```sql
Id, UserId, Rating, Text, Date, IsApproved
```

**ArenaMetrics**
```sql
Id, Date, Income, Attendance, Electricity, Notes
```

---

## HTTP-like Status

While this is TCP-based, responses follow similar patterns:

✅ **Success:** `"Success": true`  
❌ **Error:** `"Success": false, "Error": "message"`

---

## Rate Limits

Currently no rate limiting implemented.

**Production Recommendations:**
- 100 requests per minute per IP
- 1000 requests per hour per user

---

## Security

### Password Flow
1. Client encrypts with AES (EncryptionHelper.Encrypt)
2. Send encrypted password (Base64)
3. Server decrypts (ServerEncryptionHelper.Decrypt)
4. Server hashes with SHA256
5. Store hash in database

### Keys (Change in Production!)
```csharp
EncryptionKey: "12345678901234567890123456789012" (32 bytes)
EncryptionIV:  "1234567890123456" (16 bytes)
```

---

## Useful Snippets

### Parse JSON Array
```csharp
if (response.TryGetProperty("Data", out var dataArray) && 
    dataArray.ValueKind == JsonValueKind.Array)
{
    foreach (var item in dataArray.EnumerateArray())
    {
        int id = item.GetProperty("Id").GetInt32();
        string name = item.GetProperty("Name").GetString();
    }
}
```

### Safe Property Access
```csharp
string value = element.TryGetProperty("Key", out var prop) 
    ? prop.GetString() 
    : "default";
```

### Async Button Click
```csharp
private async void BtnSubmit_Click(object sender, EventArgs e)
{
    btnSubmit.Enabled = false;
    try
    {
        await PerformOperation();
    }
    finally
    {
        btnSubmit.Enabled = true;
    }
}
```

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | Dec 2025 | Initial release |

---

## Links

- [Full API Documentation](API_DOCUMENTATION.md)
- [Developer Guide](DEVELOPER_GUIDE.md)
- [User Guide](USER_GUIDE.md)

---

## Support

📧 **Email:** support@polessu.by  
🌐 **Website:** [Ice Arena Website]  
📞 **Phone:** +375 (XX) XXX-XX-XX

---

**Quick Reference Version 1.0**  
**Last Updated:** December 3, 2025  
**© 2025 Polessu Ice Arena**

*For detailed documentation, see API_DOCUMENTATION.md*
