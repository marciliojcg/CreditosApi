using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CreditosApi.API.HealthChecks;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    [HttpGet("self")]
    public IActionResult Self()
    {
        return Ok(new { status = "OK" });
    }

    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        var report = await _healthCheckService.CheckHealthAsync();

        return report.Status == HealthStatus.Healthy
            ? Ok(new { status = "READY" })
            : StatusCode(503, new { status = "UNHEALTHY" });
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        var report = await _healthCheckService.CheckHealthAsync();

        return report.Status == HealthStatus.Healthy
            ? Ok(report)
            : StatusCode(503, report);
    }
}