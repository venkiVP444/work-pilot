using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;
using WorkPilot.Application.Common.Interfaces;
using WorkPilot.Application.DTOs;
using WorkPilot.Domain.Entities;
using WorkPilot.Domain.Enums;
using WorkPilot.Infrastructure.Data;
using Xunit;

namespace WorkPilot.IntegrationTests;

public class BookingFlowIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public BookingFlowIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((ctx, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"Email:Provider", "Simulated"}
                });
            });

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<WorkPilotDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                var dbName = "IntegrationTestDb_" + Guid.NewGuid().ToString("N");
                services.AddDbContext<WorkPilotDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName);
                });
            });
        });
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task FullEndToEndBookingWorkflow_ShouldSucceed()
    {
        var client = _factory.CreateClient();
        var businessId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Step 1: Customer sends message requesting Saturday morning slot
        var chatRequest = new CustomerChatMessageRequest(
            CustomerMessage: "Hi, I want to start personal training. Do you have anything this Saturday morning?",
            ConversationId: null
        );

        var chatResponseMsg = await client.PostAsJsonAsync($"/api/customer/{businessId}/conversation/message", chatRequest);
        Assert.Equal(HttpStatusCode.OK, chatResponseMsg.StatusCode);

        var chatResult = await chatResponseMsg.Content.ReadFromJsonAsync<CustomerChatMessageResponse>();
        Assert.NotNull(chatResult);
        Assert.Equal(businessId, chatResult.BusinessId);
        Assert.NotEmpty(chatResult.ProposedSlots);

        var selectedSlot = chatResult.ProposedSlots[0];
        var serviceId = chatResult.MatchedServiceId ?? Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Step 2: Customer selects slot and submits booking request
        var bookingCommand = new CreateBookingRequestCommand(
            BusinessId: businessId,
            ConversationId: chatResult.ConversationId,
            ServiceId: serviceId,
            RequestedStartTime: selectedSlot.StartTime,
            RequestedEndTime: selectedSlot.EndTime,
            CustomerName: "John Doe",
            CustomerEmail: "johndoe@example.com",
            CustomerPhone: "+15551234567"
        );

        var bookingReqResponse = await client.PostAsJsonAsync($"/api/customer/{businessId}/booking-request", bookingCommand);
        Assert.Equal(HttpStatusCode.OK, bookingReqResponse.StatusCode);

        var bookingRequestResult = await bookingReqResponse.Content.ReadFromJsonAsync<BookingRequestDto>(_jsonOptions);
        Assert.NotNull(bookingRequestResult);
        Assert.Equal(BookingRequestStatus.PendingApproval, bookingRequestResult.Status);

        // Step 3: Owner retrieves pending booking requests
        var pendingResponse = await client.GetAsync($"/api/booking-requests/pending?businessId={businessId}");
        Assert.Equal(HttpStatusCode.OK, pendingResponse.StatusCode);

        var pendingList = await pendingResponse.Content.ReadFromJsonAsync<List<BookingRequestDto>>(_jsonOptions);
        Assert.NotNull(pendingList);
        Assert.Contains(pendingList, r => r.Id == bookingRequestResult.Id);

        // Step 4: Owner approves booking request (triggers calendar re-validation & event creation)
        var approveDto = new ApproveBookingRequestDto("Approved by trainer!");
        var approveResponse = await client.PostAsJsonAsync($"/api/booking-requests/{bookingRequestResult.Id}/approve", approveDto);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        var approvedResult = await approveResponse.Content.ReadFromJsonAsync<BookingRequestDto>(_jsonOptions);
        Assert.NotNull(approvedResult);
        Assert.Equal(BookingRequestStatus.Approved, approvedResult.Status);
        Assert.NotNull(approvedResult.GoogleCalendarEventId);
        Assert.Equal("Simulated", approvedResult.EmailDeliveryStatus); // Simulated in test environment

        // Step 5: Verify Idempotency (approving second time returns same result without duplicate events)
        var approveSecondResponse = await client.PostAsJsonAsync($"/api/booking-requests/{bookingRequestResult.Id}/approve", approveDto);
        Assert.Equal(HttpStatusCode.OK, approveSecondResponse.StatusCode);
        var secondApprovedResult = await approveSecondResponse.Content.ReadFromJsonAsync<BookingRequestDto>(_jsonOptions);
        Assert.NotNull(secondApprovedResult);
        Assert.Equal(approvedResult.GoogleCalendarEventId, secondApprovedResult.GoogleCalendarEventId);

        // Step 6: Check Dashboard Metrics update
        var metricsResponse = await client.GetAsync($"/api/metrics/{businessId}");
        Assert.Equal(HttpStatusCode.OK, metricsResponse.StatusCode);

        var metrics = await metricsResponse.Content.ReadFromJsonAsync<DashboardMetricsDto>();
        Assert.NotNull(metrics);
        Assert.True(metrics.TotalLeads >= 1);
        Assert.True(metrics.ConfirmedBookings >= 1);
    }

    [Fact]
    public async Task RetryEmail_ShouldUpdateEmailStatusWithoutDuplicatingCalendarEvent()
    {
        var client = _factory.CreateClient();
        var businessId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var bookingCommand = new CreateBookingRequestCommand(
            BusinessId: businessId,
            ConversationId: Guid.NewGuid(),
            ServiceId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            RequestedStartTime: DateTime.UtcNow.AddDays(10),
            RequestedEndTime: DateTime.UtcNow.AddDays(10).AddHours(1),
            CustomerName: "Retry Test",
            CustomerEmail: "retry@example.com",
            CustomerPhone: null
        );

        var reqResponse = await client.PostAsJsonAsync($"/api/customer/{businessId}/booking-request", bookingCommand);
        var req = await reqResponse.Content.ReadFromJsonAsync<BookingRequestDto>(_jsonOptions);
        Assert.NotNull(req);

        // Approve
        var approveResponse = await client.PostAsJsonAsync($"/api/booking-requests/{req.Id}/approve", new ApproveBookingRequestDto("OK"));
        var approved = await approveResponse.Content.ReadFromJsonAsync<BookingRequestDto>(_jsonOptions);
        Assert.NotNull(approved);
        var eventId = approved.GoogleCalendarEventId;
        Assert.NotNull(eventId);

        // Call Retry Email Endpoint
        var retryResponse = await client.PostAsync($"/api/booking-requests/{req.Id}/retry-email", null);
        Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);

        var retried = await retryResponse.Content.ReadFromJsonAsync<BookingRequestDto>(_jsonOptions);
        Assert.NotNull(retried);
        Assert.Equal(eventId, retried.GoogleCalendarEventId); // Calendar Event ID preserved!
        Assert.Equal("Simulated", retried.EmailDeliveryStatus);
    }

    [Fact]
    public async Task RejectBookingWorkflow_ShouldUpdateStatusToRejected()
    {
        var client = _factory.CreateClient();
        var businessId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var bookingCommand = new CreateBookingRequestCommand(
            BusinessId: businessId,
            ConversationId: Guid.NewGuid(),
            ServiceId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            RequestedStartTime: DateTime.UtcNow.AddDays(20),
            RequestedEndTime: DateTime.UtcNow.AddDays(20).AddHours(1),
            CustomerName: "Reject Test",
            CustomerEmail: "reject@example.com",
            CustomerPhone: null
        );

        var bookingReqResponse = await client.PostAsJsonAsync($"/api/customer/{businessId}/booking-request", bookingCommand);
        Assert.Equal(HttpStatusCode.OK, bookingReqResponse.StatusCode);

        var bookingRequestResult = await bookingReqResponse.Content.ReadFromJsonAsync<BookingRequestDto>(_jsonOptions);
        Assert.NotNull(bookingRequestResult);

        var rejectDto = new RejectBookingRequestDto("Slot fully booked");
        var rejectResponse = await client.PostAsJsonAsync($"/api/booking-requests/{bookingRequestResult.Id}/reject", rejectDto);
        Assert.Equal(HttpStatusCode.OK, rejectResponse.StatusCode);

        var rejectedResult = await rejectResponse.Content.ReadFromJsonAsync<BookingRequestDto>(_jsonOptions);
        Assert.NotNull(rejectedResult);
        Assert.Equal(BookingRequestStatus.Rejected, rejectedResult.Status);
        Assert.Equal("Slot fully booked", rejectedResult.OwnerNotes);
    }

    [Fact]
    public async Task AlreadyBookedSlot_ShouldNotBeProposedAgainInChat()
    {
        var client = _factory.CreateClient();
        var businessId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Query initial slots for Saturday
        var chatRequest1 = new CustomerChatMessageRequest("Do you have anything this Saturday morning?", null);
        var res1 = await client.PostAsJsonAsync($"/api/customer/{businessId}/conversation/message", chatRequest1);
        var chatResult1 = await res1.Content.ReadFromJsonAsync<CustomerChatMessageResponse>();
        Assert.NotNull(chatResult1);
        Assert.NotEmpty(chatResult1.ProposedSlots);

        var bookedSlot = chatResult1.ProposedSlots[0];

        // Submit booking request for this slot
        var bookingCommand = new CreateBookingRequestCommand(
            BusinessId: businessId,
            ConversationId: chatResult1.ConversationId,
            ServiceId: chatResult1.MatchedServiceId ?? Guid.Parse("22222222-2222-2222-2222-222222222222"),
            RequestedStartTime: bookedSlot.StartTime,
            RequestedEndTime: bookedSlot.EndTime,
            CustomerName: "First Customer",
            CustomerEmail: "customer1@example.com",
            CustomerPhone: null
        );
        await client.PostAsJsonAsync($"/api/customer/{businessId}/booking-request", bookingCommand);

        // Query slots for Saturday again
        var chatRequest2 = new CustomerChatMessageRequest("Do you have anything this Saturday morning?", null);
        var res2 = await client.PostAsJsonAsync($"/api/customer/{businessId}/conversation/message", chatRequest2);
        var chatResult2 = await res2.Content.ReadFromJsonAsync<CustomerChatMessageResponse>();
        Assert.NotNull(chatResult2);

        // Verify that the booked slot is NO LONGER proposed
        Assert.DoesNotContain(chatResult2.ProposedSlots, s => s.StartTime == bookedSlot.StartTime);
    }

    [Fact]
    public async Task OwnerAIChat_ShouldReturnReactivationCampaignPlan_WhenRevenueGrowthRequested()
    {
        var client = _factory.CreateClient();
        var businessId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var chatRequest = new OwnerChatRequest("I need to increase my profit by 20% this month");
        var response = await client.PostAsJsonAsync($"/api/owner/{businessId}/chat", chatRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OwnerChatResponse>(_jsonOptions);
        Assert.NotNull(result);
        Assert.Contains("reactivat", result.AssistantMessage.ToLower());
        Assert.NotNull(result.ActionPlan);
        Assert.Equal("Re-engagement campaign — Inactive 60+ days", result.ActionPlan.Title);
        Assert.Equal("Medium", result.ActionPlan.RiskLevel);
        Assert.True(result.RequiresApproval);
        Assert.NotNull(result.ActionId);
        Assert.NotEmpty(result.AgentChain);
    }

    [Fact]
    public async Task OwnerAIExecuteAction_ShouldExecuteCampaignAndSendEmails()
    {
        var client = _factory.CreateClient();
        var businessId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // 1. Propose action via chat
        var chatRequest = new OwnerChatRequest("reactivate my customers please");
        var chatRes = await client.PostAsJsonAsync($"/api/owner/{businessId}/chat", chatRequest);
        var chatResult = await chatRes.Content.ReadFromJsonAsync<OwnerChatResponse>(_jsonOptions);
        Assert.NotNull(chatResult);
        Assert.NotNull(chatResult.ActionId);

        // 2. Execute approved action
        var executeCommand = new ExecuteActionCommand(businessId, chatResult.ActionId.Value, "Do it!");
        var execRes = await client.PostAsJsonAsync($"/api/owner/{businessId}/execute-action", executeCommand);
        Assert.Equal(HttpStatusCode.OK, execRes.StatusCode);

        var execResult = await execRes.Content.ReadFromJsonAsync<ExecuteActionResult>(_jsonOptions);
        Assert.NotNull(execResult);
        Assert.True(execResult.Success);
        Assert.True(execResult.CustomersReached > 0);
        Assert.NotEmpty(execResult.ExecutionSteps);

        // 3. Verify actual outcome in operations log
        var opsResponse = await client.GetAsync($"/api/owner/{businessId}/ai-operations?take=5");
        Assert.Equal(HttpStatusCode.OK, opsResponse.StatusCode);
        var opsList = await opsResponse.Content.ReadFromJsonAsync<List<AIAgentActionDto>>(_jsonOptions);
        Assert.NotNull(opsList);
        Assert.Contains(opsList, op => op.Id == chatResult.ActionId.Value && op.Status == "Completed");
    }

    [Fact]
    public async Task OwnerAIRejectAction_ShouldMarkActionAsRejected()
    {
        var client = _factory.CreateClient();
        var businessId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // 1. Propose action via chat
        var chatRequest = new OwnerChatRequest("run slot campaign");
        var chatRes = await client.PostAsJsonAsync($"/api/owner/{businessId}/chat", chatRequest);
        var chatResult = await chatRes.Content.ReadFromJsonAsync<OwnerChatResponse>(_jsonOptions);
        Assert.NotNull(chatResult);
        Assert.NotNull(chatResult.ActionId);

        // 2. Reject action
        var rejectRequest = new { Reason = "Too busy right now" };
        var rejectRes = await client.PostAsJsonAsync($"/api/owner/{businessId}/reject-action/{chatResult.ActionId.Value}", rejectRequest);
        Assert.Equal(HttpStatusCode.OK, rejectRes.StatusCode);

        // 3. Verify status in log
        var opsResponse = await client.GetAsync($"/api/owner/{businessId}/ai-operations?take=5");
        var opsList = await opsResponse.Content.ReadFromJsonAsync<List<AIAgentActionDto>>(_jsonOptions);
        Assert.NotNull(opsList);
        Assert.Contains(opsList, op => op.Id == chatResult.ActionId.Value && op.Status == "Rejected");
    }

    [Fact]
    public async Task OwnerAIOpportunities_ShouldReturnOpportunitiesList()
    {
        var client = _factory.CreateClient();
        var businessId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var response = await client.GetAsync($"/api/owner/{businessId}/opportunities");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var opportunities = await response.Content.ReadFromJsonAsync<List<OpportunityCardDto>>(_jsonOptions);
        Assert.NotNull(opportunities);
        Assert.NotEmpty(opportunities);
    }

    [Fact]
    public async Task OwnerAIEnhancedMetrics_ShouldReturnMetricsWithRevenueAndAIVolume()
    {
        var client = _factory.CreateClient();
        var businessId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var response = await client.GetAsync($"/api/owner/{businessId}/metrics/enhanced");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var metrics = await response.Content.ReadFromJsonAsync<EnhancedMetricsDto>(_jsonOptions);
        Assert.NotNull(metrics);
        Assert.True(metrics.TotalCustomers >= 50); // From DbInitializer seed
        Assert.True(metrics.TotalRevenue > 0);
    }

    [Fact]
    public async Task OwnerAIChat_ShouldReturnAnalysisOnly_WhenRevenueDropRequested()
    {
        var client = _factory.CreateClient();
        var businessId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var chatRequest = new OwnerChatRequest("Why did my revenue drop?");
        var response = await client.PostAsJsonAsync($"/api/owner/{businessId}/chat", chatRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OwnerChatResponse>(_jsonOptions);
        Assert.NotNull(result);
        Assert.Contains("business health audit", result.AssistantMessage.ToLower());
        Assert.Null(result.ActionPlan); // analysis only - Low risk
        Assert.False(result.RequiresApproval);
        Assert.Null(result.ActionId);
        
        // Check dynamic agent steps chain: Orchestrator -> BusinessAnalyst -> BusinessGoalAgent -> BusinessAnalystAgent -> RevenueOptimizationAgent
        Assert.Contains(result.AgentChain, s => s.Agent.Contains("RevenueOptimizationAgent"));
    }

    [Fact]
    public async Task OwnerAIChat_ShouldReturnBookingSlotActionPlan_WhenFillSlotsRequested()
    {
        var client = _factory.CreateClient();
        var businessId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var chatRequest = new OwnerChatRequest("Fill my empty slots tomorrow");
        var response = await client.PostAsJsonAsync($"/api/owner/{businessId}/chat", chatRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<OwnerChatResponse>(_jsonOptions);
        Assert.NotNull(result);
        Assert.Contains("calendar capacity", result.AssistantMessage.ToLower());
        Assert.NotNull(result.ActionPlan);
        Assert.Equal("BookSlot", result.ActionPlan.ActionType);
        Assert.Equal("BookingAgent", result.ActionPlan.AgentType);
        Assert.True(result.RequiresApproval);
        Assert.NotNull(result.ActionId);
        Assert.Contains(result.AgentChain, s => s.Agent.Contains("RevenueOptimizationAgent"));
    }

    [Fact]
    public async Task OwnerAIExecuteAction_ShouldExecuteSlotFilling_WhenBookSlotApproved()
    {
        var client = _factory.CreateClient();
        var businessId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // 1. Propose BookSlot action
        var chatRequest = new OwnerChatRequest("fill my empty slots");
        var chatRes = await client.PostAsJsonAsync($"/api/owner/{businessId}/chat", chatRequest);
        var chatResult = await chatRes.Content.ReadFromJsonAsync<OwnerChatResponse>(_jsonOptions);
        Assert.NotNull(chatResult);
        Assert.NotNull(chatResult.ActionId);
        Assert.Equal("BookSlot", chatResult.ActionPlan?.ActionType);

        // 2. Execute approved action
        var executeCommand = new ExecuteActionCommand(businessId, chatResult.ActionId.Value, "Approved");
        var execRes = await client.PostAsJsonAsync($"/api/owner/{businessId}/execute-action", executeCommand);
        Assert.Equal(HttpStatusCode.OK, execRes.StatusCode);

        var execResult = await execRes.Content.ReadFromJsonAsync<ExecuteActionResult>(_jsonOptions);
        Assert.NotNull(execResult);
        Assert.True(execResult.Success);
        Assert.True(execResult.CustomersReached > 0);
        Assert.Contains(execResult.ExecutionSteps, s => s.Agent == "BookingAgent");
    }

    [Fact]
    public async Task DbInitializer_ShouldSeedCorrectCustomerSegments_AndConfirmedBookings()
    {
        var client = _factory.CreateClient();
        var businessId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var response = await client.GetAsync($"/api/owner/{businessId}/snapshot");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var snapshot = await response.Content.ReadFromJsonAsync<BusinessSnapshotDto>(_jsonOptions);
        Assert.NotNull(snapshot);

        // Verify segments (exactly matching seeded distribution):
        Assert.Equal(20, snapshot.ActiveCustomers);
        Assert.Equal(8, snapshot.InactiveCustomers30Days);
        Assert.Equal(12, snapshot.InactiveCustomers60Days);
        Assert.Equal(10, snapshot.InactiveCustomers90Plus);

        // Verify total confirmed bookings seeded
        Assert.True(snapshot.RevenueThisMonth > 0 || snapshot.RevenueLastMonth > 0);
    }

    [Fact]
    public async Task OwnerAIExecuteAction_ShouldExecuteReactivationCampaign_WhenCreateCampaignApproved()
    {
        var client = _factory.CreateClient();
        var businessId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // 1. Propose CreateCampaign action
        var chatRequest = new OwnerChatRequest("I need to make 20% more profit this month");
        var chatRes = await client.PostAsJsonAsync($"/api/owner/{businessId}/chat", chatRequest);
        var chatResult = await chatRes.Content.ReadFromJsonAsync<OwnerChatResponse>(_jsonOptions);
        Assert.NotNull(chatResult);
        Assert.NotNull(chatResult.ActionId);
        Assert.Equal("CreateCampaign", chatResult.ActionPlan?.ActionType);

        // 2. Execute approved action
        var executeCommand = new ExecuteActionCommand(businessId, chatResult.ActionId.Value, "Approved");
        var execRes = await client.PostAsJsonAsync($"/api/owner/{businessId}/execute-action", executeCommand);
        
        if (execRes.StatusCode != HttpStatusCode.OK)
        {
            var errText = await execRes.Content.ReadAsStringAsync();
            throw new Exception($"Execution failed: {errText}");
        }

        var execResult = await execRes.Content.ReadFromJsonAsync<ExecuteActionResult>(_jsonOptions);
        Assert.NotNull(execResult);
        Assert.True(execResult.Success);
        Assert.True(execResult.CustomersReached > 0);
    }

    [Fact]
    public async Task MultiBusiness_ShouldAllowOnboarding_AndMaintainStrictIsolation()
    {
        var client = _factory.CreateClient();
        var fitProId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // 1. Verify existing demo (FitPro) baseline metrics
        var fitProSnapRes = await client.GetAsync($"/api/owner/{fitProId}/snapshot");
        Assert.Equal(HttpStatusCode.OK, fitProSnapRes.StatusCode);
        var fitProSnap = await fitProSnapRes.Content.ReadFromJsonAsync<BusinessSnapshotDto>(_jsonOptions);
        Assert.NotNull(fitProSnap);
        Assert.Equal(20, fitProSnap.ActiveCustomers); // Seeded default active count

        // 2. Onboard new business "Alpha Studio"
        var createDto = new CreateBusinessDto(
            Name: "Alpha Studio",
            Description: "Yoga and Pilates boutique",
            Location: "Seattle",
            ContactEmail: "owner@alphastudio.com",
            TimeZone: "PST",
            CancellationPolicy: "12 hours",
            CommunicationTone: "Professional"
        );
        var createRes = await client.PostAsJsonAsync("/api/businesses", createDto);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var alphaDto = await createRes.Content.ReadFromJsonAsync<BusinessDto>(_jsonOptions);
        Assert.NotNull(alphaDto);
        Assert.NotEqual(Guid.Empty, alphaDto.Id);
        Assert.NotEqual(fitProId, alphaDto.Id);

        // 3. List all businesses and verify both exist
        var listRes = await client.GetAsync("/api/businesses");
        Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);
        var list = await listRes.Content.ReadFromJsonAsync<List<BusinessDto>>(_jsonOptions);
        Assert.NotNull(list);
        Assert.Contains(list, b => b.Id == fitProId);
        Assert.Contains(list, b => b.Id == alphaDto.Id);

        // 4. Verify new business starts clean (0 customers)
        var alphaSnapRes = await client.GetAsync($"/api/owner/{alphaDto.Id}/snapshot");
        Assert.Equal(HttpStatusCode.OK, alphaSnapRes.StatusCode);
        var alphaSnap = await alphaSnapRes.Content.ReadFromJsonAsync<BusinessSnapshotDto>(_jsonOptions);
        Assert.NotNull(alphaSnap);
        Assert.Equal(0, alphaSnap.TotalCustomers); // No leads seeded for new business

        // 5. Create service for Alpha Studio
        var createServicePayload = new CreateServiceDto(
            Name: "Yoga Flow",
            Description: "Dynamic yoga class",
            Price: 60.00m,
            DurationMinutes: 60
        );
        var serviceRes = await client.PostAsJsonAsync($"/api/businesses/{alphaDto.Id}/services", createServicePayload);
        Assert.Equal(HttpStatusCode.OK, serviceRes.StatusCode);
        var alphaService = await serviceRes.Content.ReadFromJsonAsync<ServiceDto>(_jsonOptions);
        Assert.NotNull(alphaService);

        // 6. Create lead with same email on both businesses
        var commonEmail = "common@customer.com";
        
        // Lead for FitPro
        var fitProLeadCmd = new CreateBookingRequestCommand(
            BusinessId: fitProId,
            ConversationId: Guid.NewGuid(),
            ServiceId: Guid.NewGuid(), // will fallback to default service
            RequestedStartTime: DateTime.UtcNow.AddDays(1),
            RequestedEndTime: DateTime.UtcNow.AddDays(1).AddHours(1),
            CustomerName: "FitPro User",
            CustomerEmail: commonEmail,
            CustomerPhone: "111-111"
        );
        var fitProBookingRes = await client.PostAsJsonAsync($"/api/customer/{fitProId}/booking-request", fitProLeadCmd);
        Assert.Equal(HttpStatusCode.OK, fitProBookingRes.StatusCode);

        // Lead for Alpha Studio
        var alphaLeadCmd = new CreateBookingRequestCommand(
            BusinessId: alphaDto.Id,
            ConversationId: Guid.NewGuid(),
            ServiceId: alphaService.Id, // use the actual created service Id
            RequestedStartTime: DateTime.UtcNow.AddDays(2),
            RequestedEndTime: DateTime.UtcNow.AddDays(2).AddHours(1),
            CustomerName: "Alpha User",
            CustomerEmail: commonEmail,
            CustomerPhone: "222-222"
        );
        var alphaBookingRes = await client.PostAsJsonAsync($"/api/customer/{alphaDto.Id}/booking-request", alphaLeadCmd);
        Assert.Equal(HttpStatusCode.OK, alphaBookingRes.StatusCode);

        // 7. Verify isolation in database snapshots
        var fitProSnapFinalRes = await client.GetAsync($"/api/owner/{fitProId}/snapshot");
        var fitProSnapFinal = await fitProSnapFinalRes.Content.ReadFromJsonAsync<BusinessSnapshotDto>(_jsonOptions);
        
        var alphaSnapFinalRes = await client.GetAsync($"/api/owner/{alphaDto.Id}/snapshot");
        var alphaSnapFinal = await alphaSnapFinalRes.Content.ReadFromJsonAsync<BusinessSnapshotDto>(_jsonOptions);

        Assert.NotNull(fitProSnapFinal);
        Assert.NotNull(alphaSnapFinal);

        // FitPro has 50 seeded + 1 new customer = 51 total customers
        Assert.Equal(51, fitProSnapFinal.TotalCustomers);
        
        // Alpha Studio has exactly 1 customer
        Assert.Equal(1, alphaSnapFinal.TotalCustomers);
    }
}

