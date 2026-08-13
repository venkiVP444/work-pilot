using System;
using System.Threading;
using System.Threading.Tasks;
using WorkPilot.Application.Tools.Bookings;

namespace WorkPilot.Application.Agents;

public class BookingAgent : IBookingAgent
{
    private readonly ICreateBookingRequestTool _createBookingRequestTool;

    public BookingAgent(ICreateBookingRequestTool createBookingRequestTool)
    {
        _createBookingRequestTool = createBookingRequestTool;
    }

    public async Task<BookingOutput> HandleBookingTaskAsync(BookingInput input, CancellationToken cancellationToken = default)
    {
        var toolInput = new BookingRequestCommandInput(
            BusinessId: input.BusinessId,
            ConversationId: input.ConversationId,
            ServiceId: input.ServiceId,
            RequestedStartTime: input.StartTime,
            RequestedEndTime: input.EndTime,
            CustomerName: input.CustomerName,
            CustomerEmail: input.CustomerEmail,
            CustomerPhone: input.CustomerPhone
        );

        var request = await _createBookingRequestTool.ExecuteAsync(toolInput, cancellationToken);

        return new BookingOutput(
            BookingRequestId: request.Id,
            ProposedSlotSummary: request.ProposedSlotSummary,
            Status: request.Status.ToString()
        );
    }
}
