using System;
using System.Collections.Generic;
using WorkPilot.Domain.Enums;

namespace WorkPilot.Application.DTOs;

public record GeminiAgentRequest(
    Guid BusinessId,
    Guid? ConversationId,
    string CustomerMessage,
    List<ServiceDto> AvailableServices
);

public record GeminiStructuredResponse(
    IntentType Intent,
    DecisionType Decision,
    string? SelectedServiceName,
    Guid? ServiceId,
    string? DatePreference,
    string? TimePreference,
    List<string> MissingInformation,
    string AssistantMessage,
    string ReasoningSummary
);
