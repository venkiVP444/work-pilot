using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkPilot.Application.Agents;
using WorkPilot.Application.Common.Interfaces;
using WorkPilot.Application.DTOs;
using WorkPilot.Application.Tools.Analytics;
using WorkPilot.Application.Tools.Bookings;
using WorkPilot.Application.Tools.Campaigns;
using WorkPilot.Application.Tools.Communications;
using WorkPilot.Application.Tools.Customers;
using WorkPilot.Domain.Entities;
using WorkPilot.Domain.Enums;
using WorkPilot.Infrastructure.Data;
using Xunit;

namespace WorkPilot.UnitTests.Agents;

public class AgentTests
{
    private static readonly BusinessSnapshotDto FakeSnapshot = new(
        BusinessName: "FitPro Gym",
        TotalCustomers: 50,
        ActiveCustomers: 20,
        InactiveCustomers30Days: 10,
        InactiveCustomers60Days: 15,
        InactiveCustomers90Plus: 5,
        RevenueThisMonth: 1000m,
        RevenueLastMonth: 1200m,
        BookingsThisMonth: 12,
        BookingsLastMonth: 14,
        PendingBookingRequests: 2,
        EmptySlotsThisWeek: 5,
        AverageOrderValue: 85m,
        TotalConfirmedBookings: 26,
        TotalRevenue: 2210m,
        TopServices: ["PT Session ($85/session)"]
    );

