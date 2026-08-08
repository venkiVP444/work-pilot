using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkPilot.Application.Common.Interfaces;
using WorkPilot.Domain.Enums;

namespace WorkPilot.Application.Tools.Bookings;

public class GetEmptySlotsTool : IGetEmptySlotsTool
{
    private readonly IWorkPilotDbContext _dbContext;

    public GetEmptySlotsTool(IWorkPilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> ExecuteAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var weekStart = now.Date;
        var weekEnd = weekStart.AddDays(7);

        var bookingsThisWeek = await _dbContext.BookingRequests
            .CountAsync(br => br.BusinessId == businessId &&
                              br.Status == BookingRequestStatus.Approved &&
                              br.RequestedStartTime >= weekStart &&
                              br.RequestedStartTime < weekEnd, cancellationToken);

        var availabilityRulesCount = await _dbContext.AvailabilityRules
            .CountAsync(r => r.BusinessId == businessId && r.IsActive, cancellationToken);

        // Estimate 2 slots per active rules day
        return Math.Max(0, availabilityRulesCount * 2 - bookingsThisWeek);
    }
}
