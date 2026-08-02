using System;

namespace WorkPilot.Application.DTOs;

public record BusinessDto(
    Guid Id,
    string Name,
    string Description,
    string Location,
    string ContactEmail,
    string TimeZone,
    string CancellationPolicy,
    string CommunicationTone,
    bool IsCalendarConnected,
    string? GoogleCalendarId,
    DateTime CreatedAt
);

public record CreateBusinessDto(
    string Name,
    string Description,
    string Location,
    string ContactEmail,
    string TimeZone,
    string CancellationPolicy,
    string CommunicationTone
);

public record UpdateBusinessDto(
    string Name,
    string Description,
    string Location,
    string ContactEmail,
    string TimeZone,
    string CancellationPolicy,
    string CommunicationTone
);
