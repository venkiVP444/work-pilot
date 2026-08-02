using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkPilot.Application.Common.Interfaces;
using WorkPilot.Application.DTOs;
using WorkPilot.Domain.Entities;
using WorkPilot.Domain.Enums;

namespace WorkPilot.Application.Services;

public class BookingOrchestratorService
{
    private readonly IWorkPilotDbContext _dbContext;
    private readonly IGeminiAgentService _geminiAgent;
    private readonly IGoogleCalendarService _calendarService;
    private readonly IEmailService _emailService;
    private readonly ILogger<BookingOrchestratorService> _logger;

    public BookingOrchestratorService(
        IWorkPilotDbContext dbContext,
        IGeminiAgentService geminiAgent,
        IGoogleCalendarService calendarService,
        IEmailService emailService,
        ILogger<BookingOrchestratorService> logger)
    {
        _dbContext = dbContext;
        _geminiAgent = geminiAgent;
        _calendarService = calendarService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<CustomerChatMessageResponse> HandleCustomerMessageAsync(
        Guid businessId,
        CustomerChatMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var business = await _dbContext.Businesses
            .Include(b => b.Services)
            .Include(b => b.AvailabilityRules)
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

        if (business == null)
        {
            throw new ArgumentException($"Business {businessId} not found.");
        }

        Conversation? conversation = null;
        if (request.ConversationId.HasValue && request.ConversationId.Value != Guid.Empty)
        {
            conversation = await _dbContext.Conversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value, cancellationToken);
        }

        if (conversation == null)
        {
            conversation = new Conversation { BusinessId = businessId };
            _dbContext.Conversations.Add(conversation);
        }

        conversation.Messages.Add(new ConversationMessage
        {
            ConversationId = conversation.Id,
            Role = MessageRole.Customer,
            Content = request.CustomerMessage,
            CreatedAt = DateTime.UtcNow
        });

        var activeServices = business.Services
            .Where(s => s.IsActive)
            .Select(s => new ServiceDto(s.Id, s.BusinessId, s.Name, s.Description, s.Price, s.DurationMinutes, s.IsActive, s.CreatedAt))
            .ToList();

        var agentRequest = new GeminiAgentRequest(businessId, conversation.Id, request.CustomerMessage, activeServices);
        var aiResult = await _geminiAgent.ProcessCustomerMessageAsync(agentRequest, cancellationToken);

        var proposedSlots = new List<CalendarSlotDto>();
        var selectedService = activeServices.FirstOrDefault(s => s.Id == aiResult.ServiceId) ?? activeServices.FirstOrDefault();
        var targetDate = DetermineTargetDate(aiResult.DatePreference ?? request.CustomerMessage);

        if (selectedService != null && (aiResult.Decision == DecisionType.ProposeSlots || aiResult.Intent == IntentType.BookingRequest))
        {
            var busyIntervals = await _calendarService.GetFreeBusyIntervalsAsync(
                businessId,
                targetDate.Date,
                targetDate.Date.AddDays(1).AddSeconds(-1),
                cancellationToken);

            var existingRequests = await _dbContext.BookingRequests
                .Where(br => br.BusinessId == businessId &&
                             br.Status != BookingRequestStatus.Rejected &&
                             br.RequestedStartTime >= targetDate.Date &&
                             br.RequestedStartTime < targetDate.Date.AddDays(1))
                .ToListAsync(cancellationToken);

            foreach (var req in existingRequests)
            {
                busyIntervals.Add(new TimeIntervalDto(req.RequestedStartTime, req.RequestedEndTime));
            }

            proposedSlots = SlotCalculationEngine.CalculateAvailableSlots(
                business.AvailabilityRules.ToList(),
                selectedService.DurationMinutes,
                busyIntervals,
                targetDate,
                aiResult.TimePreference);
        }

        var assistantMessage = aiResult.AssistantMessage;
        if (!proposedSlots.Any() && (aiResult.Decision == DecisionType.ProposeSlots || aiResult.Intent == IntentType.BookingRequest))
        {
            var requestedDay = targetDate.ToString("dddd");
            bool hasActiveRule = business.AvailabilityRules.Any(r => r.DayOfWeek == targetDate.DayOfWeek && r.IsActive);
            if (!hasActiveRule)
            {
                assistantMessage = $"We are currently closed on {requestedDay}s. Our open studio days are Monday through Saturday. Would you like to check open slots for another day?";
            }
            else
            {
                assistantMessage = $"All slots for {requestedDay}, {targetDate:MMM d} are currently fully booked. Would you like to check another day or time?";
            }
        }

        conversation.Messages.Add(new ConversationMessage
        {
            ConversationId = conversation.Id,
            Role = MessageRole.Assistant,
            Content = assistantMessage,
            CreatedAt = DateTime.UtcNow
        });

        conversation.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CustomerChatMessageResponse(
            ConversationId: conversation.Id,
            BusinessId: businessId,
            AssistantMessage: assistantMessage,
            ProposedSlots: proposedSlots,
            MissingInformation: aiResult.MissingInformation,
            Intent: aiResult.Intent.ToString(),
            Decision: aiResult.Decision.ToString(),
            MatchedServiceId: selectedService?.Id
        );
    }

