using System;
using WorkPilot.Domain.Enums;

namespace WorkPilot.Application.DTOs;

public record BookingRequestDto(
    Guid Id,
    Guid BusinessId,
    Guid LeadId,
    string LeadName,
    string LeadEmail,
    string? LeadPhone,
    Guid ServiceId,
    string ServiceName,
    decimal ServicePrice,
    int ServiceDurationMinutes,
    DateTime RequestedStartTime,
    DateTime RequestedEndTime,
    string ProposedSlotSummary,
    BookingRequestStatus Status,
    string? OwnerNotes,
    DateTime CreatedAt,
    string? GoogleCalendarEventId,
    string EmailDeliveryStatus = "NotAttempted",
    string? EmailDeliveryError = null
);

public record ApproveBookingRequestDto(
    string? Notes
);

public record RejectBookingRequestDto(
    string Reason
);
