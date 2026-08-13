using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkPilot.Application.DTOs;

namespace WorkPilot.Application.Agents;

public record RevenueOptimizationInput(
    Guid BusinessId,
    BusinessSnapshotDto Snapshot
);

public record RevenueOpportunity(
    string StrategyName,
    string Description,
    decimal PotentialRevenueIncrease,
    string Priority
);

public record RevenueOptimizationOutput(
    List<RevenueOpportunity> Opportunities,
    string OptimizationsSummary
);

public interface IRevenueOptimizationAgent
{
    Task<RevenueOptimizationOutput> IdentifyOpportunitiesAsync(RevenueOptimizationInput input, CancellationToken cancellationToken = default);
}
