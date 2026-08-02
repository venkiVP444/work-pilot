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
