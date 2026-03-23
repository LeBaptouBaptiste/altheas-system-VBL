using Microsoft.AspNetCore.Mvc;
using API_Althea_systems.Models.Shared;

namespace API_Althea_systems.Controllers;

[ApiController]
[Route("api")]
public class HealthController : ControllerBase
{
    [HttpGet("health")]
    public ActionResult<HealthResponse> Health()
    {
        return Ok(new HealthResponse("healthy", "1.0.0", DateTime.UtcNow));
    }

    [HttpGet("version")]
    public ActionResult<object> Version()
    {
        return Ok(new { version = "1.0.0", environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production" });
    }
}
