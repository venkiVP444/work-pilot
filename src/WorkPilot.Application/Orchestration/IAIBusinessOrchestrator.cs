using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WorkPilot.Application.DTOs;

namespace WorkPilot.Application.Orchestration;

public interface IAIBusinessOrchestrator
{
    Task<OwnerChatResponse> HandleOwnerChatAsync(Guid businessId, OwnerChatRequest request, CancellationToken cancellationToken = default);
    Task<ExecuteActionResult> ExecuteActionAsync(ExecuteActionCommand command, CancellationToken cancellationToken = default);
    Task RejectActionAsync(Guid actionId, Guid businessId, string reason, CancellationToken cancellationToken = default);
    Task<List<OpportunityCardDto>> GetTodaysOpportunitiesAsync(Guid businessId, CancellationToken cancellationToken = default);
    Task<List<AIAgentActionDto>> GetAIOperationsLogAsync(Guid businessId, int take = 20, CancellationToken cancellationToken = default);
    Task<BusinessSnapshotDto> GetBusinessSnapshotAsync(Guid businessId, CancellationToken cancellationToken = default);
    Task<EnhancedMetricsDto> GetEnhancedMetricsAsync(Guid businessId, CancellationToken cancellationToken = default);
}
