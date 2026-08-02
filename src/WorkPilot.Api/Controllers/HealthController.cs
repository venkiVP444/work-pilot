using System;
using Microsoft.AspNetCore.Mvc;

namespace WorkPilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "WorkPilot AI API",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }
}
