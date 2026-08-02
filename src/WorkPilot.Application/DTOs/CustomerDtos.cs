using System;
using System.Collections.Generic;

namespace WorkPilot.Application.DTOs;

public record CustomerChatMessageRequest(
    string CustomerMessage,
    Guid? ConversationId
);

public record CustomerChatMessageResponse(
    Guid ConversationId,
    Guid BusinessId,
    string AssistantMessage,
    List<CalendarSlotDto> ProposedSlots,
    List<string> MissingInformation,
    string Intent,
    string Decision,
    Guid? MatchedServiceId
);

public record CreateBookingRequestCommand(
    Guid BusinessId,
    Guid ConversationId,
    Guid ServiceId,
    DateTime RequestedStartTime,
    DateTime RequestedEndTime,
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone
);
