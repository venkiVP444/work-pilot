using System;

namespace WorkPilot.Application.DTOs;

public record CalendarSlotDto(
    DateTime StartTime,
    DateTime EndTime,
    string DisplayText
);

public record TimeIntervalDto(
    DateTime StartTime,
    DateTime EndTime
);
