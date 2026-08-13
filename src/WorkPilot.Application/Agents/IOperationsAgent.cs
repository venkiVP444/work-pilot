using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkPilot.Application.DTOs;

namespace WorkPilot.Application.Agents;

public record OperationsInput(
    Guid BusinessId,
    BusinessSnapshotDto Snapshot
);

public record OperationsOutput(
    List<OpportunityCardDto> MorningBriefAlerts,
    string DailyOutlook
);

public interface IOperationsAgent
{
    Task<OperationsOutput> GetProactiveBriefAsync(OperationsInput input, CancellationToken cancellationToken = default);
}
