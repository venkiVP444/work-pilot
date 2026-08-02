using System.Threading;
using System.Threading.Tasks;
using WorkPilot.Application.DTOs;

namespace WorkPilot.Application.Common.Interfaces;

public interface IGeminiAgentService
{
    Task<GeminiStructuredResponse> ProcessCustomerMessageAsync(GeminiAgentRequest request, CancellationToken cancellationToken = default);
}
