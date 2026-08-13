using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkPilot.Application.Common.Interfaces;

namespace WorkPilot.Application.Tools.Customers;

public class GetCustomerSegmentsTool : IGetCustomerSegmentsTool
{
    private readonly IWorkPilotDbContext _dbContext;

    public GetCustomerSegmentsTool(IWorkPilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<CustomerSegmentSummary>> ExecuteAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        var leads = await _dbContext.Leads
            .Where(l => l.BusinessId == businessId)
            .ToListAsync(cancellationToken);

        var segments = new List<CustomerSegmentSummary>();

        var activeLeads = leads.Where(l => l.Tags != null && l.Tags.Contains("active")).ToList();
        var premiumLeads = leads.Where(l => l.Tags != null && l.Tags.Contains("premium")).ToList();
        var inactive60 = leads.Where(l => l.Tags != null && l.Tags.Contains("inactive-60")).ToList();
        var inactive90 = leads.Where(l => l.Tags != null && l.Tags.Contains("inactive-90")).ToList();

        segments.Add(new CustomerSegmentSummary("Active Customers", activeLeads.Count, activeLeads.Sum(l => l.TotalSpend)));
        segments.Add(new CustomerSegmentSummary("Premium Customers", premiumLeads.Count, premiumLeads.Sum(l => l.TotalSpend)));
        segments.Add(new CustomerSegmentSummary("Inactive (60+ days)", inactive60.Count, inactive60.Sum(l => l.TotalSpend)));
        segments.Add(new CustomerSegmentSummary("Inactive (90+ days)", inactive90.Count, inactive90.Sum(l => l.TotalSpend)));

        return segments;
    }
}
