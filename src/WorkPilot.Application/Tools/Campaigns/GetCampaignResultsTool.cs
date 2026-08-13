using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkPilot.Application.Common.Interfaces;

namespace WorkPilot.Application.Tools.Campaigns;

public class GetCampaignResultsTool : IGetCampaignResultsTool
{
    private readonly IWorkPilotDbContext _dbContext;

    public GetCampaignResultsTool(IWorkPilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CampaignResultsOutput> ExecuteAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await _dbContext.Campaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken);

        if (campaign == null)
        {
            return new CampaignResultsOutput(0, 0, 0);
        }

        return new CampaignResultsOutput(
            EmailsSent: campaign.EmailsSent,
            BookingsGenerated: campaign.BookingsConfirmed,
            RevenueImpact: campaign.RevenueGenerated
        );
    }
}
