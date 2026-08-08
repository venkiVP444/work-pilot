using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkPilot.Application.DTOs;

namespace WorkPilot.Application.Agents;

public record DecomposeGoalInput(
    Guid BusinessId,
    string RawGoal,
    BusinessSnapshotDto Snapshot
);

public record BusinessObjective(
    string ObjectiveType, // "ReactivateCustomers" or "FillEmptySlots" or "AnalyzeBusiness"
    string Description,
    string TargetSegment,
    decimal ImpactEstimate
);

public record DecomposedGoalOutput(
    List<BusinessObjective> Objectives,
    string ReasoningSummary
);

public interface IBusinessGoalAgent
{
    Task<DecomposedGoalOutput> DecomposeGoalAsync(DecomposeGoalInput input, CancellationToken cancellationToken = default);
}
