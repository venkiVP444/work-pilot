using System;
using System.Collections.Generic;
using WorkPilot.Domain.Enums;

namespace WorkPilot.Domain.Entities;

/// <summary>
/// Records every AI-proposed and AI-executed action for full audit trail.
/// Supports human-in-the-loop approval gates and business impact measurement.
/// </summary>
public class AIAgentAction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }

    // Agent that proposed/executed this action
    public AgentType AgentType { get; set; }
    public ActionType ActionType { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public ActionStatus Status { get; set; } = ActionStatus.Proposed;

    // Owner's original intent that triggered this action
    public string OwnerIntent { get; set; } = string.Empty;

    // AI's reasoning for proposing this action
    public string ReasoningSummary { get; set; } = string.Empty;

    // Agent execution chain (e.g., "Orchestrator → BusinessAnalyst → CustomerGrowth")
    public string AgentChain { get; set; } = string.Empty;

    // Estimated impact (before execution)
    public string EstimatedImpact { get; set; } = string.Empty;
    public decimal EstimatedRevenue { get; set; } = 0;
    public int EstimatedBookings { get; set; } = 0;

    // Actual outcomes (after execution)
    public string? ActualOutcome { get; set; }
    public decimal ActualRevenue { get; set; } = 0;
    public int ActualBookings { get; set; } = 0;
    public int TargetCustomerCount { get; set; } = 0;

    // Execution details
    public string? OwnerNotes { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Linked campaign (if action created one)
    public Guid? CampaignId { get; set; }
    public Campaign? Campaign { get; set; }

    // Navigation
    public Business? Business { get; set; }
}
