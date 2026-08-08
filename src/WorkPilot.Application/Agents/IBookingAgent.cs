using System;
using System.Threading;
using System.Threading.Tasks;
using WorkPilot.Domain.Entities;

namespace WorkPilot.Application.Agents;

public record BookingInput(
    Guid BusinessId,
    Guid? ConversationId,
    Guid ServiceId,
    DateTime StartTime,
    DateTime EndTime,
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone
);

public record BookingOutput(
    Guid BookingRequestId,
    string ProposedSlotSummary,
    string Status
);

public interface IBookingAgent
{
    Task<BookingOutput> HandleBookingTaskAsync(BookingInput input, CancellationToken cancellationToken = default);
}
