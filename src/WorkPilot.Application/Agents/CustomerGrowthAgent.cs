using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkPilot.Application.Common.Interfaces;
using WorkPilot.Application.Tools.Customers;
using WorkPilot.Domain.Entities;

namespace WorkPilot.Application.Agents;

public class CustomerGrowthAgent : ICustomerGrowthAgent
{
    private readonly IGetInactiveCustomersTool _inactiveCustomersTool;
    private readonly IWorkPilotDbContext _dbContext;

    public CustomerGrowthAgent(
        IGetInactiveCustomersTool inactiveCustomersTool,
        IWorkPilotDbContext dbContext)
    {
        _inactiveCustomersTool = inactiveCustomersTool;
        _dbContext = dbContext;
    }

    public async Task<CustomerGrowthOutput> IdentifyReactivationCandidatesAsync(CustomerGrowthInput input, CancellationToken cancellationToken = default)
    {
        var businessId = input.BusinessId;
        var segment = input.TargetSegment;

        List<Lead> leads;

        if (segment.Contains("60"))
        {
            leads = await _inactiveCustomersTool.ExecuteAsync(businessId, 60, cancellationToken);
        }
        else if (segment.Contains("90"))
        {
            leads = await _inactiveCustomersTool.ExecuteAsync(businessId, 90, cancellationToken);
        }
        else if (segment.Contains("30"))
        {
            leads = await _inactiveCustomersTool.ExecuteAsync(businessId, 30, cancellationToken);
        }
        else
        {
            // Default: any inactive customer or active customers if capacity promotion
            leads = await _inactiveCustomersTool.ExecuteAsync(businessId, 30, cancellationToken);
        }

        // Include leads that have tags matching the segment name
        var tagMatches = await _dbContext.Leads
            .Where(l => l.BusinessId == businessId && l.Tags != null && l.Tags.Contains(segment.ToLower()))
            .ToListAsync(cancellationToken);

        var combined = leads.UnionBy(tagMatches, l => l.Id).ToList();

        var desc = $"Targeting customers in the '{segment}' segment to drive retention and fill scheduling capacity.";

        return new CustomerGrowthOutput(desc, combined, combined.Count);
    }
}
