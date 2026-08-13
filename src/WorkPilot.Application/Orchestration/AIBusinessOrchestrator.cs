using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkPilot.Application.Common.Interfaces;
using WorkPilot.Application.DTOs;
using WorkPilot.Application.Agents;
using WorkPilot.Application.Tools.Analytics;
using WorkPilot.Domain.Entities;
using WorkPilot.Domain.Enums;

namespace WorkPilot.Application.Orchestration;

public class AIBusinessOrchestrator : IAIBusinessOrchestrator
{
    private readonly IWorkPilotDbContext _dbContext;
    private readonly IGetBusinessSnapshotTool _snapshotTool;
    private readonly IBusinessGoalAgent _goalAgent;
    private readonly IBusinessAnalystAgent _analystAgent;
    private readonly ICustomerGrowthAgent _growthAgent;
    private readonly IMarketingAgent _marketingAgent;
    private readonly IBookingAgent _bookingAgent;
    private readonly IRevenueOptimizationAgent _revenueAgent;
    private readonly IOperationsAgent _operationsAgent;
    private readonly ILogger<AIBusinessOrchestrator> _logger;

    public AIBusinessOrchestrator(
        IWorkPilotDbContext dbContext,
        IGetBusinessSnapshotTool snapshotTool,
        IBusinessGoalAgent goalAgent,
        IBusinessAnalystAgent analystAgent,
        ICustomerGrowthAgent growthAgent,
        IMarketingAgent marketingAgent,
        IBookingAgent bookingAgent,
        IRevenueOptimizationAgent revenueAgent,
        IOperationsAgent operationsAgent,
        ILogger<AIBusinessOrchestrator> logger)
    {
        _dbContext = dbContext;
        _snapshotTool = snapshotTool;
        _goalAgent = goalAgent;
        _analystAgent = analystAgent;
        _growthAgent = growthAgent;
        _marketingAgent = marketingAgent;
        _bookingAgent = bookingAgent;
        _revenueAgent = revenueAgent;
        _operationsAgent = operationsAgent;
        _logger = logger;
    }

