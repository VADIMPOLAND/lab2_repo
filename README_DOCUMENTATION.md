# Ice Arena Booking System - Documentation

![Ice Arena](https://img.shields.io/badge/Ice%20Arena-Booking%20System-blue)
![Version](https://img.shields.io/badge/version-1.0-green)
![.NET](https://img.shields.io/badge/.NET-Framework-purple)
![Windows Forms](https://img.shields.io/badge/Windows-Forms-lightblue)

## 📋 Overview

The **Ice Arena Booking System** is a comprehensive Windows Forms application for managing ice rink bookings, user registrations, and arena operations. The system consists of a TCP-based server and a rich client application with modern UI design.

### Key Features

✅ **User Management**
- Secure registration and authentication
- Role-based access (Client/Admin)
- Guest mode for browsing
- Profile management

✅ **Booking System**
- Real-time schedule viewing
- Seat availability tracking
- Multiple ticket types (Adult, Child, Senior)
- Ice skate rental integration
- Booking history and cancellation

✅ **Reviews & Feedback**
- 5-star rating system
- User reviews and testimonials
- Admin moderation

✅ **Admin Panel**
- User management
- Analytics and metrics
- Booking oversight
- Schedule management
- Support ticket system

✅ **Security**
- AES encryption for passwords in transit
- SHA256 hashing for password storage
- Secure TCP communication
- Input validation and sanitization

---

## 📚 Documentation

This project includes comprehensive documentation for different audiences:

### For End Users

📖 **[User Guide](USER_GUIDE.md)**  
Complete guide for end users explaining how to:
- Register and log in
- Make bookings
- Manage reservations
- Submit reviews
- Use guest mode
- Navigate the interface

**Best for:** Arena customers, general users

---

### For Developers

🔧 **[Developer Guide](DEVELOPER_GUIDE.md)**  
In-depth guide for developers covering:
- Project structure and architecture
- Development setup
- Database schema
- Adding new features
- Code examples and patterns
- Testing strategies
- Deployment procedures

**Best for:** Software developers, contributors, maintainers

---

### For API Integration

🔌 **[API Documentation](API_DOCUMENTATION.md)**  
Complete API reference including:
- All server endpoints
- Request/response formats
- Data models
- Client services
- Encryption methods
- Error handling
- Usage examples

**Best for:** Backend developers, API integrators

---

### Quick Reference

⚡ **[API Quick Reference](API_QUICK_REFERENCE.md)**  
Fast lookup guide featuring:
- Endpoint summaries
- Request examples
- Response formats
- Common code snippets
- Connection details

**Best for:** Quick lookups during development

---

## 🚀 Quick Start

### Prerequisites

- **Operating System**: Windows 10 or later
- **Development**: Visual Studio 2019+ or Rider
- **Database**: SQL Server 2016+
- **Framework**: .NET Framework 4.7.2 or .NET 6.0+

### Installation

1. **Clone the Repository**
   ```bash
   git clone <repository-url>
   cd IceArena
   ```

2. **Set Up Database**
   ```sql
   CREATE DATABASE Ice_Arena;
   -- Run schema from DEVELOPER_GUIDE.md
   ```

3. **Configure Connection String**
   
   Edit `Program.cs`:
   ```csharp
   private const string ConnectionString = 
       "Data Source=YOUR_SERVER;Initial Catalog=Ice_Arena;Integrated Security=True;TrustServerCertificate=True;";
   ```

4. **Build Solution**
   ```bash
   dotnet build IceArena.sln
   ```

5. **Run Server**
   ```bash
   cd IceArena.Server
   dotnet run
   ```
   
   ✅ Wait for: "Сервер запущен на 127.0.0.1:8888"

6. **Run Client**
   ```bash
   cd IceArena.Client
   dotnet run
   ```

---

## 🏗️ Architecture

### System Components

```
┌─────────────────┐
│  Client App     │
│  (Windows Forms)│
└────────┬────────┘
         │ TCP/IP (JSON)
         │ Port 8888
┌────────▼────────┐
│  Server App     │
│  (TCP Server)   │
└────────┬────────┘
         │ ADO.NET
         │
┌────────▼────────┐
│  SQL Server     │
│  (Ice_Arena DB) │
└─────────────────┘
```

### Technology Stack

| Layer | Technology |
|-------|-----------|
| **Client UI** | Windows Forms, GDI+ |
| **Communication** | TCP/IP Sockets, JSON |
| **Server** | Async TCP Server |
| **Database** | SQL Server |
| **ORM** | ADO.NET (SqlConnection) |
| **Serialization** | System.Text.Json |
| **Encryption** | AES (System.Security.Cryptography) |

---

## 📂 Project Structure

```
IceArena/
├── 📄 README_DOCUMENTATION.md      # This file
├── 📄 API_DOCUMENTATION.md          # Complete API reference
├── 📄 DEVELOPER_GUIDE.md            # Developer guide
├── 📄 USER_GUIDE.md                 # End-user guide
├── 📄 API_QUICK_REFERENCE.md        # Quick API lookup
├── 
├── 🖥️ Server/
│   ├── Program.cs                   # TCP server & handlers
│   └── ServerEncryptionHelper.cs    # Server-side encryption
│
├── 💻 Client/
│   ├── 📁 Forms/
│   │   ├── Form1.cs                 # Login form
│   │   ├── RegisterForm.cs          # Registration
│   │   ├── ClientForm.cs            # Main interface
│   │   ├── BookingForm.cs           # Booking creation
│   │   ├── ProfileForm.cs           # User profile
│   │   ├── AdminForm.cs             # Admin panel
│   │   └── SupportForm.cs           # Support
│   │
│   ├── 📁 Tabs/ (Admin)
│   │   ├── ScheduleTab.cs
│   │   ├── BookingsTab.cs
│   │   ├── UsersTab.cs
│   │   ├── AnalyticsTab.cs
│   │   └── SupportTab.cs
│   │
│   ├── 📁 Services/
│   │   └── DatabaseService.cs       # DB operations
│   │
│   ├── 📁 Models/
│   │   └── DataModels.cs            # Booking, Ticket, Review
│   │
│   └── 📁 Helpers/
│       └── EncryptionHelper.cs      # Client encryption
│
└── 📁 Database/
    └── schema.sql                    # Database schema
```

---

## 🔐 Security

### Password Security Flow

1. **Client** encrypts password with AES
2. **Transmission** over TCP as Base64
3. **Server** decrypts AES-encrypted password
4. **Server** hashes with SHA256
5. **Storage** in database as SHA256 hash

### Security Features

- ✅ AES-256 encryption for transit
- ✅ SHA-256 hashing for storage
- ✅ Parameterized queries (SQL injection prevention)
- ✅ Input validation
- ✅ Timeout protection
- ⚠️ Consider TLS/SSL for production

---

## 🎯 Use Cases

### For Customers

1. **Browse Schedule** → View available time slots
2. **Register Account** → Create user profile
3. **Book Session** → Reserve ice time
4. **Add Skate Rental** → Rent equipment
5. **View History** → Check past bookings
6. **Leave Review** → Rate experience

### For Administrators

1. **View Analytics** → Monitor revenue and attendance
2. **Manage Users** → Add/edit/remove accounts
3. **Oversee Bookings** → View all reservations
4. **Update Schedule** → Add/modify time slots
5. **Handle Support** → Respond to inquiries

---

## 📊 Database Schema

### Core Tables

**Users** - User accounts and authentication  
**Schedule** - Available time slots  
**Bookings** - Reservations  
**Tickets** - Ticket details per booking  
**Rentals** - Skate rental records  
**Reviews** - User feedback  
**ArenaMetrics** - Performance analytics

**Full schema:** See [Developer Guide](DEVELOPER_GUIDE.md#database-setup)

---

## 🔌 API Endpoints

### Authentication
- `login` - User authentication
- `register` - New account creation

### Schedule
- `get_schedule` - Retrieve available slots

### Bookings
- `create_booking` - Make reservation
- `get_user_bookings` - View user's bookings
- `cancel_booking` - Cancel reservation

### Reviews
- `get_reviews` - Fetch all reviews
- `add_review` - Submit review
- `get_user_reviews` - User's reviews

### Admin
- `get_arena_metrics` - Performance data
- `get_user_info` - User details

**Full API reference:** See [API Documentation](API_DOCUMENTATION.md)

---

## 💡 Examples

### Quick Example: Making a Booking

```csharp
// 1. Connect to server
using (var client = new TcpClient())
{
    await client.ConnectAsync("127.0.0.1", 8888);
    using (var stream = client.GetStream())
    {
        // 2. Create booking request
        var request = new
        {
            Command = "create_booking",
            UserId = 123,
            ScheduleId = 1,
            Tickets = new[]
            {
                new { Type = "Adult", Quantity = 2, Price = 6.00m },
                new { Type = "Child", Quantity = 1, Price = 4.00m }
            }
        };
        
        // 3. Send request
        string json = JsonSerializer.Serialize(request);
        byte[] data = Encoding.UTF8.GetBytes(json);
        await stream.WriteAsync(data, 0, data.Length);
        
        // 4. Read response
        byte[] buffer = new byte[4096];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        
        // 5. Parse result
        var result = JsonSerializer.Deserialize<JsonElement>(response);
        if (result.GetProperty("Success").GetBoolean())
        {
            int bookingId = result.GetProperty("BookingId").GetInt32();
            Console.WriteLine($"Booking created: #{bookingId}");
        }
    }
}
```

**More examples:** See [API Documentation](API_DOCUMENTATION.md#usage-examples)

---

## 🧪 Testing

### Manual Testing Checklist

- [ ] User registration and login
- [ ] Guest mode access
- [ ] Schedule viewing
- [ ] Booking creation
- [ ] Booking cancellation
- [ ] Profile viewing
- [ ] Review submission
- [ ] Admin panel access
- [ ] Analytics viewing
- [ ] Server connection handling

**Detailed testing guide:** See [Developer Guide](DEVELOPER_GUIDE.md#testing)

---

## 🐛 Troubleshooting

### Common Issues

**Issue: "Server not running"**  
→ Start `IceArena.Server.exe` before client

**Issue: "Connection timeout"**  
→ Check firewall settings, verify port 8888 is open

**Issue: "Database connection error"**  
→ Verify SQL Server is running, check connection string

**Issue: "Login fails"**  
→ Verify credentials, check encryption keys match

**Full troubleshooting:** See [User Guide](USER_GUIDE.md#troubleshooting)

---

## 📈 Version History

| Version | Date | Changes |
|---------|------|---------|
| **1.0** | Dec 2025 | Initial release |

### Upcoming Features

🚀 **Planned:**
- Mobile app (iOS/Android)
- Online payment integration
- Email notifications
- Booking reminders
- Loyalty rewards
- Season passes

---

## 🤝 Contributing

### How to Contribute

1. **Fork** the repository
2. **Create** feature branch (`git checkout -b feature/AmazingFeature`)
3. **Commit** changes (`git commit -m 'Add AmazingFeature'`)
4. **Push** to branch (`git push origin feature/AmazingFeature`)
5. **Open** Pull Request

### Coding Standards

- Follow C# naming conventions
- Add XML documentation comments
- Write unit tests for new features
- Update documentation accordingly

**Development guide:** See [Developer Guide](DEVELOPER_GUIDE.md)

---

## 📞 Support

### Getting Help

📧 **Email:** support@polessu.by  
📞 **Phone:** +375 (XX) XXX-XX-XX  
🌐 **Website:** [Ice Arena Website]

### Documentation Issues

Found an error in documentation? Please:
1. Open an issue on GitHub
2. Or submit a pull request with fixes

---

## 📜 License

© 2025 Polessu Ice Arena. All rights reserved.

This project is proprietary software developed for Polessu Ice Arena.

---

## 👥 Authors

**Development Team**  
Polessu State University

**Contact:**  
support@polessu.by

---

## 🙏 Acknowledgments

- Polessu State University for project support
- All contributors and testers
- Microsoft for .NET Framework and documentation

---

## 📖 Documentation Map

Choose the right documentation for your needs:

```
┌─────────────────────────────────────────────┐
│           DOCUMENTATION MAP                 │
├─────────────────────────────────────────────┤
│                                             │
│  🎯 I want to...                           │
│                                             │
│  ┌─ USE the application                    │
│  │  → USER_GUIDE.md                        │
│  │                                          │
│  ┌─ DEVELOP new features                   │
│  │  → DEVELOPER_GUIDE.md                   │
│  │                                          │
│  ┌─ INTEGRATE with API                     │
│  │  → API_DOCUMENTATION.md                 │
│  │                                          │
│  ┌─ QUICK API lookup                       │
│  │  → API_QUICK_REFERENCE.md               │
│  │                                          │
│  └─ UNDERSTAND the system                  │
│     → README_DOCUMENTATION.md (this file)  │
│                                             │
└─────────────────────────────────────────────┘
```

---

## 🎓 Learning Path

### For New Users

1. Read **Introduction** (above)
2. Follow **Quick Start** guide
3. Review **[User Guide](USER_GUIDE.md)** for features
4. Try **Guest Mode** first
5. Create account and start booking!

### For New Developers

1. Read **Overview** and **Architecture**
2. Set up **Development Environment**
3. Study **[Developer Guide](DEVELOPER_GUIDE.md)**
4. Review **[API Documentation](API_DOCUMENTATION.md)**
5. Build and run the project
6. Start with small changes/fixes
7. Graduate to new features

### For API Integrators

1. Understand **System Architecture**
2. Review **[API Documentation](API_DOCUMENTATION.md)**
3. Keep **[Quick Reference](API_QUICK_REFERENCE.md)** handy
4. Test endpoints with sample data
5. Implement authentication first
6. Add booking functionality
7. Implement error handling

---

## 🎉 Get Started Now!

Ready to begin? Choose your path:

### End User
👉 **[Open User Guide](USER_GUIDE.md)** to learn how to use the system

### Developer
👉 **[Open Developer Guide](DEVELOPER_GUIDE.md)** to start coding

### API Integration
👉 **[Open API Documentation](API_DOCUMENTATION.md)** for API details

### Quick Lookup
👉 **[Open Quick Reference](API_QUICK_REFERENCE.md)** for fast answers

---

**Happy Coding! ⛸️🏒**

---

*Last Updated: December 3, 2025*  
*Documentation Version: 1.0*  
*© 2025 Polessu Ice Arena*

