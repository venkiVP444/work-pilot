using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkPilot.Application.DTOs;

namespace WorkPilot.Application.Common.Interfaces;

public interface IGoogleCalendarService
{
    string GetAuthorizationUrl(Guid businessId);
    Task<bool> ExchangeCodeForTokensAsync(Guid businessId, string code, CancellationToken cancellationToken = default);
    Task<List<TimeIntervalDto>> GetFreeBusyIntervalsAsync(Guid businessId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<string> CreateCalendarEventAsync(Guid businessId, string title, string description, DateTime startTime, DateTime endTime, string customerName, string customerEmail, CancellationToken cancellationToken = default);
}
