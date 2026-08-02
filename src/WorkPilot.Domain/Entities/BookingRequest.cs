using System;
using WorkPilot.Domain.Enums;

namespace WorkPilot.Domain.Entities;

public class BookingRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public Guid LeadId { get; set; }
    public Guid ServiceId { get; set; }
    public DateTime RequestedStartTime { get; set; }
    public DateTime RequestedEndTime { get; set; }
    public string ProposedSlotSummary { get; set; } = string.Empty;
    public BookingRequestStatus Status { get; set; } = BookingRequestStatus.PendingApproval;
    public string? OwnerNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Lead? Lead { get; set; }
    public Service? Service { get; set; }
    public Booking? Booking { get; set; }
}
