using System;
using System.Collections.Generic;

namespace WorkPilot.Domain.Entities;

public class Lead
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Status { get; set; } = "New"; // New, Qualified, Converted
    public string Source { get; set; } = "WebChat";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Customer retention analytics fields
    public DateTime? LastVisitDate { get; set; }
    public int TotalBookings { get; set; } = 0;
    public decimal TotalSpend { get; set; } = 0;
    public string? Tags { get; set; } // Comma-separated: "premium,inactive,vip"
    public bool IsActive { get; set; } = true;

    // Computed helper (not stored — calculated at runtime)
    public int DaysSinceLastVisit =>
        LastVisitDate.HasValue
            ? (int)(DateTime.UtcNow - LastVisitDate.Value).TotalDays
            : (int)(DateTime.UtcNow - CreatedAt).TotalDays;

    public Business? Business { get; set; }
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<BookingRequest> BookingRequests { get; set; } = new List<BookingRequest>();
}

