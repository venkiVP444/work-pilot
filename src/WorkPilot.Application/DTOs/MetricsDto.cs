using System;

namespace WorkPilot.Application.DTOs;

public record DashboardMetricsDto(
    int TotalLeads,
    int QualifiedLeads,
    int PendingBookingRequests,
    int ConfirmedBookings,
    double ConversionRatePercentage,
    int TotalAIInteractions
);
