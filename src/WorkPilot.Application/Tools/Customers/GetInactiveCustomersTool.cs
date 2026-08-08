using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkPilot.Application.Common.Interfaces;
using WorkPilot.Domain.Entities;

namespace WorkPilot.Application.Tools.Customers;

public class GetInactiveCustomersTool : IGetInactiveCustomersTool
{
    private readonly IWorkPilotDbContext _dbContext;

    public GetInactiveCustomersTool(IWorkPilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Lead>> ExecuteAsync(Guid businessId, int thresholdDays, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-thresholdDays);
        return await _dbContext.Leads
            .Where(l => l.BusinessId == businessId &&
                        l.LastVisitDate.HasValue &&
                        l.LastVisitDate <= cutoff)
            .OrderByDescending(l => l.LastVisitDate)
            .ToListAsync(cancellationToken);
    }
}
