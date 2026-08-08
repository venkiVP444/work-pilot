using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkPilot.Application.DTOs;

namespace WorkPilot.Application.Agents;

public record AnalysisInput(
    Guid BusinessId,
    BusinessSnapshotDto Snapshot
);

public record InsightItem(
    string Category,
    string Observation,
    string ImpactLevel, // "High", "Medium", "Low"
    string RecommendedAction
);

public record AnalysisOutput(
    List<InsightItem> Insights,
    string StrategicSummary
);

public interface IBusinessAnalystAgent
{
    Task<AnalysisOutput> AnalyzeSnapshotAsync(AnalysisInput input, CancellationToken cancellationToken = default);
}
