using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WorkPilot.Application.Services;

namespace WorkPilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly BookingOrchestratorService _orchestrator;

    public MetricsController(BookingOrchestratorService orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [HttpGet("{businessId:guid}")]
    public async Task<IActionResult> GetMetrics(Guid businessId, CancellationToken cancellationToken)
    {
        var metrics = await _orchestrator.GetDashboardMetricsAsync(businessId, cancellationToken);
        return Ok(metrics);
    }
}
