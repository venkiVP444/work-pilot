using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkPilot.Application.DTOs;
using WorkPilot.Application.Tools.Bookings;

namespace WorkPilot.Application.Agents;

public class RevenueOptimizationAgent : IRevenueOptimizationAgent
{
    private readonly IGetEmptySlotsTool _emptySlotsTool;

    public RevenueOptimizationAgent(IGetEmptySlotsTool emptySlotsTool)
    {
        _emptySlotsTool = emptySlotsTool;
    }

    public async Task<RevenueOptimizationOutput> IdentifyOpportunitiesAsync(RevenueOptimizationInput input, CancellationToken cancellationToken = default)
    {
        var snap = input.Snapshot;
        var opportunities = new List<RevenueOpportunity>();

        // 1. Evaluate empty slots
        var emptySlots = await _emptySlotsTool.ExecuteAsync(input.BusinessId, cancellationToken);
        if (emptySlots > 3)
        {
            decimal potential = emptySlots * snap.AverageOrderValue * 0.40m;
            opportunities.Add(new RevenueOpportunity(
                StrategyName: "Dynamic Off-Peak Promotion",
                Description: $"Fill {emptySlots} empty calendar slots this week using targeted email invitations with a 15% discount for off-peak hours.",
                PotentialRevenueIncrease: potential,
                Priority: "High"
            ));
        }

        // 2. Evaluate packaging/AOV opportunity
        if (snap.AverageOrderValue < 100m)
        {
            opportunities.Add(new RevenueOpportunity(
                StrategyName: "Value Session Bundles",
                Description: "Promote 5-session training bundles to active customers. Increases upfront cashflow and average order value from $85 to $400.",
                PotentialRevenueIncrease: 1200.00m,
                Priority: "Medium"
            ));
        }

        // 3. Premium tier suggestion
        if (snap.TotalCustomers > 40)
        {
            opportunities.Add(new RevenueOpportunity(
                StrategyName: "VIP VIP Coaching Tier",
                Description: "Introduce a high-value Nutrition + Goal coaching tier at $150/month. Estimated 10% adoption rate among premium segments.",
                PotentialRevenueIncrease: 750.00m,
                Priority: "Medium"
            ));
        }

        var summary = $"Identified {opportunities.Count} yield opportunities to expand average order value and utilize dormant calendar slots.";

        return new RevenueOptimizationOutput(opportunities, summary);
    }
}
