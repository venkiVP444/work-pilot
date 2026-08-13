namespace WorkPilot.Domain.Enums;

public enum BookingRequestStatus
{
    PendingApproval = 0,
    Approved = 1,
    Rejected = 2,
    Conflict = 3,
    Expired = 4
}

public enum BookingStatus
{
    Confirmed = 0,
    Cancelled = 1
}

public enum IntentType
{
    BookingRequest = 0,
    GeneralQuestion = 1,
    Unsupported = 2,
    Unknown = 3
}

public enum DecisionType
{
    AskClarification = 0,
    ProposeSlots = 1,
    CreateBookingRequest = 2,
    EscalateToOwner = 3,
    Reject = 4
}

public enum MessageRole
{
    Customer = 0,
    Assistant = 1,
    System = 2
}

// AI Business OS — Agent + Action enums

public enum AgentType
{
    Orchestrator = 0,
    BusinessAnalyst = 1,
    CustomerGrowth = 2,
    Marketing = 3,
    Booking = 4,
    RevenueOptimization = 5,
    Operations = 6,
    CampaignExperiment = 7
}

public enum ActionType
{
    AnalyzeBusiness = 0,
    IdentifyInactiveCustomers = 1,
    CreateCampaign = 2,
    SendCampaign = 3,
    FillEmptySlots = 4,
    AnalyzeRevenue = 5,
    OptimizePricing = 6,
    GenerateOffer = 7,
    ScheduleFollowUp = 8,
    BookSlot = 9,
    GenerateReport = 10
}

public enum RiskLevel
{
    Low = 0,      // Auto-execute: analytics, drafts, internal
    Medium = 1,   // Require approval: campaigns, discounts, batch comms
    High = 2      // Always confirm: financial, refunds, irreversible actions
}

public enum ActionStatus
{
    Proposed = 0,
    AwaitingApproval = 1,
    Approved = 2,
    Rejected = 3,
    Executing = 4,
    Completed = 5,
    Failed = 6,
    Cancelled = 7
}

public enum CampaignStatus
{
    Draft = 0,
    Scheduled = 1,
    Sending = 2,
    Sent = 3,
    Failed = 4,
    Cancelled = 5
}

