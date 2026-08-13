using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkPilot.Application.DTOs;
using WorkPilot.Application.Tools.Analytics;

namespace WorkPilot.Application.Agents;

public class BusinessAnalystAgent : IBusinessAnalystAgent
{
    private readonly IGetBusinessSnapshotTool _snapshotTool;

    public BusinessAnalystAgent(IGetBusinessSnapshotTool snapshotTool)
    {
        _snapshotTool = snapshotTool;
    }

    public async Task<AnalysisOutput> AnalyzeSnapshotAsync(AnalysisInput input, CancellationToken cancellationToken = default)
    {
        var snap = input.Snapshot;
        var insights = new List<InsightItem>();

        // 1. Check inactive 60+ days segment
        if (snap.InactiveCustomers60Days > 0)
        {
            insights.Add(new InsightItem(
                Category: "Customer Retention",
                Observation: $"{snap.InactiveCustomers60Days} customers haven't visited in 60+ days.",
                ImpactLevel: "High",
                RecommendedAction: "Reactivate customers via campaign"
            ));
        }

        // 2. Check empty slots
        if (snap.EmptySlotsThisWeek > 3)
        {
            insights.Add(new InsightItem(
                Category: "Capacity Optimization",
                Observation: $"{snap.EmptySlotsThisWeek} open training slots identified for the upcoming week.",
                ImpactLevel: "Medium",
                RecommendedAction: "Execute slot-fill email marketing campaign"
            ));
        }

        // 3. Evaluate revenue trends
        if (snap.RevenueThisMonth < snap.RevenueLastMonth)
        {
            insights.Add(new InsightItem(
                Category: "Financial Performance",
                Observation: $"Revenue this month (${snap.RevenueThisMonth}) is down compared to last month (${snap.RevenueLastMonth}).",
                ImpactLevel: "High",
                RecommendedAction: "Launch reactivation and packaging promotions"
            ));
        }

        var strategicSummary = $"Your business shows strong potential with {snap.TotalCustomers} total leads. " +
                               $"Re-engaging the {snap.InactiveCustomers60Days} inactive customers represents your highest financial return lever.";

        return new AnalysisOutput(insights, strategicSummary);
    }
}
