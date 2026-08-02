using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WorkPilot.Application.Common.Interfaces;

namespace WorkPilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CalendarController : ControllerBase
{
    private readonly IGoogleCalendarService _calendarService;
    private readonly IWorkPilotDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public CalendarController(IGoogleCalendarService calendarService, IWorkPilotDbContext dbContext, IConfiguration configuration)
    {
        _calendarService = calendarService;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    [HttpGet("connect")]
    public IActionResult Connect([FromQuery] Guid businessId)
    {
        var authUrl = _calendarService.GetAuthorizationUrl(businessId);
        return Ok(new { authorizationUrl = authUrl });
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(state, out var businessId))
        {
            var success = await _calendarService.ExchangeCodeForTokensAsync(businessId, code, cancellationToken);
            if (success)
            {
                // Detect frontend origin dynamically (e.g. localhost:4300 or 4200)
                var referer = Request.Headers["Referer"].ToString();
                string targetHost = "http://localhost:4300";
                if (!string.IsNullOrWhiteSpace(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
                {
                    targetHost = $"{refererUri.Scheme}://{refererUri.Authority}";
                }

                return Redirect($"{targetHost}/dashboard?calendarConnected=true");
            }
        }
        return BadRequest(new { error = "Invalid OAuth callback state or code." });
    }

    [HttpGet("status/{businessId:guid}")]
    public async Task<IActionResult> GetStatus(Guid businessId, CancellationToken cancellationToken)
    {
        var b = await _dbContext.Businesses.FirstOrDefaultAsync(x => x.Id == businessId, cancellationToken);
        if (b == null) return NotFound();

        return Ok(new
        {
            isConnected = b.IsCalendarConnected,
            calendarId = b.GoogleCalendarId
        });
    }
}
