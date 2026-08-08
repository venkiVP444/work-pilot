using System;
using System.Threading;
using System.Threading.Tasks;
using WorkPilot.Domain.Entities;

namespace WorkPilot.Application.Tools.Bookings;

public record BookingRequestCommandInput(
    Guid BusinessId,
    Guid? ConversationId,
    Guid ServiceId,
    DateTime RequestedStartTime,
    DateTime RequestedEndTime,
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone
);

public interface ICreateBookingRequestTool
{
    Task<BookingRequest> ExecuteAsync(BookingRequestCommandInput input, CancellationToken cancellationToken = default);
}