    private WorkPilotDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<WorkPilotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new WorkPilotDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 1. BUSINESS GOAL AGENT TEST
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task BusinessGoalAgent_ShouldDecomposeGoalIntoReactivationObjective()
    {
        var mockGemini = new StubGeminiAgentService(
            wantsCampaign: true,
            recommendedAction: "CreateCampaign",
            estimatedRevenue: 1500m,
            estimatedBookings: 17
        );
        var agent = new BusinessGoalAgent(mockGemini);

        var input = new DecomposeGoalInput(Guid.NewGuid(), "Grow profit 20%", FakeSnapshot);
        var output = await agent.DecomposeGoalAsync(input);

        Assert.NotNull(output);
        Assert.Single(output.Objectives);
        Assert.Equal("ReactivateCustomers", output.Objectives[0].ObjectiveType);
        Assert.Equal(1500m, output.Objectives[0].ImpactEstimate);
        Assert.Equal("Grow Profit Reasoning", output.ReasoningSummary);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. BUSINESS ANALYST AGENT TEST
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task BusinessAnalystAgent_ShouldGenerateInsights_FromDecliningMetrics()
    {
        var stubSnapshotTool = new StubGetBusinessSnapshotTool(FakeSnapshot);
        var agent = new BusinessAnalystAgent(stubSnapshotTool);

        var input = new AnalysisInput(Guid.NewGuid(), FakeSnapshot);
        var output = await agent.AnalyzeSnapshotAsync(input);

        Assert.NotNull(output);
        Assert.NotEmpty(output.Insights);
        Assert.Contains(output.Insights, i => i.Category == "Financial Performance");
        Assert.Contains(output.Insights, i => i.Category == "Customer Retention");
        Assert.Contains(output.Insights, i => i.Category == "Capacity Optimization");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. CUSTOMER GROWTH AGENT TEST
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task CustomerGrowthAgent_ShouldRetrieveTargetLeads_ForInactiveSegment()
    {
        var db = GetInMemoryDbContext();
        var lead = new Lead
        {
            BusinessId = Guid.NewGuid(),
            Name = "Alice Inactive",
            Email = "alice@example.com",
            LastVisitDate = DateTime.UtcNow.AddDays(-65),
            Status = "Converted",
            Source = "WebChat"
        };
        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        var stubInactiveTool = new StubGetInactiveCustomersTool(new List<Lead> { lead });
        var agent = new CustomerGrowthAgent(stubInactiveTool, db);

        var input = new CustomerGrowthInput(lead.BusinessId, "Inactive 60+ days");
        var output = await agent.IdentifyReactivationCandidatesAsync(input);

        Assert.NotNull(output);
        Assert.Single(output.TargetCustomers);
        Assert.Equal("Alice Inactive", output.TargetCustomers[0].Name);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. MARKETING AGENT TEST
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task MarketingAgent_ShouldPlanCampaignDetails_AndExecuteItSuccessfully()
    {
        var db = GetInMemoryDbContext();
        var stubCreateCampaignTool = new StubCreateCampaignTool(db);
        var stubSendEmailTool = new StubSendCampaignEmailTool(true);
        var stubGemini = new StubGeminiAgentService(wantsCampaign: true, recommendedAction: "CreateCampaign");

        var agent = new MarketingAgent(stubCreateCampaignTool, stubSendEmailTool, stubGemini, db);

        // Test Campaign Planning
        var planInput = new MarketingPlanInput(Guid.NewGuid(), "Inactive 60+ days", 1, "reactivate segment", FakeSnapshot);
        var planOutput = await agent.PlanCampaignAsync(planInput);

        Assert.NotNull(planOutput);
        Assert.Equal("Campaign Subject Line", planOutput.SubjectLine);
        Assert.Equal("Campaign Email Body", planOutput.EmailBody);

        // Test Campaign Execution
        var targetCustomers = new List<Lead>
        {
            new Lead { Name = "Jane Doe", Email = "jane@example.com" }
        };
        var execInput = new CampaignExecutionInput(Guid.NewGuid(), Guid.NewGuid(), "Retention Campaign", "Inactive 60+", planOutput.SubjectLine, planOutput.EmailBody, targetCustomers);
        var execOutput = await agent.ExecuteCampaignAsync(execInput);

        Assert.NotNull(execOutput);
        Assert.Equal(1, execOutput.EmailsSent);
        Assert.Equal(0, execOutput.EmailsFailed);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. BOOKING AGENT TEST
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task BookingAgent_ShouldBookAppointment_ViaCreationTool()
    {
        var stubCreateTool = new StubCreateBookingRequestTool(new BookingRequest
        {
            Id = Guid.NewGuid(),
            ProposedSlotSummary = "PT Session on Saturday @ 9:00 AM",
            Status = BookingRequestStatus.PendingApproval
        });
        var agent = new BookingAgent(stubCreateTool);

        var input = new BookingInput(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddHours(1), "John Doe", "john@example.com", null);
        var output = await agent.HandleBookingTaskAsync(input);

        Assert.NotNull(output);
        Assert.Equal("PT Session on Saturday @ 9:00 AM", output.ProposedSlotSummary);
        Assert.Equal("PendingApproval", output.Status);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 6. REVENUE OPTIMIZATION AGENT TEST
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task RevenueOptimizationAgent_ShouldIdentifyDynamicPricingOpportunities()
    {
        var stubEmptyTool = new StubGetEmptySlotsTool(5);
        var agent = new RevenueOptimizationAgent(stubEmptyTool);

        var input = new RevenueOptimizationInput(Guid.NewGuid(), FakeSnapshot);
        var output = await agent.IdentifyOpportunitiesAsync(input);

        Assert.NotNull(output);
        Assert.NotEmpty(output.Opportunities);
        Assert.Contains(output.Opportunities, o => o.StrategyName == "Dynamic Off-Peak Promotion");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 7. OPERATIONS AGENT TEST
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task OperationsAgent_ShouldBuildMorningBriefAlerts()
    {
        var agent = new OperationsAgent();

        var input = new OperationsInput(Guid.NewGuid(), FakeSnapshot);
        var output = await agent.GetProactiveBriefAsync(input);

        Assert.NotNull(output);
        Assert.NotEmpty(output.MorningBriefAlerts);
        Assert.Contains(output.MorningBriefAlerts, a => a.Icon == "👥");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // STUB IMPLEMENTATIONS FOR MOCK-FREE TESTING
    // ─────────────────────────────────────────────────────────────────────────

    private class StubGetBusinessSnapshotTool : IGetBusinessSnapshotTool
    {
        private readonly BusinessSnapshotDto _dto;
        public StubGetBusinessSnapshotTool(BusinessSnapshotDto dto) => _dto = dto;
        public Task<BusinessSnapshotDto> ExecuteAsync(Guid businessId, CancellationToken cancellationToken = default) => Task.FromResult(_dto);
    }

    private class StubGetInactiveCustomersTool : IGetInactiveCustomersTool
    {
        private readonly List<Lead> _leads;
        public StubGetInactiveCustomersTool(List<Lead> leads) => _leads = leads;
        public Task<List<Lead>> ExecuteAsync(Guid businessId, int thresholdDays, CancellationToken cancellationToken = default) => Task.FromResult(_leads);
    }

    private class StubGetCustomerSegmentsTool : IGetCustomerSegmentsTool
    {
        public Task<List<CustomerSegmentSummary>> ExecuteAsync(Guid businessId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<CustomerSegmentSummary>());
    }

    private class StubGetEmptySlotsTool : IGetEmptySlotsTool
    {
        private readonly int _slots;
        public StubGetEmptySlotsTool(int slots) => _slots = slots;
        public Task<int> ExecuteAsync(Guid businessId, CancellationToken cancellationToken = default) => Task.FromResult(_slots);
    }

    private class StubCreateBookingRequestTool : ICreateBookingRequestTool
    {
        private readonly BookingRequest _req;
        public StubCreateBookingRequestTool(BookingRequest req) => _req = req;
        public Task<BookingRequest> ExecuteAsync(BookingRequestCommandInput input, CancellationToken cancellationToken = default) => Task.FromResult(_req);
    }

    private class StubCreateCampaignTool : ICreateCampaignTool
    {
        private readonly WorkPilotDbContext _db;
        public StubCreateCampaignTool(WorkPilotDbContext db) => _db = db;

        public async Task<Campaign> ExecuteAsync(CreateCampaignInput input, CancellationToken cancellationToken = default)
        {
            var campaign = new Campaign
            {
                BusinessId = input.BusinessId,
                Name = input.Name,
                Status = CampaignStatus.Draft
            };
            _db.Campaigns.Add(campaign);
            await _db.SaveChangesAsync(cancellationToken);
            return campaign;
        }
    }

    private class StubSendCampaignEmailTool : ISendCampaignEmailTool
    {
        private readonly bool _success;
        public StubSendCampaignEmailTool(bool success) => _success = success;
        public Task<bool> ExecuteAsync(SendEmailInput input, CancellationToken cancellationToken = default) => Task.FromResult(_success);
    }

    private class StubGeminiAgentService : IGeminiAgentService
    {
        private readonly string _action;
        private readonly decimal _revenue;
        private readonly int _bookings;

        public StubGeminiAgentService(bool wantsCampaign, string recommendedAction, decimal estimatedRevenue = 1500m, int estimatedBookings = 17)
        {
            _action = recommendedAction;
            _revenue = estimatedRevenue;
            _bookings = estimatedBookings;
        }

        public Task<GeminiStructuredResponse> ProcessCustomerMessageAsync(GeminiAgentRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<OwnerIntentResponse> ProcessOwnerIntentAsync(OwnerIntentRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new OwnerIntentResponse(
                ActiveAgents: ["Orchestrator", "BusinessAnalyst", "CustomerGrowth", "Marketing"],
                ReasoningSummary: "Grow Profit Reasoning",
                AssistantMessage: "I've analyzed your business and recommend customer reactivation.",
                RecommendedActionType: _action,
                RiskLevel: "Medium",
                EstimatedImpact: "15 bookings, $1,500 revenue",
                EstimatedRevenue: _revenue,
                EstimatedBookings: _bookings,
                TargetCustomerCount: 15,
                WhatWillHappen: "What will happen",
                WhyRecommended: "Why recommended",
                CampaignSubjectLine: "Campaign Subject Line",
                CampaignEmailBody: "Campaign Email Body",
                CampaignOfferDescription: "Reactivation promotion",
                TargetSegment: "Inactive 60+ days"
            ));
        }
    }
}
