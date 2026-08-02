using System;

namespace WorkPilot.Application.DTOs;

public record AvailabilityRuleDto(
    Guid Id,
    Guid BusinessId,
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int BufferMinutes,
    bool IsActive
);

public record CreateAvailabilityRuleDto(
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int BufferMinutes
);

public record UpdateAvailabilityRuleDto(
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    int BufferMinutes,
    bool IsActive
);