    public async Task<BookingRequestDto> CreateBookingRequestAsync(
        CreateBookingRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        var defaultBusinessId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var targetBusinessId = command.BusinessId != Guid.Empty ? command.BusinessId : defaultBusinessId;

        var business = await _dbContext.Businesses.FirstOrDefaultAsync(b => b.Id == targetBusinessId, cancellationToken)
            ?? await _dbContext.Businesses.FirstOrDefaultAsync(cancellationToken);

        if (business == null)
        {
            throw new ArgumentException("Business not found.");
        }

        var service = await _dbContext.Services.FirstOrDefaultAsync(s => s.Id == command.ServiceId, cancellationToken)
            ?? await _dbContext.Services.FirstOrDefaultAsync(s => s.BusinessId == business.Id, cancellationToken);

        if (service == null)
        {
            throw new ArgumentException("Service not found.");
        }

        var lead = await _dbContext.Leads.FirstOrDefaultAsync(l => l.BusinessId == business.Id && l.Email == command.CustomerEmail, cancellationToken);
        if (lead == null)
        {
            lead = new Lead
            {
                BusinessId = business.Id,
                Name = command.CustomerName,
                Email = command.CustomerEmail,
                Phone = command.CustomerPhone,
                Status = "Qualified",
                Source = "WebChat"
            };
            _dbContext.Leads.Add(lead);
        }
        else
        {
            lead.Status = "Qualified";
            lead.Name = command.CustomerName;
        }

        var conversation = await _dbContext.Conversations.FirstOrDefaultAsync(c => c.Id == command.ConversationId, cancellationToken);
        if (conversation != null)
        {
            conversation.LeadId = lead.Id;
        }

        var bookingRequest = new BookingRequest
        {
            BusinessId = business.Id,
            LeadId = lead.Id,
            ServiceId = service.Id,
            RequestedStartTime = command.RequestedStartTime,
            RequestedEndTime = command.RequestedEndTime,
            ProposedSlotSummary = $"{service.Name} on {command.RequestedStartTime:ddd, MMM d} @ {command.RequestedStartTime:h:mm tt}",
            Status = BookingRequestStatus.PendingApproval,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.BookingRequests.Add(bookingRequest);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapToBookingRequestDto(bookingRequest);
    }

    public async Task<BookingRequestDto> ApproveBookingRequestAsync(
        Guid bookingRequestId,
        string? ownerNotes,
        CancellationToken cancellationToken = default)
    {
        var bookingRequest = await _dbContext.BookingRequests
            .Include(br => br.Lead)
            .Include(br => br.Service)
            .Include(br => br.Booking)
            .FirstOrDefaultAsync(br => br.Id == bookingRequestId, cancellationToken);

        if (bookingRequest == null) throw new ArgumentException("Booking request not found.");

        // Idempotency check: If already approved and booking exists
        if (bookingRequest.Status == BookingRequestStatus.Approved && bookingRequest.Booking != null)
        {
            // If email previously failed, auto-retry email without duplicating calendar event
            if (bookingRequest.Booking.EmailDeliveryStatus == "Failed")
            {
                _logger.LogInformation("Booking request {BookingRequestId} is already approved, but email failed previously. Retrying email dispatch...", bookingRequestId);
                return await RetryBookingEmailAsync(bookingRequestId, cancellationToken);
            }

            _logger.LogInformation("Booking request {BookingRequestId} is already approved and confirmed. Returning existing result idempotently.", bookingRequestId);
            return MapToBookingRequestDto(bookingRequest);
        }

        var business = await _dbContext.Businesses.FirstAsync(b => b.Id == bookingRequest.BusinessId, cancellationToken);

        // Step 1: Re-validate Google Calendar Availability prior to confirmation
        var busyIntervals = await _calendarService.GetFreeBusyIntervalsAsync(
            bookingRequest.BusinessId,
            bookingRequest.RequestedStartTime,
            bookingRequest.RequestedEndTime,
            cancellationToken);

        bool isConflict = busyIntervals.Any(b => bookingRequest.RequestedStartTime < b.EndTime && bookingRequest.RequestedEndTime > b.StartTime);
        if (isConflict)
        {
            bookingRequest.Status = BookingRequestStatus.Conflict;
            bookingRequest.OwnerNotes = "Slot conflict detected during calendar re-validation: requested slot overlaps with an existing calendar busy entry.";
            bookingRequest.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Requested slot is no longer available on the Google Calendar. Request status updated to Conflict.");
        }

        // Step 2: Create Google Calendar Event
        var title = $"{bookingRequest.Service?.Name ?? "Session"} - {bookingRequest.Lead?.Name}";
        var description = $"WorkPilot AI Booking\nCustomer: {bookingRequest.Lead?.Name}\nEmail: {bookingRequest.Lead?.Email}\nNotes: {ownerNotes}";

        string googleEventId;
        try
        {
            googleEventId = await _calendarService.CreateCalendarEventAsync(
                bookingRequest.BusinessId,
                title,
                description,
                bookingRequest.RequestedStartTime,
                bookingRequest.RequestedEndTime,
                bookingRequest.Lead?.Name ?? "Customer",
                bookingRequest.Lead?.Email ?? "",
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Google Calendar event for request {BookingRequestId}. Booking remains pending/unconfirmed.", bookingRequestId);
            throw new InvalidOperationException($"Google Calendar event creation failed: {ex.Message}");
        }

        // Step 3: Create or Update Booking Record with Calendar Event ID & Status
        if (bookingRequest.Booking == null)
        {
            var booking = new Booking
            {
                BookingRequestId = bookingRequest.Id,
                GoogleCalendarEventId = googleEventId,
                Status = BookingStatus.Confirmed,
                ConfirmedAt = DateTime.UtcNow,
                EmailDeliveryStatus = "NotAttempted"
            };
            _dbContext.Bookings.Add(booking);
            bookingRequest.Booking = booking;
        }
        else
        {
            bookingRequest.Booking.GoogleCalendarEventId = googleEventId;
            bookingRequest.Booking.Status = BookingStatus.Confirmed;
            bookingRequest.Booking.ConfirmedAt = DateTime.UtcNow;
        }

        bookingRequest.Status = BookingRequestStatus.Approved;
        bookingRequest.OwnerNotes = ownerNotes;
        bookingRequest.UpdatedAt = DateTime.UtcNow;

        if (bookingRequest.Lead != null)
        {
            bookingRequest.Lead.Status = "Converted";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Step 4: Dispatch Email via HTTPS Email API / Provider
        if (bookingRequest.Lead != null && !string.IsNullOrWhiteSpace(bookingRequest.Lead.Email))
        {
            var emailResult = await _emailService.SendBookingConfirmationEmailAsync(
                bookingRequest.Lead.Email,
                bookingRequest.Lead.Name,
                business.Name,
                bookingRequest.Service?.Name ?? "Training Session",
                bookingRequest.RequestedStartTime,
                bookingRequest.RequestedEndTime,
                business.Location,
                business.CancellationPolicy,
                cancellationToken);

            bookingRequest.Booking.EmailDeliveryStatus = emailResult.Status.ToString();
            bookingRequest.Booking.EmailDeliveryError = emailResult.ErrorMessage;
            bookingRequest.Booking.ConfirmationEmailSent = emailResult.Success;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapToBookingRequestDto(bookingRequest);
    }

    public async Task<BookingRequestDto> RetryBookingEmailAsync(
        Guid bookingRequestId,
        CancellationToken cancellationToken = default)
    {
        var bookingRequest = await _dbContext.BookingRequests
            .Include(br => br.Lead)
            .Include(br => br.Service)
            .Include(br => br.Booking)
            .FirstOrDefaultAsync(br => br.Id == bookingRequestId, cancellationToken);

        if (bookingRequest == null || bookingRequest.Booking == null)
        {
            throw new ArgumentException("Approved booking request not found for email retry.");
        }

        var business = await _dbContext.Businesses.FirstAsync(b => b.Id == bookingRequest.BusinessId, cancellationToken);

        if (bookingRequest.Lead != null && !string.IsNullOrWhiteSpace(bookingRequest.Lead.Email))
        {
            _logger.LogInformation("Retrying email dispatch for BookingRequest {BookingRequestId} (Event ID: {EventId})...", bookingRequestId, bookingRequest.Booking.GoogleCalendarEventId);

            var emailResult = await _emailService.SendBookingConfirmationEmailAsync(
                bookingRequest.Lead.Email,
                bookingRequest.Lead.Name,
                business.Name,
                bookingRequest.Service?.Name ?? "Training Session",
                bookingRequest.RequestedStartTime,
                bookingRequest.RequestedEndTime,
                business.Location,
                business.CancellationPolicy,
                cancellationToken);

            bookingRequest.Booking.EmailDeliveryStatus = emailResult.Status.ToString();
            bookingRequest.Booking.EmailDeliveryError = emailResult.ErrorMessage;
            bookingRequest.Booking.ConfirmationEmailSent = emailResult.Success;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapToBookingRequestDto(bookingRequest);
    }

    public async Task<BookingRequestDto> RejectBookingRequestAsync(
        Guid bookingRequestId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var bookingRequest = await _dbContext.BookingRequests
            .Include(br => br.Lead)
            .Include(br => br.Service)
            .FirstOrDefaultAsync(br => br.Id == bookingRequestId, cancellationToken);

        if (bookingRequest == null) throw new ArgumentException("Booking request not found.");

        bookingRequest.Status = BookingRequestStatus.Rejected;
        bookingRequest.OwnerNotes = reason;
        bookingRequest.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapToBookingRequestDto(bookingRequest);
    }

    public async Task<DashboardMetricsDto> GetDashboardMetricsAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        var totalLeads = await _dbContext.Leads.CountAsync(l => l.BusinessId == businessId, cancellationToken);
        var qualifiedLeads = await _dbContext.Leads.CountAsync(l => l.BusinessId == businessId && (l.Status == "Qualified" || l.Status == "Converted"), cancellationToken);
        var pendingRequests = await _dbContext.BookingRequests.CountAsync(br => br.BusinessId == businessId && br.Status == BookingRequestStatus.PendingApproval, cancellationToken);
        var confirmedBookings = await _dbContext.Bookings.CountAsync(b => b.BookingRequest!.BusinessId == businessId && b.Status == BookingStatus.Confirmed, cancellationToken);
        var totalAIInteractions = await _dbContext.AIInteractionLogs.CountAsync(a => a.BusinessId == businessId, cancellationToken);

        double conversionRate = totalLeads > 0 ? (double)confirmedBookings / totalLeads * 100.0 : 0.0;

        return new DashboardMetricsDto(
            totalLeads,
            qualifiedLeads,
            pendingRequests,
            confirmedBookings,
            Math.Round(conversionRate, 1),
            totalAIInteractions
        );
    }

    private static DateTime DetermineTargetDate(string? inputStr)
    {
        var now = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(inputStr))
        {
            return GetNextWeekday(now, DayOfWeek.Sunday);
        }

        var text = inputStr.ToLowerInvariant();

        var monthMatch = Regex.Match(text, @"\b(\d{1,2})(?:st|nd|rd|th)?\s*(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\w*\b");
        if (monthMatch.Success)
        {
            int day = int.Parse(monthMatch.Groups[1].Value);
            string monthStr = monthMatch.Groups[2].Value;
            int month = DateTime.ParseExact(monthStr, "MMM", CultureInfo.InvariantCulture).Month;
            int year = now.Year;
            var dt = new DateTime(year, month, day);
            if (dt < now.Date) dt = dt.AddYears(1);
            return dt;
        }

        var monthMatch2 = Regex.Match(text, @"\b(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\w*\s*(\d{1,2})(?:st|nd|rd|th)?\b");
        if (monthMatch2.Success)
        {
            string monthStr = monthMatch2.Groups[1].Value;
            int day = int.Parse(monthMatch2.Groups[2].Value);
            int month = DateTime.ParseExact(monthStr, "MMM", CultureInfo.InvariantCulture).Month;
            int year = now.Year;
            var dt = new DateTime(year, month, day);
            if (dt < now.Date) dt = dt.AddYears(1);
            return dt;
        }

        if (text.Contains("sunday")) return GetNextWeekday(now, DayOfWeek.Sunday);
        if (text.Contains("saturday")) return GetNextWeekday(now, DayOfWeek.Saturday);
        if (text.Contains("tomorrow")) return now.Date.AddDays(1);
        if (text.Contains("today")) return now.Date;
        if (text.Contains("monday")) return GetNextWeekday(now, DayOfWeek.Monday);
        if (text.Contains("tuesday")) return GetNextWeekday(now, DayOfWeek.Tuesday);
        if (text.Contains("wednesday")) return GetNextWeekday(now, DayOfWeek.Wednesday);
        if (text.Contains("thursday")) return GetNextWeekday(now, DayOfWeek.Thursday);
        if (text.Contains("friday")) return GetNextWeekday(now, DayOfWeek.Friday);

        var match = Regex.Match(text, @"\b(\d{1,2})[/.-](\d{1,2})[/.-](\d{2,4})\b");
        if (match.Success)
        {
            var datePart = match.Value;
            string[] formats = { "MM/dd/yyyy", "dd/MM/yyyy", "M/d/yyyy", "d/M/yyyy", "yyyy-MM-dd", "MM-dd-yyyy", "dd-MM-yyyy" };
            if (DateTime.TryParseExact(datePart, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                if (parsed.Year < 2000) parsed = parsed.AddYears(2000);
                return parsed.Date;
            }
        }

        if (DateTime.TryParse(inputStr, out var directParsed))
        {
            return directParsed.Date;
        }

        return GetNextWeekday(now, DayOfWeek.Sunday);
    }

    private static DateTime GetNextWeekday(DateTime start, DayOfWeek day)
    {
        int days = ((int)day - (int)start.DayOfWeek + 7) % 7;
        if (days == 0) days = 7;
        return start.Date.AddDays(days);
    }

    private static BookingRequestDto MapToBookingRequestDto(BookingRequest br)
    {
        return new BookingRequestDto(
            br.Id,
            br.BusinessId,
            br.LeadId,
            br.Lead?.Name ?? "Customer",
            br.Lead?.Email ?? "",
            br.Lead?.Phone,
            br.ServiceId,
            br.Service?.Name ?? "Service",
            br.Service?.Price ?? 0,
            br.Service?.DurationMinutes ?? 60,
            br.RequestedStartTime,
            br.RequestedEndTime,
            br.ProposedSlotSummary,
            br.Status,
            br.OwnerNotes,
            br.CreatedAt,
            br.Booking?.GoogleCalendarEventId,
            br.Booking?.EmailDeliveryStatus ?? "NotAttempted",
            br.Booking?.EmailDeliveryError
        );
    }
}
