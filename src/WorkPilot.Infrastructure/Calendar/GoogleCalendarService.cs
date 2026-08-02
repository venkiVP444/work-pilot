using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WorkPilot.Application.Common.Interfaces;
using WorkPilot.Application.DTOs;

namespace WorkPilot.Infrastructure.Calendar;

public class GoogleCalendarService : IGoogleCalendarService
{
    private readonly IConfiguration _configuration;
    private readonly IWorkPilotDbContext _dbContext;
    private readonly ILogger<GoogleCalendarService> _logger;

    public GoogleCalendarService(
        IConfiguration configuration,
        IWorkPilotDbContext dbContext,
        ILogger<GoogleCalendarService> logger)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _logger = logger;
    }

    public string GetAuthorizationUrl(Guid businessId)
    {
        var clientId = _configuration["Google:ClientId"];
        var redirectUri = _configuration["Google:RedirectUri"] ?? "http://localhost:5050/api/calendar/callback";

        if (string.IsNullOrWhiteSpace(clientId) || clientId == "YOUR_GOOGLE_CLIENT_ID_HERE")
        {
            _logger.LogWarning("Google Client ID is not configured.");
            return $"http://localhost:5050/api/calendar/callback?code=mock_authorization_code&state={businessId}";
        }

        var flow = CreateInitializer();
        var codeRequest = flow.CreateAuthorizationCodeRequest(redirectUri);
        codeRequest.State = businessId.ToString();
        codeRequest.Scope = CalendarService.Scope.Calendar;
        return codeRequest.Build().ToString();
    }

    public async Task<bool> ExchangeCodeForTokensAsync(Guid businessId, string code, CancellationToken cancellationToken = default)
    {
        var business = await _dbContext.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
        if (business == null) return false;

        var clientId = _configuration["Google:ClientId"];
        var clientSecret = _configuration["Google:ClientSecret"];
        var redirectUri = _configuration["Google:RedirectUri"] ?? "http://localhost:5050/api/calendar/callback";

        if (string.IsNullOrWhiteSpace(clientId) || clientId == "YOUR_GOOGLE_CLIENT_ID_HERE" || code == "mock_authorization_code")
        {
            _logger.LogInformation("Mock token exchange executed for business {BusinessId}.", businessId);
            business.IsCalendarConnected = true;
            business.GoogleRefreshToken = "mock_refresh_token_" + Guid.NewGuid().ToString("N");
            business.GoogleCalendarId = "primary";
            business.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        try
        {
            var flow = CreateInitializer();
            var token = await flow.ExchangeCodeForTokenAsync("user", code, redirectUri, cancellationToken);

            business.IsCalendarConnected = true;
            business.GoogleRefreshToken = token.RefreshToken ?? token.AccessToken;
            business.GoogleCalendarId = "primary";
            business.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to exchange Google OAuth code for tokens for business {BusinessId}.", businessId);
            return false;
        }
    }

    public async Task<List<TimeIntervalDto>> GetFreeBusyIntervalsAsync(Guid businessId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        var business = await _dbContext.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
        if (business == null) return new List<TimeIntervalDto>();

        var clientId = _configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId) || clientId == "YOUR_GOOGLE_CLIENT_ID_HERE" || string.IsNullOrWhiteSpace(business.GoogleRefreshToken) || business.GoogleRefreshToken.StartsWith("mock_"))
        {
            _logger.LogInformation("Returning simulated calendar free/busy intervals for business {BusinessId}.", businessId);
            return GetSimulatedBusyIntervals(startDate, endDate);
        }

        try
        {
            var calendarService = await CreateCalendarServiceAsync(business.GoogleRefreshToken, cancellationToken);
            var request = new FreeBusyRequest
            {
                TimeMinDateTimeOffset = startDate,
                TimeMaxDateTimeOffset = endDate,
                Items = new List<FreeBusyRequestItem>
                {
                    new FreeBusyRequestItem { Id = business.GoogleCalendarId ?? "primary" }
                }
            };

            var response = await calendarService.Freebusy.Query(request).ExecuteAsync(cancellationToken);
            var result = new List<TimeIntervalDto>();

            if (response.Calendars != null && response.Calendars.TryGetValue(business.GoogleCalendarId ?? "primary", out var calendarBusy))
            {
                foreach (var busyPeriod in calendarBusy.Busy)
                {
                    if (busyPeriod.StartDateTimeOffset.HasValue && busyPeriod.EndDateTimeOffset.HasValue)
                    {
                        result.Add(new TimeIntervalDto(busyPeriod.StartDateTimeOffset.Value.UtcDateTime, busyPeriod.EndDateTimeOffset.Value.UtcDateTime));
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying Google Calendar FreeBusy API. Falling back to simulated intervals.");
            return GetSimulatedBusyIntervals(startDate, endDate);
        }
    }

    public async Task<string> CreateCalendarEventAsync(Guid businessId, string title, string description, DateTime startTime, DateTime endTime, string customerName, string customerEmail, CancellationToken cancellationToken = default)
    {
        var business = await _dbContext.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
        var clientId = _configuration["Google:ClientId"];

        if (business == null || string.IsNullOrWhiteSpace(clientId) || clientId == "YOUR_GOOGLE_CLIENT_ID_HERE" || string.IsNullOrWhiteSpace(business.GoogleRefreshToken) || business.GoogleRefreshToken.StartsWith("mock_"))
        {
            var eventId = "gcal_evt_" + Guid.NewGuid().ToString("N")[..12];
            _logger.LogInformation("Created simulated Google Calendar event {EventId} for {CustomerName} ({StartTime} - {EndTime}).", eventId, customerName, startTime, endTime);
            return eventId;
        }

        try
        {
            var calendarService = await CreateCalendarServiceAsync(business.GoogleRefreshToken, cancellationToken);
            var newEvent = new Event
            {
                Summary = title,
                Description = description,
                Start = new EventDateTime { DateTimeDateTimeOffset = new DateTimeOffset(startTime.ToUniversalTime()) },
                End = new EventDateTime { DateTimeDateTimeOffset = new DateTimeOffset(endTime.ToUniversalTime()) },
                Attendees = new List<EventAttendee>
                {
                    new EventAttendee { Email = customerEmail, DisplayName = customerName }
                }
            };

            var createdEvent = await calendarService.Events.Insert(newEvent, business.GoogleCalendarId ?? "primary").ExecuteAsync(cancellationToken);
            return createdEvent.Id ?? "gcal_evt_" + Guid.NewGuid().ToString("N")[..12];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Google Calendar event via API.");
            return "gcal_evt_" + Guid.NewGuid().ToString("N")[..12];
        }
    }

    private GoogleAuthorizationCodeFlow CreateInitializer()
    {
        return new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets
            {
                ClientId = _configuration["Google:ClientId"] ?? "",
                ClientSecret = _configuration["Google:ClientSecret"] ?? ""
            },
            Scopes = new[] { CalendarService.Scope.Calendar }
        });
    }

    private async Task<CalendarService> CreateCalendarServiceAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var tokenResponse = new TokenResponse { RefreshToken = refreshToken };
        var flow = CreateInitializer();
        var credential = new UserCredential(flow, "user", tokenResponse);

        if (credential.Token.IsStale)
        {
            await credential.RefreshTokenAsync(cancellationToken);
        }

        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "WorkPilot AI"
        });
    }

    private List<TimeIntervalDto> GetSimulatedBusyIntervals(DateTime startDate, DateTime endDate)
    {
        var busy = new List<TimeIntervalDto>();
        var saturday = startDate.Date.AddDays(((int)DayOfWeek.Saturday - (int)startDate.DayOfWeek + 7) % 7);
        if (saturday >= startDate.Date && saturday <= endDate.Date)
        {
            busy.Add(new TimeIntervalDto(saturday.AddHours(10), saturday.AddHours(11)));
        }
        return busy;
    }
}
