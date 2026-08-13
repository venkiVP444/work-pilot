using System;
using WorkPilot.Domain.Enums;

namespace WorkPilot.Domain.Entities;

/// <summary>
/// A marketing campaign targeting a customer segment.
/// Created by the Marketing Agent, executed after owner approval.
/// </summary>
public class Campaign
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public Guid? AIAgentActionId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string TargetSegment { get; set; } = string.Empty; // e.g. "Inactive 60+ days"
    public int TargetCustomerCount { get; set; } = 0;

    // AI-generated content
    public string SubjectLine { get; set; } = string.Empty;
    public string EmailBody { get; set; } = string.Empty;
    public string OfferDescription { get; set; } = string.Empty;

    // Execution state
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }

    // Results
    public int EmailsSent { get; set; } = 0;
    public int EmailsFailed { get; set; } = 0;
    public int BookingRequestsGenerated { get; set; } = 0;
    public int BookingsConfirmed { get; set; } = 0;
    public decimal RevenueGenerated { get; set; } = 0;
    public decimal CampaignCost { get; set; } = 0;

    // Navigation
    public Business? Business { get; set; }
    public AIAgentAction? AIAgentAction { get; set; }
}
