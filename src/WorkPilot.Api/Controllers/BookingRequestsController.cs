using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkPilot.Application.Common.Interfaces;
using WorkPilot.Application.DTOs;
using WorkPilot.Application.Services;
using WorkPilot.Domain.Enums;

namespace WorkPilot.Api.Controllers;

[ApiController]
[Route("api/booking-requests")]
public class BookingRequestsController : ControllerBase
{
    private readonly BookingOrchestratorService _orchestrator;
    private readonly IWorkPilotDbContext _dbContext;
    private readonly ILogger<BookingRequestsController> _logger;

    public BookingRequestsController(
        BookingOrchestratorService orchestrator,
        IWorkPilotDbContext dbContext,
        ILogger<BookingRequestsController> logger)
    {
        _orchestrator = orchestrator;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingRequests([FromQuery] Guid businessId, CancellationToken cancellationToken)
    {
        try
        {
            var defaultBusinessId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var targetBusinessId = businessId != Guid.Empty ? businessId : defaultBusinessId;

            var entities = await _dbContext.BookingRequests
                .Include(br => br.Lead)
                .Include(br => br.Service)
                .Include(br => br.Booking)
                .Where(br => (br.BusinessId == targetBusinessId || businessId == Guid.Empty) && br.Status == BookingRequestStatus.PendingApproval)
                .OrderByDescending(br => br.CreatedAt)
                .ToListAsync(cancellationToken);

            var dtos = entities.Select(br => new BookingRequestDto(
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
            )).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending booking requests for business {BusinessId}: {ErrorMessage}", businessId, ex.Message);
            return Ok(new List<BookingRequestDto>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllRequests([FromQuery] Guid businessId, CancellationToken cancellationToken)
    {
        try
        {
            var defaultBusinessId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var targetBusinessId = businessId != Guid.Empty ? businessId : defaultBusinessId;

            var entities = await _dbContext.BookingRequests
                .Include(br => br.Lead)
                .Include(br => br.Service)
                .Include(br => br.Booking)
                .Where(br => br.BusinessId == targetBusinessId || businessId == Guid.Empty)
                .OrderByDescending(br => br.CreatedAt)
                .ToListAsync(cancellationToken);

            var dtos = entities.Select(br => new BookingRequestDto(
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
            )).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all booking requests for business {BusinessId}: {ErrorMessage}", businessId, ex.Message);
            return Ok(new List<BookingRequestDto>());
        }
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ApproveBookingRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _orchestrator.ApproveBookingRequestAsync(id, dto?.Notes, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving booking request {BookingRequestId}: {ErrorMessage}", id, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/retry-email")]
    public async Task<IActionResult> RetryEmail(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _orchestrator.RetryBookingEmailAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrying email for booking request {BookingRequestId}: {ErrorMessage}", id, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectBookingRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _orchestrator.RejectBookingRequestAsync(id, dto.Reason, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting booking request {BookingRequestId}: {ErrorMessage}", id, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }
}
