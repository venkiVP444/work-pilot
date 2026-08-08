using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WorkPilot.Application.Common.Interfaces;
using WorkPilot.Application.DTOs;
using WorkPilot.Application.Services;
using WorkPilot.Domain.Entities;

namespace WorkPilot.Application.Tools.Bookings;

public class CreateBookingRequestTool : ICreateBookingRequestTool
{
    private readonly BookingOrchestratorService _bookingOrchestrator;
    private readonly IWorkPilotDbContext _dbContext;

    public CreateBookingRequestTool(BookingOrchestratorService bookingOrchestrator, IWorkPilotDbContext dbContext)
    {
        _bookingOrchestrator = bookingOrchestrator;
        _dbContext = dbContext;
    }

    public async Task<BookingRequest> ExecuteAsync(BookingRequestCommandInput input, CancellationToken cancellationToken = default)
    {
        var command = new CreateBookingRequestCommand(
            BusinessId: input.BusinessId,
            ConversationId: input.ConversationId ?? Guid.Empty,
            ServiceId: input.ServiceId,
            RequestedStartTime: input.RequestedStartTime,
            RequestedEndTime: input.RequestedEndTime,
            CustomerName: input.CustomerName,
            CustomerEmail: input.CustomerEmail,
            CustomerPhone: input.CustomerPhone
        );

        var dto = await _bookingOrchestrator.CreateBookingRequestAsync(command, cancellationToken);

        // Fetch and return the actual entity
        return await _dbContext.BookingRequests
            .Include(br => br.Lead)
            .Include(br => br.Service)
            .FirstAsync(br => br.Id == dto.Id, cancellationToken);
    }
}
