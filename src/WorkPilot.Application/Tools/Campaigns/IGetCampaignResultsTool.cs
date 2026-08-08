using System;
using System.Threading;
using System.Threading.Tasks;

namespace WorkPilot.Application.Tools.Campaigns;

public record CampaignResultsOutput(
    int EmailsSent,
    int BookingsGenerated,
    decimal RevenueImpact
);

public interface IGetCampaignResultsTool
{
    Task<CampaignResultsOutput> ExecuteAsync(Guid campaignId, CancellationToken cancellationToken = default);
}
