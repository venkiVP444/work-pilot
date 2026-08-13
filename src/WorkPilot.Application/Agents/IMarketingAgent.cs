using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkPilot.Application.DTOs;
using WorkPilot.Domain.Entities;

namespace WorkPilot.Application.Agents;

public record MarketingPlanInput(
    Guid BusinessId,
    string TargetSegment,
    int CustomerCount,
    string RawGoal,
    BusinessSnapshotDto Snapshot
);

public record MarketingOutput(
    string SubjectLine,
    string EmailBody,
    decimal EstimatedCost,
    decimal ExpectedRevenue,
    int ExpectedBookings
);

public record CampaignExecutionInput(
    Guid BusinessId,
    Guid AIAgentActionId,
    string CampaignName,
    string TargetSegment,
    string SubjectLine,
    string EmailBody,
    List<Lead> TargetCustomers
);

public record CampaignExecutionOutput(
    Guid CampaignId,
    bool Success,
    int EmailsSent,
    int EmailsFailed
);

public interface IMarketingAgent
{
    Task<MarketingOutput> PlanCampaignAsync(MarketingPlanInput input, CancellationToken cancellationToken = default);
    Task<CampaignExecutionOutput> ExecuteCampaignAsync(CampaignExecutionInput input, CancellationToken cancellationToken = default);
}
