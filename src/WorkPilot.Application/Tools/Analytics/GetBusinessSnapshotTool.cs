using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkPilot.Application.Common.Interfaces;
using WorkPilot.Application.DTOs;
using WorkPilot.Domain.Enums;

namespace WorkPilot.Application.Tools.Analytics;

public class GetBusinessSnapshotTool : IGetBusinessSnapshotTool
{
    private readonly IWorkPilotDbContext _dbContext;

    public GetBusinessSnapshotTool(IWorkPilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BusinessSnapshotDto> ExecuteAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        var business = await _dbContext.Businesses
            .Include(b => b.Services)
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken)
            ?? throw new ArgumentException($"Business {businessId} not found.");

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfLastMonth = startOfMonth.AddMonths(-1);
        var endOfLastMonth = startOfMonth.AddSeconds(-1);
        var thirtyDaysAgo = now.AddDays(-30);
        var sixtyDaysAgo = now.AddDays(-60);
        var ninetyDaysAgo = now.AddDays(-90);

        var allLeads = await _dbContext.Leads
            .Where(l => l.BusinessId == businessId)
            .ToListAsync(cancellationToken);

        var totalCustomers = allLeads.Count;
        var activeCustomers = allLeads.Count(l => l.LastVisitDate.HasValue && l.LastVisitDate >= thirtyDaysAgo);
        var inactive30 = allLeads.Count(l => l.LastVisitDate.HasValue && l.LastVisitDate < thirtyDaysAgo && l.LastVisitDate >= sixtyDaysAgo);
        var inactive60 = allLeads.Count(l => l.LastVisitDate.HasValue && l.LastVisitDate < sixtyDaysAgo && l.LastVisitDate >= ninetyDaysAgo);
        var inactive90 = allLeads.Count(l => l.LastVisitDate.HasValue && l.LastVisitDate < ninetyDaysAgo);

        var inactiveNoDate = allLeads.Count(l => !l.LastVisitDate.HasValue && l.Status != "Converted" &&
                                                  (now - l.CreatedAt).TotalDays > 60);
        inactive60 += inactiveNoDate;

        var allBookings = await _dbContext.Bookings
            .Include(b => b.BookingRequest)
                .ThenInclude(br => br!.Service)
            .Where(b => b.BookingRequest!.BusinessId == businessId && b.Status == BookingStatus.Confirmed)
            .ToListAsync(cancellationToken);

        var bookingsThisMonth = allBookings.Where(b => b.ConfirmedAt >= startOfMonth).ToList();
        var bookingsLastMonth = allBookings.Where(b => b.ConfirmedAt >= startOfLastMonth && b.ConfirmedAt <= endOfLastMonth).ToList();

        var revenueThisMonth = bookingsThisMonth.Sum(b => b.BookingRequest?.Service?.Price ?? 0);
        var revenueLastMonth = bookingsLastMonth.Sum(b => b.BookingRequest?.Service?.Price ?? 0);
        var totalRevenue = allBookings.Sum(b => b.BookingRequest?.Service?.Price ?? 0);

        var avgOrderValue = allBookings.Count > 0
            ? totalRevenue / allBookings.Count
            : business.Services.Any() ? business.Services.Average(s => s.Price) : 85m;

        var pendingRequests = await _dbContext.BookingRequests
            .CountAsync(br => br.BusinessId == businessId && br.Status == BookingRequestStatus.PendingApproval, cancellationToken);

        var weekStart = now.Date;
        var weekEnd = weekStart.AddDays(7);
        var bookingsThisWeek = await _dbContext.BookingRequests
            .CountAsync(br => br.BusinessId == businessId &&
                              br.Status == BookingRequestStatus.Approved &&
                              br.RequestedStartTime >= weekStart &&
                              br.RequestedStartTime < weekEnd, cancellationToken);
        var availabilityRulesCount = await _dbContext.AvailabilityRules
            .CountAsync(r => r.BusinessId == businessId && r.IsActive, cancellationToken);
        var emptySlots = Math.Max(0, availabilityRulesCount * 2 - bookingsThisWeek);

        var topServices = business.Services
            .OrderByDescending(s => s.Price)
            .Take(3)
            .Select(s => $"{s.Name} (${s.Price}/session)")
            .ToList();

        return new BusinessSnapshotDto(
            BusinessName: business.Name,
            TotalCustomers: totalCustomers,
            ActiveCustomers: activeCustomers,
            InactiveCustomers30Days: inactive30,
            InactiveCustomers60Days: inactive60,
            InactiveCustomers90Plus: inactive90,
            RevenueThisMonth: revenueThisMonth,
            RevenueLastMonth: revenueLastMonth,
            BookingsThisMonth: bookingsThisMonth.Count,
            BookingsLastMonth: bookingsLastMonth.Count,
            PendingBookingRequests: pendingRequests,
            EmptySlotsThisWeek: emptySlots,
            AverageOrderValue: avgOrderValue,
            TotalConfirmedBookings: allBookings.Count,
            TotalRevenue: totalRevenue,
            TopServices: topServices
        );
    }
}
