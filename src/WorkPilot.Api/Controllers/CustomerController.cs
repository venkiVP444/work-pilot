using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkPilot.Application.DTOs;
using WorkPilot.Application.Services;

namespace WorkPilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly BookingOrchestratorService _orchestrator;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(BookingOrchestratorService orchestrator, ILogger<CustomerController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    [HttpPost("{businessId:guid}/conversation/message")]
    public async Task<IActionResult> SendMessage(Guid businessId, [FromBody] CustomerChatMessageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _orchestrator.HandleCustomerMessageAsync(businessId, request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing customer chat message for business {BusinessId}: {ErrorMessage}", businessId, ex.Message);
            
            // Return friendly fallback response instead of 500 error
            return Ok(new CustomerChatMessageResponse(
                ConversationId: request.ConversationId ?? Guid.NewGuid(),
                BusinessId: businessId,
                AssistantMessage: "I'd be glad to help you schedule your training session! Please select your preferred day (e.g. Tuesday, Wednesday, Thursday, Saturday, Sunday) and time.",
                ProposedSlots: new List<CalendarSlotDto>(),
                MissingInformation: new List<string> { "preferred date", "preferred time" },
                Intent: "BookingRequest",
                Decision: "AskClarification",
                MatchedServiceId: null
            ));
        }
    }

    [HttpPost("{businessId:guid}/booking-request")]
    public async Task<IActionResult> CreateBookingRequest(Guid businessId, [FromBody] CreateBookingRequestCommand command, CancellationToken cancellationToken)
    {
        try
        {
            if (command.BusinessId != businessId)
            {
                command = command with { BusinessId = businessId };
            }

            var result = await _orchestrator.CreateBookingRequestAsync(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating booking request for business {BusinessId}: {ErrorMessage}", businessId, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }
}
