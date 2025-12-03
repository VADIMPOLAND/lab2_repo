# Ice Arena Booking System - Developer Guide

## Table of Contents

1. [Getting Started](#getting-started)
2. [Project Structure](#project-structure)
3. [Development Setup](#development-setup)
4. [Architecture Overview](#architecture-overview)
5. [Code Examples](#code-examples)
6. [Extending the System](#extending-the-system)
7. [Testing](#testing)
8. [Deployment](#deployment)
9. [Common Development Tasks](#common-development-tasks)

---

## Getting Started

### Prerequisites

- Visual Studio 2019 or later
- .NET Framework 4.7.2 or .NET 6.0+
- SQL Server 2016 or later
- Basic knowledge of C#, Windows Forms, and TCP/IP

### Quick Start

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd IceArena
   ```

2. **Set up the database**
   ```sql
   CREATE DATABASE Ice_Arena;
   -- Run database schema scripts
   ```

3. **Configure connection string**
   Edit `Program.cs`:
   ```csharp
   private const string ConnectionString = 
       "Data Source=YOUR_SERVER\\SQLEXPRESS;Initial Catalog=Ice_Arena;...";
   ```

4. **Build the solution**
   ```bash
   dotnet build IceArena.sln
   ```

5. **Run the server**
   ```bash
   dotnet run --project IceArena.Server
   ```

6. **Run the client**
   ```bash
   dotnet run --project IceArena.Client
   ```

---

## Project Structure

```
IceArena/
├── Server/
│   ├── Program.cs                 # TCP server and request handlers
│   └── ServerEncryptionHelper.cs  # Server-side encryption
├── Client/
│   ├── Forms/
│   │   ├── Form1.cs              # Login form
│   │   ├── RegisterForm.cs       # Registration form
│   │   ├── ClientForm.cs         # Main client interface
│   │   ├── BookingForm.cs        # Booking creation
│   │   ├── ProfileForm.cs        # User profile
│   │   ├── AdminForm.cs          # Admin panel
│   │   └── SupportForm.cs        # Support form
│   ├── Tabs/
│   │   ├── ScheduleTab.cs        # Schedule management (admin)
│   │   ├── BookingsTab.cs        # Bookings management (admin)
│   │   ├── UsersTab.cs           # User management (admin)
│   │   ├── AnalyticsTab.cs       # Analytics (admin)
│   │   └── SupportTab.cs         # Support management (admin)
│   ├── Services/
│   │   └── DatabaseService.cs    # Client-side DB operations
│   ├── Models/
│   │   └── DataModels.cs         # Data classes (Booking, Ticket, Review)
│   └── Helpers/
│       └── EncryptionHelper.cs   # Client-side encryption
└── Shared/
    └── Constants.cs               # Shared constants
```

---

## Development Setup

### Visual Studio Configuration

1. **Set startup projects**
   - Right-click solution → Properties
   - Select "Multiple startup projects"
   - Set Server to "Start"
   - Set Client to "Start"

2. **Configure build order**
   - Ensure Server builds before Client
   - Project Dependencies: Client depends on Shared (if applicable)

3. **Enable debugging**
   - Set breakpoints in both projects
   - Use "Debug → Attach to Process" for running instances

---

### Database Setup

#### Schema

```sql
-- Users table
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Email NVARCHAR(255) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(50) NOT NULL DEFAULT 'Client',
    RegDate DATETIME NOT NULL DEFAULT GETDATE()
);

-- Schedule table
CREATE TABLE Schedule (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Date DATE NOT NULL,
    TimeSlot NVARCHAR(50) NOT NULL,
    BreakSlot NVARCHAR(50),
    DayOfWeek NVARCHAR(20),
    Capacity INT NOT NULL DEFAULT 50,
    AvailableSeats INT NOT NULL DEFAULT 50,
    Status NVARCHAR(50) NOT NULL DEFAULT 'ДОСТУПНО'
);

-- Bookings table
CREATE TABLE Bookings (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    ScheduleId INT NOT NULL FOREIGN KEY REFERENCES Schedule(Id),
    Status NVARCHAR(50) NOT NULL DEFAULT 'Booked',
    BookingDate DATETIME NOT NULL DEFAULT GETDATE()
);

-- Tickets table
CREATE TABLE Tickets (
    Id INT PRIMARY KEY IDENTITY(1,1),
    BookingId INT NOT NULL FOREIGN KEY REFERENCES Bookings(Id),
    Type NVARCHAR(50) NOT NULL,
    Quantity INT NOT NULL,
    Price DECIMAL(10,2) NOT NULL
);

-- Rentals table (optional, for skate rentals)
CREATE TABLE Rentals (
    Id INT PRIMARY KEY IDENTITY(1,1),
    TicketId INT NOT NULL FOREIGN KEY REFERENCES Tickets(Id),
    SkateSize NVARCHAR(20),
    SkateType NVARCHAR(50)
);

-- Reviews table
CREATE TABLE Reviews (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    Rating INT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Text NVARCHAR(MAX) NOT NULL,
    Date DATETIME NOT NULL DEFAULT GETDATE(),
    IsApproved BIT NOT NULL DEFAULT 1
);

-- ArenaMetrics table (for admin analytics)
CREATE TABLE ArenaMetrics (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Date DATE NOT NULL,
    Income DECIMAL(10,2) NOT NULL,
    Attendance INT NOT NULL,
    Electricity DECIMAL(10,2),
    Notes NVARCHAR(MAX)
);
```

#### Sample Data

```sql
-- Create admin user (password: "admin" encrypted and hashed)
INSERT INTO Users (Email, PasswordHash, Role) 
VALUES ('admin@example.com', '<hashed_password>', 'Admin');

-- Create sample schedule
DECLARE @StartDate DATE = CAST(GETDATE() AS DATE);
DECLARE @i INT = 0;

WHILE @i < 7
BEGIN
    INSERT INTO Schedule (Date, TimeSlot, BreakSlot, DayOfWeek, Capacity, AvailableSeats)
    VALUES 
        (DATEADD(DAY, @i, @StartDate), '10:00-10:45', '10:45-11:00', DATENAME(WEEKDAY, DATEADD(DAY, @i, @StartDate)), 50, 50),
        (DATEADD(DAY, @i, @StartDate), '12:00-12:45', '12:45-13:00', DATENAME(WEEKDAY, DATEADD(DAY, @i, @StartDate)), 50, 50),
        (DATEADD(DAY, @i, @StartDate), '14:00-14:45', '14:45-15:00', DATENAME(WEEKDAY, DATEADD(DAY, @i, @StartDate)), 50, 50),
        (DATEADD(DAY, @i, @StartDate), '16:00-16:45', '16:45-17:00', DATENAME(WEEKDAY, DATEADD(DAY, @i, @StartDate)), 50, 50),
        (DATEADD(DAY, @i, @StartDate), '18:00-18:45', '18:45-19:00', DATENAME(WEEKDAY, DATEADD(DAY, @i, @StartDate)), 50, 50),
        (DATEADD(DAY, @i, @StartDate), '20:00-20:45', '20:45-21:00', DATENAME(WEEKDAY, DATEADD(DAY, @i, @StartDate)), 50, 50);
    
    SET @i = @i + 1;
END;
```

---

## Architecture Overview

### Communication Flow

```
[Client] ←→ TCP Socket ←→ [Server] ←→ [SQL Server]
```

1. **Client** sends JSON request over TCP
2. **Server** parses request, validates, and queries database
3. **Server** formats response as JSON and sends back
4. **Client** parses response and updates UI

### Request-Response Cycle

```
Client                          Server
  |                               |
  |-- Connect to 127.0.0.1:8888 -|
  |                               |
  |-- Send JSON request --------->|
  |                               |-- Parse JSON
  |                               |-- Validate request
  |                               |-- Query database
  |                               |-- Format response
  |<------- Send JSON response --|
  |                               |
  |-- Parse response              |
  |-- Update UI                   |
  |                               |
```

### Threading Model

- **Server**: Asynchronous, handles multiple clients concurrently
- **Client**: Main UI thread + background async operations

---

## Code Examples

### 1. Adding a New Server Command

**Step 1: Add handler in Program.cs**

```csharp
// In HandleJsonRequest method
object response = command switch
{
    "login" => await HandleLogin(root),
    "register" => await HandleRegister(root),
    // ... existing commands
    "my_new_command" => await HandleMyNewCommand(root), // Add this
    _ => new { Success = false, Error = "Неизвестная команда" }
};
```

**Step 2: Implement the handler**

```csharp
private static async Task<object> HandleMyNewCommand(JsonElement root)
{
    // 1. Extract parameters
    if (!root.TryGetProperty("Parameter1", out JsonElement param1Elem))
    {
        return new { Success = false, Error = "Missing Parameter1" };
    }
    
    string param1 = param1Elem.GetString();
    
    // 2. Validate inputs
    if (string.IsNullOrEmpty(param1))
    {
        return new { Success = false, Error = "Parameter1 cannot be empty" };
    }
    
    try
    {
        // 3. Database operation
        using var conn = new SqlConnection(ConnectionString);
        await conn.OpenAsync();
        
        string sql = "SELECT * FROM MyTable WHERE Column1 = @Param1";
        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Param1", param1);
        
        using var reader = await cmd.ExecuteReaderAsync();
        var results = new List<object>();
        
        while (reader.Read())
        {
            results.Add(new
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
                // ... more fields
            });
        }
        
        // 4. Return success response
        return new { Success = true, Data = results };
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error in HandleMyNewCommand: {ex.Message}");
        return new { Success = false, Error = "Database error" };
    }
}
```

**Step 3: Call from client**

```csharp
var response = await SendServerRequest(new
{
    Command = "my_new_command",
    Parameter1 = "value"
});

if (response.TryGetProperty("Success", out var success) && success.GetBoolean())
{
    if (response.TryGetProperty("Data", out var data))
    {
        // Process data
    }
}
```

---

### 2. Creating a New Form

**Step 1: Create the form class**

```csharp
using System;
using System.Drawing;
using System.Windows.Forms;

namespace IceArena.Client
{
    public partial class MyNewForm : Form
    {
        private ClientForm parentForm;
        private int userId;
        
        // UI controls
        private Button btnSubmit;
        private TextBox txtInput;
        
        public MyNewForm(ClientForm parent, int userId)
        {
            this.parentForm = parent;
            this.userId = userId;
            
            InitializeComponents();
            SetupUI();
        }
        
        private void InitializeComponents()
        {
            this.Text = "My New Form";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
        }
        
        private void SetupUI()
        {
            // Create UI elements
            txtInput = new TextBox
            {
                Location = new Point(20, 20),
                Size = new Size(300, 30),
                Font = new Font("Segoe UI", 10)
            };
            this.Controls.Add(txtInput);
            
            btnSubmit = new Button
            {
                Text = "Submit",
                Location = new Point(20, 60),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSubmit.FlatAppearance.BorderSize = 0;
            btnSubmit.Click += BtnSubmit_Click;
            this.Controls.Add(btnSubmit);
        }
        
        private async void BtnSubmit_Click(object sender, EventArgs e)
        {
            string input = txtInput.Text.Trim();
            
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("Please enter a value");
                return;
            }
            
            try
            {
                btnSubmit.Enabled = false;
                btnSubmit.Text = "Processing...";
                
                var response = await parentForm.SendServerRequest(new
                {
                    Command = "my_new_command",
                    UserId = userId,
                    Input = input
                });
                
                if (response.TryGetProperty("Success", out var s) && s.GetBoolean())
                {
                    MessageBox.Show("Success!");
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    string error = response.TryGetProperty("Error", out var e)
                        ? e.GetString()
                        : "Unknown error";
                    MessageBox.Show($"Error: {error}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
            finally
            {
                btnSubmit.Enabled = true;
                btnSubmit.Text = "Submit";
            }
        }
    }
}
```

**Step 2: Add Designer file (optional)**

```csharp
namespace IceArena.Client
{
    partial class MyNewForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
```

**Step 3: Open the form**

```csharp
// From ClientForm or other form
var myForm = new MyNewForm(this, currentUserId);
if (myForm.ShowDialog() == DialogResult.OK)
{
    // Handle successful result
}
```

---

### 3. Adding a Data Model

**Step 1: Define the model in DataModels.cs**

```csharp
public class MyNewModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedDate { get; set; }
    public bool IsActive { get; set; }
    
    // Computed property
    public string DisplayName => $"{Name} (ID: {Id})";
    
    // Validation
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(Name) && CreatedDate <= DateTime.Now;
    }
}
```

**Step 2: Use the model**

```csharp
// Parse from server response
var model = new MyNewModel
{
    Id = jsonElement.GetProperty("Id").GetInt32(),
    Name = jsonElement.GetProperty("Name").GetString(),
    CreatedDate = DateTime.Parse(jsonElement.GetProperty("CreatedDate").GetString()),
    IsActive = jsonElement.GetProperty("IsActive").GetBoolean()
};

if (model.IsValid())
{
    // Use the model
    Console.WriteLine(model.DisplayName);
}
```

---

### 4. Implementing Caching

To improve performance, implement client-side caching:

```csharp
public class CacheService
{
    private static Dictionary<string, (DateTime, object)> cache = 
        new Dictionary<string, (DateTime, object)>();
    
    private static TimeSpan defaultExpiration = TimeSpan.FromMinutes(5);
    
    public static void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        var exp = expiration ?? defaultExpiration;
        cache[key] = (DateTime.Now.Add(exp), value);
    }
    
    public static bool TryGet<T>(string key, out T value)
    {
        if (cache.TryGetValue(key, out var cached))
        {
            if (cached.Item1 > DateTime.Now)
            {
                value = (T)cached.Item2;
                return true;
            }
            else
            {
                // Expired, remove
                cache.Remove(key);
            }
        }
        
        value = default;
        return false;
    }
    
    public static void Clear()
    {
        cache.Clear();
    }
}

// Usage
private async Task<List<Booking>> LoadUserBookings(int userId)
{
    string cacheKey = $"bookings_{userId}";
    
    // Try cache first
    if (CacheService.TryGet(cacheKey, out List<Booking> cached))
    {
        return cached;
    }
    
    // Fetch from server
    var response = await SendServerRequest(new
    {
        Command = "get_user_bookings",
        UserId = userId
    });
    
    var bookings = ParseBookingsFromResponse(response);
    
    // Store in cache
    CacheService.Set(cacheKey, bookings, TimeSpan.FromMinutes(2));
    
    return bookings;
}
```

---

## Extending the System

### Adding a New Ticket Type

**Step 1: Update ticket prices (BookingForm.cs)**

```csharp
private const decimal ADULT_PRICE = 6.00m;
private const decimal CHILD_PRICE = 4.00m;
private const decimal SENIOR_PRICE = 4.00m;
private const decimal VIP_PRICE = 12.00m;  // New ticket type
```

**Step 2: Add UI for new ticket type**

```csharp
private NumericUpDown numVip;

private void InitializeLogicControls()
{
    // ... existing controls
    numVip = new NumericUpDown { Maximum = 100, Value = 0 };
    numVip.ValueChanged += UpdateTotals;
}

// In CreateTicketsPanel
ticketsPanel.Controls.Add(CreateCounterRow("VIP (Premium)", 
    $"{(int)VIP_PRICE} BYN", numVip, rowY, rowWidth));
```

**Step 3: Include in booking request**

```csharp
private async void BtnConfirm_Click(object sender, EventArgs e)
{
    // ... existing code
    
    if (numVip.Value > 0)
        ticketsList.Add(new TicketDto 
        { 
            Type = "VIP", 
            Quantity = (int)numVip.Value, 
            Price = VIP_PRICE 
        });
    
    // ... rest of code
}
```

**Step 4: Update display logic**

```csharp
// In ProfileForm.cs
private string GetTicketTypeDisplayName(string type)
{
    switch (type.ToLower().Trim())
    {
        case "adult": return "✅ Взрослый";
        case "child": return "🧒 Детский";
        case "senior": return "👴 Пенсионер";
        case "vip": return "💎 VIP";  // Add this
        default: return $"✅ {type}";
    }
}
```

---

### Adding Email Notifications

**Step 1: Add email configuration to server**

```csharp
// In Program.cs
private static string SmtpServer = "smtp.gmail.com";
private static int SmtpPort = 587;
private static string SmtpUsername = "your-email@gmail.com";
private static string SmtpPassword = "your-app-password";
```

**Step 2: Create email helper**

```csharp
using System.Net;
using System.Net.Mail;

private static async Task SendEmailAsync(string to, string subject, string body)
{
    try
    {
        using (var client = new SmtpClient(SmtpServer, SmtpPort))
        {
            client.Credentials = new NetworkCredential(SmtpUsername, SmtpPassword);
            client.EnableSsl = true;
            
            var message = new MailMessage
            {
                From = new MailAddress(SmtpUsername, "Ice Arena"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(to);
            
            await client.SendMailAsync(message);
            Console.WriteLine($"✅ Email sent to {to}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Email error: {ex.Message}");
    }
}
```

**Step 3: Send notification on booking**

```csharp
private static async Task<object> HandleCreateBooking(JsonElement root)
{
    // ... create booking logic
    
    if (success)
    {
        // Get user email
        string userEmail = await GetUserEmail(userId);
        
        // Send confirmation email
        await SendEmailAsync(
            to: userEmail,
            subject: "Booking Confirmation - Ice Arena",
            body: $@"
                <h2>Booking Confirmed</h2>
                <p>Your booking #{bookingId} has been confirmed.</p>
                <p>Date: {bookingDate}</p>
                <p>Time: {timeSlot}</p>
                <p>Total: {totalCost} BYN</p>
            "
        );
    }
    
    // ... return response
}
```

---

### Adding Payment Integration

**Step 1: Create payment service**

```csharp
public class PaymentService
{
    private const string ApiKey = "your_payment_api_key";
    private const string ApiEndpoint = "https://payment-gateway.com/api";
    
    public static async Task<PaymentResult> ProcessPayment(
        decimal amount, string cardNumber, string cvv, string expiry)
    {
        try
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
                
                var request = new
                {
                    amount = amount,
                    currency = "BYN",
                    card_number = cardNumber,
                    cvv = cvv,
                    expiry = expiry
                };
                
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await client.PostAsync($"{ApiEndpoint}/charge", content);
                var responseJson = await response.Content.ReadAsStringAsync();
                
                var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
                
                return new PaymentResult
                {
                    Success = result.GetProperty("success").GetBoolean(),
                    TransactionId = result.GetProperty("transaction_id").GetString(),
                    Message = result.TryGetProperty("message", out var msg) 
                        ? msg.GetString() 
                        : null
                };
            }
        }
        catch (Exception ex)
        {
            return new PaymentResult
            {
                Success = false,
                Message = $"Payment error: {ex.Message}"
            };
        }
    }
}

public class PaymentResult
{
    public bool Success { get; set; }
    public string TransactionId { get; set; }
    public string Message { get; set; }
}
```

**Step 2: Add payment UI to BookingForm**

```csharp
private void CreatePaymentSection()
{
    Panel paymentPanel = new Panel
    {
        Location = new Point(20, currentY),
        Size = new Size(this.Width - 60, 200),
        BackColor = Color.White
    };
    
    // Card number
    TextBox txtCardNumber = new TextBox
    {
        Location = new Point(20, 50),
        Size = new Size(300, 30),
        PlaceholderText = "Card Number"
    };
    paymentPanel.Controls.Add(txtCardNumber);
    
    // CVV
    TextBox txtCVV = new TextBox
    {
        Location = new Point(340, 50),
        Size = new Size(100, 30),
        PlaceholderText = "CVV",
        MaxLength = 3
    };
    paymentPanel.Controls.Add(txtCVV);
    
    // Expiry
    TextBox txtExpiry = new TextBox
    {
        Location = new Point(460, 50),
        Size = new Size(100, 30),
        PlaceholderText = "MM/YY"
    };
    paymentPanel.Controls.Add(txtExpiry);
    
    this.Controls.Add(paymentPanel);
}
```

**Step 3: Process payment before booking**

```csharp
private async void BtnConfirm_Click(object sender, EventArgs e)
{
    decimal totalCost = CalculateTotalCost();
    
    // Process payment first
    var paymentResult = await PaymentService.ProcessPayment(
        totalCost,
        txtCardNumber.Text,
        txtCVV.Text,
        txtExpiry.Text
    );
    
    if (!paymentResult.Success)
    {
        MessageBox.Show($"Payment failed: {paymentResult.Message}");
        return;
    }
    
    // Payment successful, create booking
    var request = new
    {
        Command = "create_booking",
        UserId = userId,
        ScheduleId = scheduleId,
        Tickets = ticketsList,
        TransactionId = paymentResult.TransactionId
    };
    
    // ... send request
}
```

---

## Testing

### Unit Testing Example

```csharp
using Xunit;

public class EncryptionHelperTests
{
    [Fact]
    public void Encrypt_ReturnsNonEmptyString()
    {
        // Arrange
        string input = "test password";
        
        // Act
        string encrypted = EncryptionHelper.Encrypt(input);
        
        // Assert
        Assert.NotNull(encrypted);
        Assert.NotEmpty(encrypted);
        Assert.NotEqual(input, encrypted);
    }
    
    [Fact]
    public void Decrypt_ReturnsOriginalString()
    {
        // Arrange
        string original = "test password";
        string encrypted = EncryptionHelper.Encrypt(original);
        
        // Act
        string decrypted = EncryptionHelper.Decrypt(encrypted);
        
        // Assert
        Assert.Equal(original, decrypted);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Encrypt_EmptyOrNull_ReturnsNull(string input)
    {
        // Act
        string result = EncryptionHelper.Encrypt(input);
        
        // Assert
        Assert.Null(result);
    }
}
```

### Integration Testing

```csharp
public class BookingIntegrationTests
{
    private TestServer server;
    private TcpClient client;
    
    [SetUp]
    public void Setup()
    {
        server = new TestServer();
        server.Start();
        
        client = new TcpClient();
        client.Connect("127.0.0.1", 8888);
    }
    
    [TearDown]
    public void TearDown()
    {
        client?.Close();
        server?.Stop();
    }
    
    [Test]
    public async Task CreateBooking_WithValidData_Succeeds()
    {
        // Arrange
        var request = new
        {
            Command = "create_booking",
            UserId = 1,
            ScheduleId = 1,
            Tickets = new[]
            {
                new { Type = "Adult", Quantity = 2, Price = 6.00m }
            }
        };
        
        // Act
        var response = await SendRequest(request);
        
        // Assert
        Assert.IsTrue(response.GetProperty("Success").GetBoolean());
        Assert.IsTrue(response.TryGetProperty("BookingId", out var bookingId));
        Assert.Greater(bookingId.GetInt32(), 0);
    }
    
    private async Task<JsonElement> SendRequest(object request)
    {
        var stream = client.GetStream();
        byte[] data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
        await stream.WriteAsync(data, 0, data.Length);
        
        byte[] buffer = new byte[4096];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        
        return JsonSerializer.Deserialize<JsonElement>(json);
    }
}
```

---

## Deployment

### Build Release Version

```bash
# Build in Release mode
dotnet build -c Release

# Publish self-contained
dotnet publish -c Release -r win-x64 --self-contained true
```

### Deployment Checklist

- [ ] Update connection string for production database
- [ ] Change encryption keys (generate new secure keys)
- [ ] Configure firewall rules for server port
- [ ] Set up SSL/TLS for production
- [ ] Create database backup strategy
- [ ] Set up logging and monitoring
- [ ] Test all functionality in production-like environment
- [ ] Prepare rollback plan
- [ ] Document deployment process

### Configuration Management

Create `appsettings.json` for production:

```json
{
  "Server": {
    "Port": 8888,
    "IP": "0.0.0.0",
    "MaxConnections": 100
  },
  "Database": {
    "ConnectionString": "Data Source=PROD_SERVER;Initial Catalog=Ice_Arena;..."
  },
  "Security": {
    "EncryptionKey": "GENERATE_NEW_KEY",
    "EncryptionIV": "GENERATE_NEW_IV"
  },
  "Logging": {
    "Level": "Information",
    "Path": "C:\\Logs\\IceArena"
  }
}
```

---

## Common Development Tasks

### Task 1: Debug Server Communication

```csharp
// Add detailed logging to server
private static async Task HandleJsonRequest(NetworkStream stream, string request)
{
    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
    Console.WriteLine($"[{timestamp}] Received: {request}");
    
    try
    {
        // ... process request
        
        Console.WriteLine($"[{timestamp}] Response: {responseJson}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[{timestamp}] ERROR: {ex.Message}");
        Console.WriteLine($"[{timestamp}] Stack: {ex.StackTrace}");
    }
}
```

### Task 2: Optimize Database Queries

```csharp
// Add indexes
CREATE INDEX IX_Bookings_UserId ON Bookings(UserId);
CREATE INDEX IX_Bookings_ScheduleId ON Bookings(ScheduleId);
CREATE INDEX IX_Schedule_Date ON Schedule(Date);

// Use parameterized queries
string sql = @"
    SELECT b.*, s.Date, s.TimeSlot
    FROM Bookings b
    JOIN Schedule s ON b.ScheduleId = s.Id
    WHERE b.UserId = @UserId AND s.Date >= @StartDate
    ORDER BY s.Date";

using var cmd = new SqlCommand(sql, conn);
cmd.Parameters.AddWithValue("@UserId", userId);
cmd.Parameters.AddWithValue("@StartDate", DateTime.Today);
```

### Task 3: Handle Connection Errors Gracefully

```csharp
public async Task<JsonElement> SendServerRequestWithRetry(object request, int maxRetries = 3)
{
    int attempts = 0;
    Exception lastException = null;
    
    while (attempts < maxRetries)
    {
        try
        {
            return await SendServerRequest(request);
        }
        catch (Exception ex)
        {
            lastException = ex;
            attempts++;
            
            if (attempts < maxRetries)
            {
                // Exponential backoff
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempts)));
            }
        }
    }
    
    throw new Exception($"Failed after {maxRetries} attempts", lastException);
}
```

### Task 4: Add Localization

```csharp
public class LocalizationService
{
    private static Dictionary<string, Dictionary<string, string>> translations = 
        new Dictionary<string, Dictionary<string, string>>
    {
        ["en"] = new Dictionary<string, string>
        {
            ["login"] = "Login",
            ["register"] = "Register",
            ["booking_success"] = "Booking successful!"
        },
        ["ru"] = new Dictionary<string, string>
        {
            ["login"] = "Вход",
            ["register"] = "Регистрация",
            ["booking_success"] = "Бронирование успешно!"
        }
    };
    
    private static string currentLanguage = "ru";
    
    public static string Get(string key)
    {
        if (translations.TryGetValue(currentLanguage, out var lang))
        {
            if (lang.TryGetValue(key, out var value))
            {
                return value;
            }
        }
        return key; // Fallback to key itself
    }
    
    public static void SetLanguage(string language)
    {
        if (translations.ContainsKey(language))
        {
            currentLanguage = language;
        }
    }
}

// Usage
btnLogin.Text = LocalizationService.Get("login");
MessageBox.Show(LocalizationService.Get("booking_success"));
```

---

## Performance Tips

1. **Use connection pooling** - Already enabled by default in SqlConnection
2. **Cache static data** - Schedule, prices, etc.
3. **Minimize UI updates** - Batch DataGridView updates
4. **Use async/await** - Never block UI thread
5. **Dispose resources** - Always use `using` statements
6. **Optimize JSON parsing** - Use `JsonElement` instead of deserializing to objects when possible

---

## Security Checklist

- [ ] Never log sensitive data (passwords, payment info)
- [ ] Use parameterized queries (prevent SQL injection)
- [ ] Encrypt all passwords (AES in transit, SHA256 hash in database)
- [ ] Validate all user inputs
- [ ] Implement rate limiting on server
- [ ] Use HTTPS/TLS in production
- [ ] Regular security audits
- [ ] Keep dependencies updated

---

## Resources

- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [Windows Forms Guide](https://docs.microsoft.com/en-us/dotnet/desktop/winforms/)
- [SQL Server Best Practices](https://docs.microsoft.com/en-us/sql/)
- [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview)

---

## Support

For development questions:
- Check existing documentation
- Review code comments
- Contact development team

---

*End of Developer Guide*
