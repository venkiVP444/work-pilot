using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkPilot.Application.Common.Interfaces;
using WorkPilot.Application.DTOs;
using WorkPilot.Domain.Entities;

namespace WorkPilot.Api.Controllers;

[ApiController]
[Route("api")]
public class AvailabilityController : ControllerBase
{
    private readonly IWorkPilotDbContext _dbContext;

    public AvailabilityController(IWorkPilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("businesses/{businessId:guid}/availability")]
    public async Task<IActionResult> GetAvailability(Guid businessId, CancellationToken cancellationToken)
    {
        var rules = await _dbContext.AvailabilityRules
            .Where(r => r.BusinessId == businessId)
            .Select(r => new AvailabilityRuleDto(r.Id, r.BusinessId, r.DayOfWeek, r.StartTime, r.EndTime, r.BufferMinutes, r.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(rules);
    }

    [HttpPost("businesses/{businessId:guid}/availability")]
    public async Task<IActionResult> CreateAvailabilityRule(Guid businessId, [FromBody] CreateAvailabilityRuleDto dto, CancellationToken cancellationToken)
    {
        var rule = new AvailabilityRule
        {
            BusinessId = businessId,
            DayOfWeek = dto.DayOfWeek,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            BufferMinutes = dto.BufferMinutes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.AvailabilityRules.Add(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new AvailabilityRuleDto(rule.Id, rule.BusinessId, rule.DayOfWeek, rule.StartTime, rule.EndTime, rule.BufferMinutes, rule.IsActive));
    }

    [HttpPut("availability/{id:guid}")]
    public async Task<IActionResult> UpdateAvailabilityRule(Guid id, [FromBody] UpdateAvailabilityRuleDto dto, CancellationToken cancellationToken)
    {
        var rule = await _dbContext.AvailabilityRules.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (rule == null) return NotFound(new { error = "Availability rule not found." });

        rule.DayOfWeek = dto.DayOfWeek;
        rule.StartTime = dto.StartTime;
        rule.EndTime = dto.EndTime;
        rule.BufferMinutes = dto.BufferMinutes;
        rule.IsActive = dto.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new AvailabilityRuleDto(rule.Id, rule.BusinessId, rule.DayOfWeek, rule.StartTime, rule.EndTime, rule.BufferMinutes, rule.IsActive));
    }

    [HttpDelete("availability/{id:guid}")]
    public async Task<IActionResult> DeleteAvailabilityRule(Guid id, CancellationToken cancellationToken)
    {
        var rule = await _dbContext.AvailabilityRules.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (rule == null) return NotFound(new { error = "Availability rule not found." });

        _dbContext.AvailabilityRules.Remove(rule);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
