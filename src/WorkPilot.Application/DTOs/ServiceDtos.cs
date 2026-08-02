using System;

namespace WorkPilot.Application.DTOs;

public record ServiceDto(
    Guid Id,
    Guid BusinessId,
    string Name,
    string Description,
    decimal Price,
    int DurationMinutes,
    bool IsActive,
    DateTime CreatedAt
);

public record CreateServiceDto(
    string Name,
    string Description,
    decimal Price,
    int DurationMinutes
);

public record UpdateServiceDto(
    string Name,
    string Description,
    decimal Price,
    int DurationMinutes,
    bool IsActive
);
