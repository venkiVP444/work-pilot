using System;
using WorkPilot.Domain.Enums;

namespace WorkPilot.Domain.Entities;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingRequestId { get; set; }
    public string? GoogleCalendarEventId { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
    public DateTime ConfirmedAt { get; set; } = DateTime.UtcNow;
    public bool ConfirmationEmailSent { get; set; } = false;
    public string EmailDeliveryStatus { get; set; } = "NotAttempted"; // "Sent", "Failed", "Simulated", "NotAttempted"
    public string? EmailDeliveryError { get; set; }

    public BookingRequest? BookingRequest { get; set; }
}
