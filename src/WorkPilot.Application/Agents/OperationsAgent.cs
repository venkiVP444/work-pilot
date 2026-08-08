using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkPilot.Application.DTOs;

namespace WorkPilot.Application.Agents;

public class OperationsAgent : IOperationsAgent
{
    public Task<OperationsOutput> GetProactiveBriefAsync(OperationsInput input, CancellationToken cancellationToken = default)
    {
        var snap = input.Snapshot;
        var alerts = new List<OpportunityCardDto>();

        // 1. Inactive customers alert
        if (snap.InactiveCustomers60Days > 0)
        {
            var estRev = snap.InactiveCustomers60Days * 85m * 0.35m;
            alerts.Add(new OpportunityCardDto(
                Title: $"{snap.InactiveCustomers60Days} Inactive Customers",
                Description: "Reactivate customers who haven't booked in the last 60 days.",
                EstimatedRevenue: $"${estRev:F0}",
                ActionLabel: "Reactivate Customers",
                ActionType: "CreateCampaign",
                AffectedCustomers: snap.InactiveCustomers60Days,
                Priority: "high",
                Icon: "👥"
            ));
        }

        // 2. Empty slots alert
        if (snap.EmptySlotsThisWeek > 2)
        {
            var estRev = snap.EmptySlotsThisWeek * 85m * 0.5m;
            alerts.Add(new OpportunityCardDto(
                Title: $"{snap.EmptySlotsThisWeek} Empty Slots This Week",
                Description: "Fill open hours in your schedule by targeting active leads.",
                EstimatedRevenue: $"${estRev:F0}",
                ActionLabel: "Fill Calendar Slots",
                ActionType: "FillEmptySlots",
                AffectedCustomers: snap.TotalCustomers - snap.InactiveCustomers60Days,
                Priority: "medium",
                Icon: "📅"
            ));
        }

        // 3. Overview performance report alert
        alerts.Add(new OpportunityCardDto(
            Title: "Monthly Health Check",
            Description: "Analyze your booking conversions, revenue distributions, and top services.",
            EstimatedRevenue: "N/A",
            ActionLabel: "Analyze Performance",
            ActionType: "AnalyzeBusiness",
            AffectedCustomers: 0,
            Priority: "low",
            Icon: "📊"
        ));

        var dailyOutlook = $"Your calendar has {snap.EmptySlotsThisWeek} open training blocks this week. Re-engaging your inactive clients is your highest impact action item today.";

        return Task.FromResult(new OperationsOutput(alerts, dailyOutlook));
    }
}
