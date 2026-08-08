using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WorkPilot.Application.DTOs;
using WorkPilot.Application.Orchestration;

namespace WorkPilot.Api.Controllers;

/// <summary>
/// Owner AI Business Operating System API.
/// Provides conversational AI access for the business owner.
/// 
/// Security: All actions are permission-checked (LOW/MEDIUM/HIGH risk gates).
/// AI never directly executes DB mutations — all mutations go through typed service methods.
/// </summary>
[ApiController]
[Route("api/owner")]
public class OwnerAIController : ControllerBase
{
    private readonly IAIBusinessOrchestrator _aiOrchestrator;
    private readonly ILogger<OwnerAIController> _logger;

    public OwnerAIController(
        IAIBusinessOrchestrator aiOrchestrator,
        ILogger<OwnerAIController> logger)
    {
        _aiOrchestrator = aiOrchestrator;
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/owner/{businessId}/chat
    // Main conversational AI endpoint — owner sends any natural language goal
    // ─────────────────────────────────────────────────────────────────────────

    [HttpPost("{businessId:guid}/chat")]
    public async Task<IActionResult> Chat(
        Guid businessId,
        [FromBody] OwnerChatRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Owner AI chat: BusinessId={BusinessId}", businessId);
            var response = await _aiOrchestrator.HandleOwnerChatAsync(businessId, request, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Owner chat — business not found: {BusinessId}", businessId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Owner chat error for business {BusinessId}", businessId);
            return Ok(new OwnerChatResponse(
                AssistantMessage: "I'm having a moment of trouble. Please try again in a few seconds.",
                ReasoningSummary: "Service error — fallback response",
                BusinessSnapshot: null,
                ActionPlan: null,
                Opportunities: [],
                AgentChain: [],
                RequiresApproval: false,
                ActionId: null
            ));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/owner/{businessId}/execute-action
    // Executes an approved AI action (owner has clicked "Approve")
    // Medium/High risk: This is only called after owner explicitly approves
    // ─────────────────────────────────────────────────────────────────────────

    [HttpPost("{businessId:guid}/execute-action")]
    public async Task<IActionResult> ExecuteAction(
        Guid businessId,
        [FromBody] ExecuteActionCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate that command targets the correct business (tenant isolation)
            var safeCommand = command with { BusinessId = businessId };
            var result = await _aiOrchestrator.ExecuteActionAsync(safeCommand, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            var detailedMsg = ex.InnerException != null ? $"{ex.Message} Inner: {ex.InnerException.Message}" : ex.Message;
            _logger.LogError(ex, "Execute action error for business {BusinessId}: {Details}", businessId, detailedMsg);
            return BadRequest(new { error = $"Action execution failed: {detailedMsg}" });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/owner/{businessId}/reject-action
    // Owner rejects a proposed AI action
    // ─────────────────────────────────────────────────────────────────────────

    [HttpPost("{businessId:guid}/reject-action/{actionId:guid}")]
    public async Task<IActionResult> RejectAction(
        Guid businessId,
        Guid actionId,
        [FromBody] RejectActionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _aiOrchestrator.RejectActionAsync(actionId, businessId, request.Reason ?? "Rejected by owner", cancellationToken);
            return Ok(new { success = true, message = "Action rejected." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reject action error: {ActionId}", actionId);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/owner/{businessId}/opportunities
    // Proactive AI morning brief — today's opportunities
    // ─────────────────────────────────────────────────────────────────────────

    [HttpGet("{businessId:guid}/opportunities")]
    public async Task<IActionResult> GetOpportunities(
        Guid businessId,
        CancellationToken cancellationToken)
    {
        try
        {
            var opportunities = await _aiOrchestrator.GetTodaysOpportunitiesAsync(businessId, cancellationToken);
            return Ok(opportunities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get opportunities error for business {BusinessId}", businessId);
            return Ok(Array.Empty<OpportunityCardDto>());
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/owner/{businessId}/ai-operations
    // AI Operations dashboard — audit trail of all agent actions
    // ─────────────────────────────────────────────────────────────────────────

    [HttpGet("{businessId:guid}/ai-operations")]
    public async Task<IActionResult> GetAIOperations(
        Guid businessId,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var log = await _aiOrchestrator.GetAIOperationsLogAsync(businessId, take, cancellationToken);
            return Ok(log);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get AI operations error for business {BusinessId}", businessId);
            return Ok(Array.Empty<AIAgentActionDto>());
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/owner/{businessId}/snapshot
    // Business context snapshot — revenue, customers, metrics
    // ─────────────────────────────────────────────────────────────────────────

    [HttpGet("{businessId:guid}/snapshot")]
    public async Task<IActionResult> GetSnapshot(
        Guid businessId,
        CancellationToken cancellationToken)
    {
        try
        {
            var snapshot = await _aiOrchestrator.GetBusinessSnapshotAsync(businessId, cancellationToken);
            return Ok(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get snapshot error for business {BusinessId}", businessId);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/owner/{businessId}/metrics/enhanced
    // Enhanced metrics with revenue and AI impact data
    // ─────────────────────────────────────────────────────────────────────────

    [HttpGet("{businessId:guid}/metrics/enhanced")]
    public async Task<IActionResult> GetEnhancedMetrics(
        Guid businessId,
        CancellationToken cancellationToken)
    {
        try
        {
            var metrics = await _aiOrchestrator.GetEnhancedMetricsAsync(businessId, cancellationToken);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get enhanced metrics error for {BusinessId}", businessId);
            return BadRequest(new { error = ex.Message });
        }
    }
}

public record RejectActionRequest(string? Reason);
