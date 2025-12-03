using System;
using System.Collections.Generic;
using System.Linq;

namespace IceArena.Client
{
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
        public decimal TotalCost => Tickets?.Sum(t => t.Price * t.Quantity) ?? 0;
        public int AdultTickets => Tickets?.FirstOrDefault(t => t.Type == "Adult")?.Quantity ?? 0;
        public int ChildTickets => Tickets?.FirstOrDefault(t => t.Type == "Child")?.Quantity ?? 0;
        public int SeniorTickets => Tickets?.FirstOrDefault(t => t.Type == "Senior")?.Quantity ?? 0;
        public int TotalTickets => Tickets?.Sum(t => t.Quantity) ?? 0;
    }

    public class Ticket
    {
        public int Id { get; set; }
        public int BookingId { get; set; }
        public string Type { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class Review
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int Rating { get; set; }
        public string Text { get; set; }
        public DateTime Date { get; set; }
        public bool IsApproved { get; set; }
    }
}