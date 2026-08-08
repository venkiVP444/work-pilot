using System;
using System.Threading;
using System.Threading.Tasks;
using WorkPilot.Domain.Entities;

namespace WorkPilot.Application.Tools.Campaigns;

public record CreateCampaignInput(
    Guid BusinessId,
    Guid? AIAgentActionId,
    string Name,
    string TargetSegment,
    string SubjectLine,
    string EmailBody,
    int TargetCustomerCount,
    decimal Budget
);

public interface ICreateCampaignTool
{
    Task<Campaign> ExecuteAsync(CreateCampaignInput input, CancellationToken cancellationToken = default);
}