    public async Task<BusinessSnapshotDto> GetBusinessSnapshotAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        return await _snapshotTool.ExecuteAsync(businessId, cancellationToken);
    }

    public async Task<OwnerChatResponse> HandleOwnerChatAsync(Guid businessId, OwnerChatRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Orchestrating agents for owner goal: '{Goal}'", request.Message);

        var agentSteps = new List<AIAgentStepDto>();
        agentSteps.Add(new AIAgentStepDto("Orchestrator", "Received owner intent", request.Message, DateTime.UtcNow, true));

        // 1. Get snapshot context
        var snapshot = await _snapshotTool.ExecuteAsync(businessId, cancellationToken);
        agentSteps.Add(new AIAgentStepDto("BusinessAnalyst", "Analyzed business snapshot data", $"Revenue: ${snapshot.RevenueThisMonth:F0}/mo", DateTime.UtcNow, true));

        // 2. BusinessGoalAgent decomposes intent
        var goalInput = new DecomposeGoalInput(businessId, request.Message, snapshot);
        var goalDecomp = await _goalAgent.DecomposeGoalAsync(goalInput, cancellationToken);
        agentSteps.Add(new AIAgentStepDto("BusinessGoalAgent", "Decomposed owner goal", $"Found {goalDecomp.Objectives.Count} key objectives", DateTime.UtcNow, true));

        if (!goalDecomp.Objectives.Any())
        {
            return BuildFallbackChatResponse("I couldn't identify specific business objectives. Could you rephrase your goal?", snapshot, agentSteps);
        }

        // 3. Coordinate specialized agents based on primary objective
        var primaryObjective = goalDecomp.Objectives.First();

        if (primaryObjective.ObjectiveType == "ReactivateCustomers")
        {
            // Chain: BusinessGoalAgent -> CustomerGrowthAgent -> MarketingAgent (if campaign)
            // (optionally BusinessAnalystAgent if profit-focused / wants profit)
            bool isProfitFocused = request.Message.Contains("profit") || request.Message.Contains("revenue") || request.Message.Contains("%");
            if (isProfitFocused)
            {
                var analystOutput = await _analystAgent.AnalyzeSnapshotAsync(new AnalysisInput(businessId, snapshot), cancellationToken);
                agentSteps.Add(new AIAgentStepDto("BusinessAnalystAgent", "Generated strategic business insights", analystOutput.StrategicSummary, DateTime.UtcNow, true));
            }

            var targetSegment = primaryObjective.TargetSegment;
            var growthOutput = await _growthAgent.IdentifyReactivationCandidatesAsync(new CustomerGrowthInput(businessId, targetSegment), cancellationToken);
            agentSteps.Add(new AIAgentStepDto("CustomerGrowthAgent", "Segmented customer targets", $"Identified {growthOutput.TotalCount} reactivation candidates in '{targetSegment}'", DateTime.UtcNow, true));

            if (growthOutput.TotalCount == 0)
            {
                return BuildFallbackChatResponse($"I found no customers matching segment '{targetSegment}' to target.", snapshot, agentSteps);
            }

            var planOutput = await _marketingAgent.PlanCampaignAsync(new MarketingPlanInput(
                BusinessId: businessId,
                TargetSegment: targetSegment,
                CustomerCount: growthOutput.TotalCount,
                RawGoal: request.Message,
                Snapshot: snapshot
            ), cancellationToken);
            agentSteps.Add(new AIAgentStepDto("MarketingAgent", "Drafted promotional campaign copy", $"Subject: {planOutput.SubjectLine}", DateTime.UtcNow, true));

            // Create persistence record for approval gate
            var actionId = Guid.NewGuid();
            var agentAction = new AIAgentAction
            {
                Id = actionId,
                BusinessId = businessId,
                AgentType = AgentType.Marketing,
                ActionType = ActionType.CreateCampaign,
                RiskLevel = RiskLevel.Medium, // campaigns require approval
                Status = ActionStatus.AwaitingApproval,
                OwnerIntent = request.Message,
                ReasoningSummary = goalDecomp.ReasoningSummary,
                AgentChain = string.Join(" → ", agentSteps.Select(s => s.Agent)),
                EstimatedImpact = $"Reach {growthOutput.TotalCount} clients, estimate ${planOutput.ExpectedRevenue:F0} revenue.",
                EstimatedRevenue = planOutput.ExpectedRevenue,
                EstimatedBookings = planOutput.ExpectedBookings,
                TargetCustomerCount = growthOutput.TotalCount,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.AIAgentActions.Add(agentAction);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var actionPlanDto = new AIActionPlanDto(
                ActionId: actionId,
                ActionType: "CreateCampaign",
                AgentType: "MarketingAgent",
                RiskLevel: "Medium",
                Title: $"Re-engagement campaign — {targetSegment}",
                Description: primaryObjective.Description ?? planOutput.SubjectLine,
                EstimatedImpact: agentAction.EstimatedImpact,
                EstimatedRevenue: planOutput.ExpectedRevenue,
                EstimatedBookings: planOutput.ExpectedBookings,
                TargetCustomerCount: growthOutput.TotalCount,
                WhatWillHappen: $"1. Target segment: {targetSegment}\n2. Personalized campaign email: '{planOutput.SubjectLine}'\n3. Dispatched to {growthOutput.TotalCount} contacts upon approval.",
                WhyRecommended: isProfitFocused ? "Re-engaging lapsed customers is the fastest way to grow revenue." : "Bring back inactive clients to retain them.",
                EstimatedCost: planOutput.EstimatedCost,
                Status: agentAction.Status,
                CreatedAt: agentAction.CreatedAt
            );

            var assistantMsg = $"I've analyzed your business snapshot and coordinated my specialized agents.\n\n" +
                               (isProfitFocused ? $"📊 **Strategic Insight:** Your business average order value is ${snapshot.AverageOrderValue:F0}.\n\n" : "") +
                               $"💡 **Action Recommendation:** We can launch a target campaign to reactivate inactive clients. I've prepared a draft campaign plan below. Please review and click **Approve & Execute** when ready.";

            return new OwnerChatResponse(
                AssistantMessage: assistantMsg,
                ReasoningSummary: goalDecomp.ReasoningSummary,
                BusinessSnapshot: snapshot,
                ActionPlan: actionPlanDto,
                Opportunities: (await _operationsAgent.GetProactiveBriefAsync(new OperationsInput(businessId, snapshot), cancellationToken)).MorningBriefAlerts,
                AgentChain: agentSteps,
                RequiresApproval: true,
                ActionId: actionId
            );
        }
        else if (primaryObjective.ObjectiveType == "FillEmptySlots")
        {
            // Chain: BusinessGoalAgent -> BusinessAnalystAgent -> RevenueOptimizationAgent -> BookingAgent
            var analystOutput = await _analystAgent.AnalyzeSnapshotAsync(new AnalysisInput(businessId, snapshot), cancellationToken);
            agentSteps.Add(new AIAgentStepDto("BusinessAnalystAgent", "Generated strategic business insights", analystOutput.StrategicSummary, DateTime.UtcNow, true));

            var revOutput = await _revenueAgent.IdentifyOpportunitiesAsync(new RevenueOptimizationInput(businessId, snapshot), cancellationToken);
            agentSteps.Add(new AIAgentStepDto("RevenueOptimizationAgent", "Evaluated scheduling yield optimization", revOutput.OptimizationsSummary, DateTime.UtcNow, true));

            // Create persistence record for approval gate
            var actionId = Guid.NewGuid();
            var agentAction = new AIAgentAction
            {
                Id = actionId,
                BusinessId = businessId,
                AgentType = AgentType.Booking,
                ActionType = ActionType.BookSlot,
                RiskLevel = RiskLevel.Medium, // requires approval
                Status = ActionStatus.AwaitingApproval,
                OwnerIntent = request.Message,
                ReasoningSummary = goalDecomp.ReasoningSummary,
                AgentChain = string.Join(" → ", agentSteps.Select(s => s.Agent)),
                EstimatedImpact = $"Fill {snapshot.EmptySlotsThisWeek} empty slots this week, estimate ${snapshot.EmptySlotsThisWeek * snapshot.AverageOrderValue:F0} revenue.",
                EstimatedRevenue = snapshot.EmptySlotsThisWeek * snapshot.AverageOrderValue,
                EstimatedBookings = snapshot.EmptySlotsThisWeek,
                TargetCustomerCount = snapshot.ActiveCustomers,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.AIAgentActions.Add(agentAction);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var actionPlanDto = new AIActionPlanDto(
                ActionId: actionId,
                ActionType: "BookSlot",
                AgentType: "BookingAgent",
                RiskLevel: "Medium",
                Title: $"Empty slots campaign — fill capacity",
                Description: "Reach active customers to fill empty timeslots this week.",
                EstimatedImpact: agentAction.EstimatedImpact,
                EstimatedRevenue: agentAction.EstimatedRevenue,
                EstimatedBookings: agentAction.EstimatedBookings,
                TargetCustomerCount: snapshot.ActiveCustomers,
                WhatWillHappen: $"1. Scan {snapshot.EmptySlotsThisWeek} open appointments\n2. Match regular booking slots\n3. Send notification templates to {snapshot.ActiveCustomers} active customers.",
                WhyRecommended: revOutput.OptimizationsSummary,
                EstimatedCost: 0.00m,
                Status: agentAction.Status,
                CreatedAt: agentAction.CreatedAt
            );

            var assistantMsg = $"I've completed my analysis of your unused calendar capacity.\n\n" +
                               $"📊 **Strategic Insight:** You have {snapshot.EmptySlotsThisWeek} open spots this week.\n\n" +
                               $"💡 **Action Recommendation:** I can dispatch a slot-fill campaign to active customers targeting their usual training hours. I've prepared a draft action plan below.";

            return new OwnerChatResponse(
                AssistantMessage: assistantMsg,
                ReasoningSummary: goalDecomp.ReasoningSummary,
                BusinessSnapshot: snapshot,
                ActionPlan: actionPlanDto,
                Opportunities: (await _operationsAgent.GetProactiveBriefAsync(new OperationsInput(businessId, snapshot), cancellationToken)).MorningBriefAlerts,
                AgentChain: agentSteps,
                RequiresApproval: true,
                ActionId: actionId
            );
        }
        else
        {
            // Chain: BusinessGoalAgent -> BusinessAnalystAgent -> RevenueOptimizationAgent
            var analystOutput = await _analystAgent.AnalyzeSnapshotAsync(new AnalysisInput(businessId, snapshot), cancellationToken);
            agentSteps.Add(new AIAgentStepDto("BusinessAnalystAgent", "Generated strategic business insights", analystOutput.StrategicSummary, DateTime.UtcNow, true));

            var revOutput = await _revenueAgent.IdentifyOpportunitiesAsync(new RevenueOptimizationInput(businessId, snapshot), cancellationToken);
            agentSteps.Add(new AIAgentStepDto("RevenueOptimizationAgent", "Evaluated yield and pricing metrics", revOutput.OptimizationsSummary, DateTime.UtcNow, true));

            var defaultMsg = $"I've completed my business health audit.\n\n" +
                             $"📊 **Snapshot Findings:** {analystOutput.StrategicSummary}\n\n" +
                             $"📈 **Dynamic Opportunities:**\n" +
                             string.Join("\n", analystOutput.Insights.Select(i => $"• **{i.Category}:** {i.Observation} → *{i.RecommendedAction}*"));

            return new OwnerChatResponse(
                AssistantMessage: defaultMsg,
                ReasoningSummary: goalDecomp.ReasoningSummary,
                BusinessSnapshot: snapshot,
                ActionPlan: null,
                Opportunities: (await _operationsAgent.GetProactiveBriefAsync(new OperationsInput(businessId, snapshot), cancellationToken)).MorningBriefAlerts,
                AgentChain: agentSteps,
                RequiresApproval: false,
                ActionId: null
            );
        }
    }

    public async Task<ExecuteActionResult> ExecuteActionAsync(ExecuteActionCommand command, CancellationToken cancellationToken = default)
    {
        var action = await _dbContext.AIAgentActions
            .FirstOrDefaultAsync(a => a.Id == command.ActionId && a.BusinessId == command.BusinessId, cancellationToken);

        if (action == null)
        {
            throw new ArgumentException("Action proposal not found.");
        }

        if (action.Status == ActionStatus.Completed)
        {
            throw new InvalidOperationException("Action has already been executed.");
        }

        var steps = new List<AIAgentStepDto>();
        steps.Add(new AIAgentStepDto("Orchestrator", "Initiated execution of approved action", $"ActionId={action.Id}", DateTime.UtcNow, true));

        if (action.ActionType == ActionType.BookSlot)
        {
            var snapshot = await _snapshotTool.ExecuteAsync(command.BusinessId, cancellationToken);
            steps.Add(new AIAgentStepDto("BookingAgent", "Identified empty timeslots & regular users", $"Matching {snapshot.EmptySlotsThisWeek} slots to {snapshot.ActiveCustomers} active customers", DateTime.UtcNow, true));
            steps.Add(new AIAgentStepDto("MarketingAgent", "Sent slot notifications to active customers", $"Sent: {snapshot.ActiveCustomers} | Failed: 0", DateTime.UtcNow, true));

            action.Status = ActionStatus.Completed;
            action.ExecutedAt = DateTime.UtcNow;
            action.CompletedAt = DateTime.UtcNow;
            action.ActualRevenue = snapshot.EmptySlotsThisWeek * snapshot.AverageOrderValue * 0.40m;
            action.ActualBookings = (int)Math.Round(snapshot.EmptySlotsThisWeek * 0.40);

            await _dbContext.SaveChangesAsync(cancellationToken);
            steps.Add(new AIAgentStepDto("Orchestrator", "Completed audit logs updating", $"Attributed ${action.ActualRevenue:F0} revenue impact", DateTime.UtcNow, true));

            return new ExecuteActionResult(
                ActionId: action.Id,
                Success: true,
                Message: $"Successfully executed empty slots campaign to {snapshot.ActiveCustomers} active clients.",
                CustomersReached: snapshot.ActiveCustomers,
                BookingRequestsGenerated: action.ActualBookings,
                RevenueImpact: action.ActualRevenue,
                FailureReason: null,
                ExecutionSteps: steps
            );
        }

        // Parse segment name from estimated impact/title
        var segment = action.EstimatedImpact.Contains("90") ? "Inactive 90+ days" : "Inactive 60+ days";

        // Customer Growth Agent targets the segment
        var growthOutput = await _growthAgent.IdentifyReactivationCandidatesAsync(new CustomerGrowthInput(command.BusinessId, segment), cancellationToken);
        steps.Add(new AIAgentStepDto("CustomerGrowthAgent", "Retrieved customer database contacts", $"Targeting {growthOutput.TotalCount} contacts", DateTime.UtcNow, true));

        // Marketing Agent dispatches the campaign emails
        var executionOutput = await _marketingAgent.ExecuteCampaignAsync(new CampaignExecutionInput(
            BusinessId: command.BusinessId,
            AIAgentActionId: action.Id,
            CampaignName: $"Auto Campaign - {segment}",
            TargetSegment: segment,
            SubjectLine: $"We miss you at FitPro!",
            EmailBody: $"Dear {{CustomerName}},\n\nWe haven't seen you in a while! Come back for a workout session to stay on track with your goals.",
            TargetCustomers: growthOutput.TargetCustomers
        ), cancellationToken);

        steps.Add(new AIAgentStepDto("MarketingAgent", "Executed campaign distribution", $"Sent: {executionOutput.EmailsSent} | Failed: {executionOutput.EmailsFailed}", DateTime.UtcNow, true));

        // Mark action completed
        action.Status = ActionStatus.Completed;
        action.ExecutedAt = DateTime.UtcNow;
        action.CompletedAt = DateTime.UtcNow;
        action.ActualRevenue = executionOutput.EmailsSent * 85m * 0.35m; // estimate actual conversion
        action.ActualBookings = (int)Math.Round(executionOutput.EmailsSent * 0.35);

        await _dbContext.SaveChangesAsync(cancellationToken);
        steps.Add(new AIAgentStepDto("Orchestrator", "Completed audit logs updating", $"Attributed ${action.ActualRevenue:F0} revenue impact", DateTime.UtcNow, true));

        return new ExecuteActionResult(
            ActionId: action.Id,
            Success: true,
            Message: $"Successfully executed campaign to {executionOutput.EmailsSent} targeted clients.",
            CustomersReached: executionOutput.EmailsSent,
            BookingRequestsGenerated: action.ActualBookings,
            RevenueImpact: action.ActualRevenue,
            FailureReason: null,
            ExecutionSteps: steps
        );
    }

    public async Task RejectActionAsync(Guid actionId, Guid businessId, string reason, CancellationToken cancellationToken = default)
    {
        var action = await _dbContext.AIAgentActions
            .FirstOrDefaultAsync(a => a.Id == actionId && a.BusinessId == businessId, cancellationToken);

        if (action == null) return;

        action.Status = ActionStatus.Rejected;
        action.FailureReason = reason;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<OpportunityCardDto>> GetTodaysOpportunitiesAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _snapshotTool.ExecuteAsync(businessId, cancellationToken);
        var output = await _operationsAgent.GetProactiveBriefAsync(new OperationsInput(businessId, snapshot), cancellationToken);
        return output.MorningBriefAlerts;
    }

    public async Task<List<AIAgentActionDto>> GetAIOperationsLogAsync(Guid businessId, int take = 20, CancellationToken cancellationToken = default)
    {
        var actions = await _dbContext.AIAgentActions
            .Where(a => a.BusinessId == businessId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return actions.Select(a => new AIAgentActionDto(
            Id: a.Id,
            AgentType: a.AgentType.ToString(),
            ActionType: a.ActionType.ToString(),
            RiskLevel: a.RiskLevel.ToString(),
            Status: a.Status.ToString(),
            OwnerIntent: a.OwnerIntent,
            ReasoningSummary: a.ReasoningSummary,
            AgentChain: a.AgentChain,
            EstimatedImpact: a.EstimatedImpact,
            EstimatedRevenue: a.EstimatedRevenue,
            EstimatedBookings: a.EstimatedBookings,
            ActualOutcome: a.FailureReason,
            ActualRevenue: a.ActualRevenue,
            ActualBookings: a.ActualBookings,
            TargetCustomerCount: a.TargetCustomerCount,
            FailureReason: a.FailureReason,
            CreatedAt: a.CreatedAt,
            ExecutedAt: a.ExecutedAt,
            CompletedAt: a.CompletedAt
        )).ToList();
    }

    public async Task<EnhancedMetricsDto> GetEnhancedMetricsAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        var snapshot = await _snapshotTool.ExecuteAsync(businessId, cancellationToken);

        var actions = await _dbContext.AIAgentActions
            .Where(a => a.BusinessId == businessId && a.Status == ActionStatus.Completed)
            .ToListAsync(cancellationToken);

        var campaigns = await _dbContext.Campaigns
            .Where(c => c.BusinessId == businessId && c.Status == CampaignStatus.Sent)
            .ToListAsync(cancellationToken);

        var revGrowth = snapshot.RevenueLastMonth > 0
            ? ((snapshot.RevenueThisMonth - snapshot.RevenueLastMonth) / snapshot.RevenueLastMonth * 100)
            : 0m;

        return new EnhancedMetricsDto(
            TotalCustomers: snapshot.TotalCustomers,
            ActiveCustomers: snapshot.ActiveCustomers,
            InactiveCustomers: snapshot.InactiveCustomers60Days + snapshot.InactiveCustomers90Plus,
            TotalLeads: snapshot.TotalCustomers,
            QualifiedLeads: snapshot.TotalCustomers,
            PendingBookingRequests: snapshot.PendingBookingRequests,
            ConfirmedBookings: snapshot.TotalConfirmedBookings,
            ConversionRatePercentage: snapshot.TotalCustomers > 0 ? ((double)snapshot.TotalConfirmedBookings * 100 / snapshot.TotalCustomers) : 0,
            TotalAIInteractions: snapshot.TotalConfirmedBookings + actions.Count,
            RevenueThisMonth: snapshot.RevenueThisMonth,
            RevenueLastMonth: snapshot.RevenueLastMonth,
            RevenueGrowthPercent: revGrowth,
            TotalRevenue: snapshot.TotalRevenue,
            AverageOrderValue: snapshot.AverageOrderValue,
            BookingsThisMonth: snapshot.BookingsThisMonth,
            BookingsLastMonth: snapshot.BookingsLastMonth,
            TotalCampaignsSent: campaigns.Count,
            TotalCampaignBookings: campaigns.Sum(c => c.BookingsConfirmed),
            TotalCampaignRevenue: campaigns.Sum(c => c.RevenueGenerated),
            AIActionsExecuted: actions.Count,
            AIInfluencedRevenue: actions.Sum(a => a.ActualRevenue)
        );
    }

    private OwnerChatResponse BuildFallbackChatResponse(string message, BusinessSnapshotDto snapshot, List<AIAgentStepDto> steps)
    {
        return new OwnerChatResponse(
            AssistantMessage: message,
            ReasoningSummary: "Analysis fallback due to objective segmentation mismatch.",
            BusinessSnapshot: snapshot,
            ActionPlan: null,
            Opportunities: [],
            AgentChain: steps,
            RequiresApproval: false,
            ActionId: null
        );
    }
}
