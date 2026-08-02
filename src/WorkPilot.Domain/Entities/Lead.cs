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

    public Business? Business { get; set; }
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<BookingRequest> BookingRequests { get; set; } = new List<BookingRequest>();
}
