using System;
using System.Collections.Generic;
using WorkPilot.Domain.Enums;

namespace WorkPilot.Application.DTOs;

// ─── Owner Chat ────────────────────────────────────────────────────────────────

public record OwnerChatRequest(
    string Message,
    Guid? LastActionId = null
);

public record OwnerChatResponse(
    string AssistantMessage,
    string ReasoningSummary,
    BusinessSnapshotDto? BusinessSnapshot,
    AIActionPlanDto? ActionPlan,
    List<OpportunityCardDto> Opportunities,
    List<AIAgentStepDto> AgentChain,
    bool RequiresApproval,
    Guid? ActionId
);

// ─── Business Snapshot (context fed to Gemini) ────────────────────────────────

public record BusinessSnapshotDto(
    string BusinessName,
    int TotalCustomers,
    int ActiveCustomers,
    int InactiveCustomers30Days,
    int InactiveCustomers60Days,
    int InactiveCustomers90Plus,
    decimal RevenueThisMonth,
    decimal RevenueLastMonth,
    int BookingsThisMonth,
    int BookingsLastMonth,
    int PendingBookingRequests,
    int EmptySlotsThisWeek,
    decimal AverageOrderValue,
    int TotalConfirmedBookings,
    decimal TotalRevenue,
    List<string> TopServices
);

// ─── AI Action Plan ────────────────────────────────────────────────────────────

public record AIActionPlanDto(
    Guid ActionId,
    string ActionType,
    string AgentType,
    string RiskLevel,
    string Title,
    string Description,
    string EstimatedImpact,
    decimal EstimatedRevenue,
    int EstimatedBookings,
    int TargetCustomerCount,
    string WhatWillHappen,
    string WhyRecommended,
    decimal EstimatedCost,
    ActionStatus Status,
    DateTime CreatedAt
);

// ─── Execute Action ────────────────────────────────────────────────────────────

public record ExecuteActionCommand(
    Guid BusinessId,
    Guid ActionId,
    string? OwnerNotes = null
);

public record ExecuteActionResult(
    Guid ActionId,
    bool Success,
    string Message,
    int CustomersReached,
    int BookingRequestsGenerated,
    decimal RevenueImpact,
    string? FailureReason,
    List<AIAgentStepDto> ExecutionSteps
);

// ─── Opportunity Cards (proactive morning brief) ───────────────────────────────

public record OpportunityCardDto(
    string Title,
    string Description,
    string EstimatedRevenue,
    string ActionLabel,
    string ActionType,
    int AffectedCustomers,
    string Priority,      // high / medium / low
    string Icon
);

// ─── AI Agent Step (for Operations dashboard / audit trail) ───────────────────

public record AIAgentStepDto(
    string Agent,
    string Action,
    string Result,
    DateTime Timestamp,
    bool Success
);

// ─── Owner Intent → Gemini ────────────────────────────────────────────────────

public record OwnerIntentRequest(
    Guid BusinessId,
    string OwnerMessage,
    BusinessSnapshotDto BusinessSnapshot
);

public record OwnerIntentResponse(
    string[] ActiveAgents,
    string ReasoningSummary,
    string AssistantMessage,
    string RecommendedActionType,   // maps to ActionType enum string
    string RiskLevel,               // Low / Medium / High
    string EstimatedImpact,
    decimal EstimatedRevenue,
    int EstimatedBookings,
    int TargetCustomerCount,
    string WhatWillHappen,
    string WhyRecommended,
    string CampaignSubjectLine,
    string CampaignEmailBody,
    string CampaignOfferDescription,
    string TargetSegment            // "Inactive 60+ days", "Empty slots", etc.
);

// ─── AI Operations Log ────────────────────────────────────────────────────────

public record AIAgentActionDto(
    Guid Id,
    string AgentType,
    string ActionType,
    string RiskLevel,
    string Status,
    string OwnerIntent,
    string ReasoningSummary,
    string AgentChain,
    string EstimatedImpact,
    decimal EstimatedRevenue,
    int EstimatedBookings,
    string? ActualOutcome,
    decimal ActualRevenue,
    int ActualBookings,
    int TargetCustomerCount,
    string? FailureReason,
    DateTime CreatedAt,
    DateTime? ExecutedAt,
    DateTime? CompletedAt
);

// ─── Revenue / Business Metrics (enhanced) ────────────────────────────────────

public record EnhancedMetricsDto(
    int TotalCustomers,
    int ActiveCustomers,
    int InactiveCustomers,
    int TotalLeads,
    int QualifiedLeads,
    int PendingBookingRequests,
    int ConfirmedBookings,
    double ConversionRatePercentage,
    int TotalAIInteractions,
    decimal RevenueThisMonth,
    decimal RevenueLastMonth,
    decimal RevenueGrowthPercent,
    decimal TotalRevenue,
    decimal AverageOrderValue,
    int BookingsThisMonth,
    int BookingsLastMonth,
    int TotalCampaignsSent,
    int TotalCampaignBookings,
    decimal TotalCampaignRevenue,
    int AIActionsExecuted,
    decimal AIInfluencedRevenue
);
