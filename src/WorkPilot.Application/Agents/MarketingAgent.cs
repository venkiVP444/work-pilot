using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkPilot.Application.Common.Interfaces;
using WorkPilot.Application.DTOs;
using WorkPilot.Application.Tools.Campaigns;
using WorkPilot.Application.Tools.Communications;
using WorkPilot.Domain.Entities;
using WorkPilot.Domain.Enums;

namespace WorkPilot.Application.Agents;

public class MarketingAgent : IMarketingAgent
{
    private readonly ICreateCampaignTool _createCampaignTool;
    private readonly ISendCampaignEmailTool _sendEmailTool;
    private readonly IGeminiAgentService _geminiService;
    private readonly IWorkPilotDbContext _dbContext;

    public MarketingAgent(
        ICreateCampaignTool createCampaignTool,
        ISendCampaignEmailTool sendEmailTool,
        IGeminiAgentService geminiService,
        IWorkPilotDbContext dbContext)
    {
        _createCampaignTool = createCampaignTool;
        _sendEmailTool = sendEmailTool;
        _geminiService = geminiService;
        _dbContext = dbContext;
    }

    public async Task<MarketingOutput> PlanCampaignAsync(MarketingPlanInput input, CancellationToken cancellationToken = default)
    {
        // 1. Reason campaign subject/body with Gemini (or fallback if unconfigured)
        var intentRequest = new OwnerIntentRequest(input.BusinessId, input.RawGoal, input.Snapshot);
        var response = await _geminiService.ProcessOwnerIntentAsync(intentRequest, cancellationToken);

        decimal cost = 0.00m; // AI marketing campaigns are free/simulated
        int expectedBookings = response.EstimatedBookings > 0 ? response.EstimatedBookings : (int)Math.Round(input.CustomerCount * 0.35);
        decimal expectedRevenue = response.EstimatedRevenue > 0 ? response.EstimatedRevenue : expectedBookings * 85m;

        return new MarketingOutput(
            SubjectLine: response.CampaignSubjectLine ?? $"We miss you! Special offer from FitPro",
            EmailBody: response.CampaignEmailBody ?? "Dear valued client, it's been a while! Come back for a tailored workout.",
            EstimatedCost: cost,
            ExpectedRevenue: expectedRevenue,
            ExpectedBookings: expectedBookings
        );
    }

    public async Task<CampaignExecutionOutput> ExecuteCampaignAsync(CampaignExecutionInput input, CancellationToken cancellationToken = default)
    {
        // 1. Create campaign record (draft)
        var createInput = new CreateCampaignInput(
            BusinessId: input.BusinessId,
            AIAgentActionId: input.AIAgentActionId,
            Name: input.CampaignName,
            TargetSegment: input.TargetSegment,
            SubjectLine: input.SubjectLine,
            EmailBody: input.EmailBody,
            TargetCustomerCount: input.TargetCustomers.Count,
            Budget: 0.00m
        );

        var campaign = await _createCampaignTool.ExecuteAsync(createInput, cancellationToken);

        // 2. Dispatch emails to all targeted leads
        int sent = 0;
        int failed = 0;

        foreach (var customer in input.TargetCustomers)
        {
            var personalBody = input.EmailBody.Replace("{{CustomerName}}", customer.Name);
            var sendInput = new SendEmailInput(
                RecipientEmail: customer.Email,
                RecipientName: customer.Name,
                BusinessName: "FitPro Personal Training",
                SubjectLine: input.SubjectLine,
                HtmlBody: personalBody
            );

            bool success = await _sendEmailTool.ExecuteAsync(sendInput, cancellationToken);

            if (success) sent++;
            else failed++;
        }

        // 3. Update campaign state to executed
        campaign.Status = CampaignStatus.Sent;
        campaign.EmailsSent = sent;
        campaign.EmailsFailed = failed;
        campaign.BookingRequestsGenerated = (int)Math.Round(sent * 0.35);
        campaign.BookingsConfirmed = campaign.BookingRequestsGenerated;
        campaign.RevenueGenerated = campaign.BookingsConfirmed * 85.00m;
        campaign.SentAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CampaignExecutionOutput(campaign.Id, failed == 0, sent, failed);
    }
}
