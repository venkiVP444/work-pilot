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

        var bookingRequestResult = await bookingReqResponse.Content.ReadFromJsonAsync<BookingRequestDto>();
        Assert.NotNull(bookingRequestResult);
        Assert.Equal(BookingRequestStatus.PendingApproval, bookingRequestResult.Status);

        // Step 3: Owner retrieves pending booking requests
        var pendingResponse = await client.GetAsync($"/api/booking-requests/pending?businessId={businessId}");
        Assert.Equal(HttpStatusCode.OK, pendingResponse.StatusCode);

        var pendingList = await pendingResponse.Content.ReadFromJsonAsync<List<BookingRequestDto>>();
        Assert.NotNull(pendingList);
        Assert.Contains(pendingList, r => r.Id == bookingRequestResult.Id);

        // Step 4: Owner approves booking request (triggers calendar re-validation & event creation)
        var approveDto = new ApproveBookingRequestDto("Approved by trainer!");
        var approveResponse = await client.PostAsJsonAsync($"/api/booking-requests/{bookingRequestResult.Id}/approve", approveDto);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        var approvedResult = await approveResponse.Content.ReadFromJsonAsync<BookingRequestDto>();
        Assert.NotNull(approvedResult);
        Assert.Equal(BookingRequestStatus.Approved, approvedResult.Status);
        Assert.NotNull(approvedResult.GoogleCalendarEventId);
        Assert.Equal("Simulated", approvedResult.EmailDeliveryStatus); // Simulated in test environment

        // Step 5: Verify Idempotency (approving second time returns same result without duplicate events)
        var approveSecondResponse = await client.PostAsJsonAsync($"/api/booking-requests/{bookingRequestResult.Id}/approve", approveDto);
        Assert.Equal(HttpStatusCode.OK, approveSecondResponse.StatusCode);
        var secondApprovedResult = await approveSecondResponse.Content.ReadFromJsonAsync<BookingRequestDto>();
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
        var req = await reqResponse.Content.ReadFromJsonAsync<BookingRequestDto>();
        Assert.NotNull(req);

        // Approve
        var approveResponse = await client.PostAsJsonAsync($"/api/booking-requests/{req.Id}/approve", new ApproveBookingRequestDto("OK"));
        var approved = await approveResponse.Content.ReadFromJsonAsync<BookingRequestDto>();
        Assert.NotNull(approved);
        var eventId = approved.GoogleCalendarEventId;
        Assert.NotNull(eventId);

        // Call Retry Email Endpoint
        var retryResponse = await client.PostAsync($"/api/booking-requests/{req.Id}/retry-email", null);
        Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);

        var retried = await retryResponse.Content.ReadFromJsonAsync<BookingRequestDto>();
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

        var bookingRequestResult = await bookingReqResponse.Content.ReadFromJsonAsync<BookingRequestDto>();
        Assert.NotNull(bookingRequestResult);

        var rejectDto = new RejectBookingRequestDto("Slot fully booked");
        var rejectResponse = await client.PostAsJsonAsync($"/api/booking-requests/{bookingRequestResult.Id}/reject", rejectDto);
        Assert.Equal(HttpStatusCode.OK, rejectResponse.StatusCode);

        var rejectedResult = await rejectResponse.Content.ReadFromJsonAsync<BookingRequestDto>();
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
}
