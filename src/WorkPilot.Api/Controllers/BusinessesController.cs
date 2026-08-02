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
[Route("api/[controller]")]
public class BusinessesController : ControllerBase
{
    private readonly IWorkPilotDbContext _dbContext;

    public BusinessesController(IWorkPilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBusiness(Guid id, CancellationToken cancellationToken)
    {
        var b = await _dbContext.Businesses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (b == null) return NotFound(new { error = "Business not found." });

        return Ok(new BusinessDto(
            b.Id, b.Name, b.Description, b.Location, b.ContactEmail, b.TimeZone,
            b.CancellationPolicy, b.CommunicationTone, b.IsCalendarConnected, b.GoogleCalendarId, b.CreatedAt));
    }

    [HttpPost]
    public async Task<IActionResult> CreateBusiness([FromBody] CreateBusinessDto dto, CancellationToken cancellationToken)
    {
        var b = new Business
        {
            Name = dto.Name,
            Description = dto.Description,
            Location = dto.Location,
            ContactEmail = dto.ContactEmail,
            TimeZone = dto.TimeZone,
            CancellationPolicy = dto.CancellationPolicy,
            CommunicationTone = dto.CommunicationTone,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Businesses.Add(b);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetBusiness), new { id = b.Id }, new BusinessDto(
            b.Id, b.Name, b.Description, b.Location, b.ContactEmail, b.TimeZone,
            b.CancellationPolicy, b.CommunicationTone, b.IsCalendarConnected, b.GoogleCalendarId, b.CreatedAt));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateBusiness(Guid id, [FromBody] UpdateBusinessDto dto, CancellationToken cancellationToken)
    {
        var b = await _dbContext.Businesses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (b == null) return NotFound(new { error = "Business not found." });

        b.Name = dto.Name;
        b.Description = dto.Description;
        b.Location = dto.Location;
        b.ContactEmail = dto.ContactEmail;
        b.TimeZone = dto.TimeZone;
        b.CancellationPolicy = dto.CancellationPolicy;
        b.CommunicationTone = dto.CommunicationTone;
        b.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new BusinessDto(
            b.Id, b.Name, b.Description, b.Location, b.ContactEmail, b.TimeZone,
            b.CancellationPolicy, b.CommunicationTone, b.IsCalendarConnected, b.GoogleCalendarId, b.CreatedAt));
    }
}
