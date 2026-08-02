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
public class ServicesController : ControllerBase
{
    private readonly IWorkPilotDbContext _dbContext;

    public ServicesController(IWorkPilotDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("businesses/{businessId:guid}/services")]
    public async Task<IActionResult> GetServices(Guid businessId, CancellationToken cancellationToken)
    {
        var services = await _dbContext.Services
            .Where(s => s.BusinessId == businessId)
            .Select(s => new ServiceDto(s.Id, s.BusinessId, s.Name, s.Description, s.Price, s.DurationMinutes, s.IsActive, s.CreatedAt))
            .ToListAsync(cancellationToken);

        return Ok(services);
    }

    [HttpPost("businesses/{businessId:guid}/services")]
    public async Task<IActionResult> CreateService(Guid businessId, [FromBody] CreateServiceDto dto, CancellationToken cancellationToken)
    {
        var s = new Service
        {
            BusinessId = businessId,
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            DurationMinutes = dto.DurationMinutes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Services.Add(s);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ServiceDto(s.Id, s.BusinessId, s.Name, s.Description, s.Price, s.DurationMinutes, s.IsActive, s.CreatedAt));
    }

    [HttpPut("services/{id:guid}")]
    public async Task<IActionResult> UpdateService(Guid id, [FromBody] UpdateServiceDto dto, CancellationToken cancellationToken)
    {
        var s = await _dbContext.Services.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (s == null) return NotFound(new { error = "Service not found." });

        s.Name = dto.Name;
        s.Description = dto.Description;
        s.Price = dto.Price;
        s.DurationMinutes = dto.DurationMinutes;
        s.IsActive = dto.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(new ServiceDto(s.Id, s.BusinessId, s.Name, s.Description, s.Price, s.DurationMinutes, s.IsActive, s.CreatedAt));
    }

    [HttpDelete("services/{id:guid}")]
    public async Task<IActionResult> DeleteService(Guid id, CancellationToken cancellationToken)
    {
        var s = await _dbContext.Services.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (s == null) return NotFound(new { error = "Service not found." });

        _dbContext.Services.Remove(s);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
