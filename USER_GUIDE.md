# Ice Arena Booking System - User Guide

## Table of Contents

1. [Welcome](#welcome)
2. [Getting Started](#getting-started)
3. [Registration & Login](#registration--login)
4. [Making a Booking](#making-a-booking)
5. [Managing Your Bookings](#managing-your-bookings)
6. [User Profile](#user-profile)
7. [Reviews & Feedback](#reviews--feedback)
8. [Guest Mode](#guest-mode)
9. [Admin Features](#admin-features)
10. [Troubleshooting](#troubleshooting)
11. [FAQ](#faq)

---

## Welcome

Welcome to the **Ice Arena Booking System**! This application allows you to:

- 📅 View ice rink session schedules
- 🎫 Book sessions and purchase tickets
- ⛸️ Rent ice skates
- 📊 Track your booking history
- ⭐ Leave reviews and ratings
- 💰 View your spending

---

## Getting Started

### System Requirements

- **Operating System**: Windows 10 or later
- **Internet Connection**: Required for server communication
- **Display**: Minimum 1280x720 resolution recommended

### First Time Setup

1. **Launch the Application**
   - Double-click `IceArena.Client.exe`
   - You'll see the login screen

2. **Register an Account**
   - Click "Регистрация" (Registration) button
   - Fill in your email and password
   - Click "ЗАРЕГИСТРИРОВАТЬСЯ" (Register)

3. **Or Use Guest Mode**
   - Click "Продолжить как Гость" (Continue as Guest)
   - View schedule without booking ability

---

## Registration & Login

### Creating an Account

![Registration Form](screenshots/registration.png)

**Step-by-Step:**

1. Click **"Регистрация"** button on the main screen
2. Fill in the required fields:
   - **Email**: Your email address (e.g., john@example.com)
   - **Password**: At least 6 characters
   - **Confirm Password**: Must match your password
3. Click **"ЗАРЕГИСТРИРОВАТЬСЯ"**
4. Wait for confirmation message
5. Return to login screen

**Password Requirements:**
- Minimum 6 characters
- No special restrictions
- Passwords are encrypted for security

---

### Logging In

![Login Screen](screenshots/login.png)

**Step-by-Step:**

1. Enter your **email**
2. Enter your **password**
3. Click **"ВОЙТИ В АККАУНТ"** (Login)

**Troubleshooting Login Issues:**
- ❌ "Неверный пароль" - Incorrect password
- ❌ "Пользователь не найден" - Email not registered
- ❌ "Сервер не запущен" - Contact administrator

---

## Making a Booking

### Viewing the Schedule

Once logged in, you'll see the weekly schedule:

![Schedule View](screenshots/schedule.png)

**Schedule Information:**
- **Day of Week**: Monday through Sunday
- **Date**: Calendar date
- **Time Slot**: Session start and end times
- **Capacity**: Total available spots
- **Available Seats**: Current free spots
- **Status**: 
  - 🟢 "ДОСТУПНО" (Available) - You can book
  - 🔴 "НЕТ МЕСТ" (Full) - No seats left
  - 🔵 "Вы записаны" (Your booking) - You're booked

**Action Buttons:**
- **"Записаться"** (Book) - Available sessions
- **"Отменить"** (Cancel) - Your existing bookings

---

### Creating a Booking

![Booking Form](screenshots/booking-form.png)

**Step-by-Step:**

1. **Find Available Session**
   - Look for green "ДОСТУПНО" status
   - Click the **"Записаться"** button in the row

2. **Select Tickets**
   - **Adult (18-64)**: 6.00 BYN per person
   - **Child (under 17)**: 4.00 BYN per person
   - **Senior (65+)**: 4.00 BYN per person
   
   Use **+** and **-** buttons to adjust quantities

3. **Add Skate Rental (Optional)**
   - Check **"⛸️ Добавить прокат коньков"**
   - Select skate **size** (30-46)
   - Select skate **type**:
     - Фигурные (Figure skates)
     - Хоккейные (Hockey skates)

4. **Review Total**
   - Check the **total amount** at the bottom
   - Verify ticket count

5. **Confirm Booking**
   - Click **"ОПЛАТИТЬ"** (Pay) button
   - Wait for confirmation message
   - Click OK to return to schedule

**Example Booking:**
```
Session: Wednesday, 10:00-10:45
Tickets: 2 Adults + 1 Child = 16.00 BYN
Skates: +1 pair (Size 40, Hockey)
Total: 16.00 BYN
```

---

### Booking Tips

💡 **Best Practices:**

1. **Book Early** - Popular time slots fill up fast
2. **Check Availability** - Schedule shows real-time seat counts
3. **Arrive 15 Minutes Early** - For skate fitting and preparation
4. **Bring Identification** - May be required for verification
5. **Review Cancellation Policy** - Know your rights

⚠️ **Important Notes:**

- Bookings cannot be made for past time slots
- You can only book one session per time slot
- Same-day cancellations may have restrictions
- Skate rentals are per booking, not per person

---

## Managing Your Bookings

### Viewing Your Bookings

Access your bookings from:
1. **Main Schedule** - Shows "Вы записаны" status on booked slots
2. **Profile Page** - Click "👤 МОЙ КАБИНЕТ" button

![Profile Bookings](screenshots/profile-bookings.png)

**Booking Information Displayed:**
- Booking ID
- Date and time
- Ticket types and quantities
- Total cost
- Status (Active or Completed)

---

### Canceling a Booking

![Cancel Booking](screenshots/cancel-booking.png)

**From Schedule:**
1. Find your booking (marked "Вы записаны")
2. Click **"Отменить"** button
3. Confirm cancellation
4. Seat returns to availability

**From Profile:**
1. Select booking in the list
2. Click **"🗑️ УДАЛИТЬ ВЫБРАННОЕ"**
3. Confirm deletion
4. Booking is canceled

**Cancellation Rules:**
- ✅ Can cancel active future bookings
- ❌ Cannot cancel completed past bookings
- ⏰ Same-day cancellations may be restricted

---

## User Profile

Access your profile by clicking **"👤 МОЙ КАБИНЕТ"** (My Profile).

![User Profile](screenshots/profile.png)

### Profile Sections

#### 1. User Information
- Display name
- User ID
- Registration date

#### 2. Statistics
- **Active Bookings**: Upcoming sessions
- **Total Bookings**: All-time bookings
- **Total Reviews**: Reviews submitted
- **Last Booking**: Most recent booking date

#### 3. Booking History
Complete list of all your bookings with:
- Date and time
- Ticket details
- Prices
- Status

**Actions:**
- **🔄 ОБНОВИТЬ** (Refresh) - Reload data
- **🗑️ УДАЛИТЬ** (Delete) - Cancel selected booking

#### 4. Reviews Section
- View all your reviews
- Add new reviews
- See approval status

---

## Reviews & Feedback

### Submitting a Review

![Add Review](screenshots/add-review.png)

**From Profile Page:**

1. Scroll to **"💬 МОИ ОТЗЫВЫ"** (My Reviews) section
2. Enter your review text (minimum 3 characters)
3. Select rating:
   - ⭐ (1 star) - Poor
   - ⭐⭐ (2 stars) - Fair
   - ⭐⭐⭐ (3 stars) - Good
   - ⭐⭐⭐⭐ (4 stars) - Very Good
   - ⭐⭐⭐⭐⭐ (5 stars) - Excellent
4. Click **"📝 ДОБАВИТЬ"** (Add)
5. Review is submitted for approval

**Review Guidelines:**
- Be honest and constructive
- Focus on your experience
- Minimum 3 characters required
- Reviews may be moderated
- All reviews are appreciated!

**Example Reviews:**
```
⭐⭐⭐⭐⭐ "Отличная арена! Чистый лед, приветливый персонал."
⭐⭐⭐⭐ "Хорошее место для катания, но раздевалки маловаты."
⭐⭐⭐⭐⭐ "Идеально для семейного отдыха!"
```

---

### Viewing Public Reviews

Public reviews appear on the main page or reviews section. You can:
- See ratings from other users
- Read detailed feedback
- Get insights before booking

---

## Guest Mode

### Using Guest Mode

Don't want to create an account? Use **Guest Mode** to:

✅ **What You Can Do:**
- View the full schedule
- See available time slots
- Check prices
- Browse without commitment

❌ **What You Cannot Do:**
- Make bookings
- View booking history
- Submit reviews
- Access profile features

**How to Enter Guest Mode:**
1. Click **"Продолжить как Гость"** on login screen
2. Browse schedule
3. To book, exit and create an account

---

## Admin Features

⚠️ **For Administrators Only**

Admin panel access: Username "admin", Password "admin"

![Admin Panel](screenshots/admin-panel.png)

### Admin Capabilities

#### 1. User Management (👥 УПРАВЛЕНИЕ ПОЛЬЗОВАТЕЛЯМИ)
- View all registered users
- Add/edit/delete user accounts
- Change user roles
- Monitor user activity

#### 2. Analytics (📊 АНАЛИТИКА АРЕНЫ)
- View daily revenue
- Track attendance numbers
- Monitor electricity costs
- Add performance notes
- Generate reports

#### 3. Bookings Management (🎫 ВСЕ БРОНИРОВАНИЯ)
- View all bookings across all users
- Filter by date, user, or status
- Cancel bookings if needed
- Export booking data

#### 4. Schedule Management (📅 РАСПИСАНИЕ)
- Add new time slots
- Edit existing sessions
- Set capacity limits
- Mark slots as unavailable
- Bulk schedule updates

#### 5. Support (🛠 ТЕХПОДДЕРЖКА)
- View support tickets
- Respond to user inquiries
- Track issue resolution
- Manage FAQ

---

## Troubleshooting

### Common Issues

#### Issue 1: Cannot Connect to Server

**Error Message:** "❌ Сервер не запущен!"

**Solutions:**
1. Verify server application is running
2. Check internet connection
3. Contact system administrator
4. Restart both client and server
5. Check firewall settings

---

#### Issue 2: Login Fails

**Error Messages:**
- "Неверный пароль" (Incorrect password)
- "Пользователь не найден" (User not found)

**Solutions:**
1. **Forgot Password:**
   - Contact support for password reset
   - Email: support@polessu.by

2. **New User:**
   - Click "Регистрация" to create account
   - Verify email is correct

3. **Account Locked:**
   - Contact administrator
   - Provide your email address

---

#### Issue 3: Booking Not Showing

**Problem:** Made a booking but don't see it

**Solutions:**
1. Click **"↻ ОБНОВИТЬ"** (Refresh) button
2. Log out and log back in
3. Check "МОЙ КАБИНЕТ" (Profile) page
4. Verify booking confirmation was received
5. Contact support with booking details

---

#### Issue 4: Cannot Cancel Booking

**Problem:** Cancel button is disabled or greyed out

**Reasons:**
- Booking time has already passed
- Cancellation period expired
- Booking already canceled
- System maintenance

**Solutions:**
- Check booking status (Active vs. Completed)
- Contact support for assistance
- Review cancellation policy

---

#### Issue 5: Schedule Not Loading

**Problem:** Blank or loading schedule

**Solutions:**
1. Click refresh button
2. Check internet connection
3. Clear application cache (restart app)
4. Verify server status
5. Wait a few moments and retry

---

## FAQ

### General Questions

**Q: Is registration free?**  
A: Yes, account registration is completely free.

**Q: How do I change my email?**  
A: Contact support at support@polessu.by with your request.

**Q: Can I book for someone else?**  
A: Yes, bookings are not tied to attendee names. You can book and transfer tickets.

**Q: What payment methods are accepted?**  
A: Currently accepting cash at venue. Online payment coming soon.

---

### Booking Questions

**Q: How far in advance can I book?**  
A: Schedule typically shows 7 days ahead.

**Q: Can I modify an existing booking?**  
A: No, you must cancel and create a new booking.

**Q: What if my session is canceled?**  
A: You'll be notified and refunded automatically.

**Q: Can I book multiple sessions?**  
A: Yes, no limit on number of bookings.

**Q: Are group discounts available?**  
A: Contact support for group booking information.

---

### Skate Rental Questions

**Q: Are skates included in ticket price?**  
A: No, skate rental is optional and selected during booking.

**Q: What sizes are available?**  
A: Sizes 30-46 in both figure and hockey styles.

**Q: Can I bring my own skates?**  
A: Yes! Skate rental is optional.

**Q: Do I need to return skates?**  
A: Yes, return to desk after your session.

**Q: What if skates don't fit?**  
A: Exchange at the desk before your session starts.

---

### Technical Questions

**Q: What browsers are supported?**  
A: This is a Windows application, not web-based.

**Q: Can I use on Mac or Linux?**  
A: Currently Windows only. Mobile app coming soon.

**Q: Is my data secure?**  
A: Yes, all passwords are encrypted and data is protected.

**Q: Can I access from multiple computers?**  
A: Yes, log in with your email and password.

**Q: Where is my data stored?**  
A: Securely on Ice Arena servers.

---

### Cancellation & Refunds

**Q: Can I get a refund?**  
A: Refund policy depends on cancellation timing. Contact support.

**Q: How do I cancel?**  
A: Click "Отменить" button next to your booking.

**Q: Is there a cancellation fee?**  
A: No fees for cancellations made 24+ hours in advance.

**Q: What if I don't show up?**  
A: No-shows are not refunded. Please cancel in advance.

---

## Support Contact

### Need Help?

**Email Support:**  
📧 support@polessu.by

**Phone Support:**  
📞 +375 (XX) XXX-XX-XX  
Hours: Monday-Friday, 9:00-18:00

**In-Person:**  
Visit the Ice Arena reception desk during operating hours.

**Response Time:**  
- Email: Within 24 hours
- Phone: Immediate during business hours
- Urgent issues: Call reception desk

---

## Tips for Best Experience

### Before Your Session

✅ **Preparation Checklist:**
- [ ] Confirm booking in profile
- [ ] Arrive 15 minutes early
- [ ] Wear warm, comfortable clothing
- [ ] Bring gloves or mittens
- [ ] Have ID ready (if required)
- [ ] Check skate rental confirmation

### During Your Session

✅ **Safety First:**
- Follow staff instructions
- Stay within marked areas
- Watch for other skaters
- Report any hazards
- Keep personal items secure

### After Your Session

✅ **Don't Forget:**
- Return rental skates
- Collect personal belongings
- Leave a review!
- Check for next booking
- Enjoy your day! ⛸️

---

## Quick Reference

### Button Guide

| Button | Russian | English | Action |
|--------|---------|---------|--------|
| 🚪 ВЫХОД | Выход | Exit | Log out |
| ↻ ОБНОВИТЬ | Обновить | Refresh | Reload data |
| ➕ ЗАБРОНИРОВАТЬ | Забронировать | Book | Make booking |
| 👤 МОЙ КАБИНЕТ | Мой кабинет | My Profile | Open profile |
| ✕ ОТМЕНИТЬ | Отменить | Cancel | Cancel booking |
| 📝 ДОБАВИТЬ | Добавить | Add | Submit review |
| 🗑️ УДАЛИТЬ | Удалить | Delete | Remove item |

### Status Guide

| Status | Meaning | Action |
|--------|---------|--------|
| 🟢 ДОСТУПНО | Available | Can book |
| 🔴 НЕТ МЕСТ | Full | Cannot book |
| 🔵 Вы записаны | Your booking | Can cancel |
| ○ ПРОШЕЛ | Completed | Past session |
| ✓ АКТИВНО | Active | Upcoming |

---

## Updates & New Features

### Recent Updates

**Version 1.0 (December 2025)**
- Initial release
- User registration and login
- Schedule viewing and booking
- Skate rental system
- Review submission
- Profile management
- Admin panel

### Upcoming Features

🚀 **Coming Soon:**
- Mobile app (iOS/Android)
- Online payment integration
- Email notifications
- Booking reminders
- Loyalty rewards program
- Group booking discounts
- Season passes

Stay tuned for updates!

---

## Terms of Service

### User Responsibilities

By using this system, you agree to:
- Provide accurate information
- Maintain account security
- Honor bookings or cancel in advance
- Follow arena rules and regulations
- Respect other users

### Privacy

Your privacy matters. We:
- Encrypt all passwords
- Protect personal information
- Never share data with third parties
- Comply with data protection regulations
- Allow account deletion upon request

For full terms: [Link to Terms]

---

## Glossary

**Booking** - Reserved time slot for ice rink session

**Guest Mode** - Limited access without account registration

**Schedule** - Calendar of available ice rink sessions

**Session** - Time period for ice skating (typically 45 minutes)

**Time Slot** - Specific start and end time for a session

**Ticket** - Entry pass for one person (Adult, Child, or Senior)

**Rental** - Ice skate borrowing service

**Profile** - Your personal account page with booking history

**Review** - User feedback and rating (1-5 stars)

---

## Keyboard Shortcuts

### Login Screen
- `Tab` - Navigate between fields
- `Enter` - Submit login
- `Esc` - Close dialog

### Main Screen
- `F5` - Refresh schedule
- `Ctrl+P` - Open profile
- `Ctrl+B` - New booking (if slot selected)
- `Ctrl+Q` - Logout

### Admin Panel
- `Ctrl+U` - Users tab
- `Ctrl+A` - Analytics tab
- `Ctrl+B` - Bookings tab
- `Ctrl+S` - Schedule tab

---

## Accessibility

### Features for All Users

- **Large Text** - Readable fonts (10-18pt)
- **High Contrast** - Clear color schemes
- **Keyboard Navigation** - Full keyboard support
- **Screen Reader Compatible** - ARIA labels
- **Resizable Windows** - Adjust to your preference

---

## System Status

### Check Service Status

Visit: [Status Page URL]

Or check server console for:
- ✅ "Сервер запущен" (Server running)
- ✅ "Подключение к БД успешно" (DB connected)

### Maintenance Schedule

Regular maintenance: Sunday 2:00-4:00 AM

---

## Conclusion

Thank you for using the Ice Arena Booking System! We hope this guide helps you make the most of our platform.

**Happy Skating! ⛸️**

---

**Document Version:** 1.0  
**Last Updated:** December 3, 2025  
**© 2025 Polessu Ice Arena**

*For the latest version of this guide, visit our support page or contact support@polessu.by*

---

*End of User Guide*
