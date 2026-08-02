using System;
using WorkPilot.Domain.Enums;

namespace WorkPilot.Domain.Entities;

public class AIInteractionLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BusinessId { get; set; }
    public Guid? ConversationId { get; set; }
    public string Model { get; set; } = "gemini-1.5-flash";
    public IntentType DetectedIntent { get; set; }
    public DecisionType DecisionMade { get; set; }
    public string InputSummary { get; set; } = string.Empty;
    public string StructuredOutputJson { get; set; } = string.Empty;
    public double LatencyMs { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Business? Business { get; set; }
    public Conversation? Conversation { get; set; }
}
