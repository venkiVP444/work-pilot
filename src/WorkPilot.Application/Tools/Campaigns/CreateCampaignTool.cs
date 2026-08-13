using System;
using System.Threading;
using System.Threading.Tasks;
using WorkPilot.Application.Common.Interfaces;
using WorkPilot.Domain.Entities;
using WorkPilot.Domain.Enums;

namespace WorkPilot.Application.Tools.Campaigns;

public class CreateCampaignTool : ICreateCampaignTool
{
    private readonly IWorkPilotDbContext _dbContext;

    public CreateCampaignTool(IWorkPilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Campaign> ExecuteAsync(CreateCampaignInput input, CancellationToken cancellationToken = default)
    {
        var campaign = new Campaign
        {
            BusinessId = input.BusinessId,
            AIAgentActionId = input.AIAgentActionId,
            Name = input.Name,
            TargetSegment = input.TargetSegment,
            SubjectLine = input.SubjectLine,
            EmailBody = input.EmailBody,
            TargetCustomerCount = input.TargetCustomerCount,
            CampaignCost = input.Budget,
            Status = CampaignStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Campaigns.Add(campaign);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return campaign;
    }
}
