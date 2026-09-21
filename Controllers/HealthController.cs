using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lingua.Controllers;

public class HealthController : ApiController
{
    [HttpGet("v1/health")]
    [AllowAnonymous]
    public IActionResult Get([FromServices] IWebHostEnvironment environment)
        => Ok(new { status = "ok", environment = environment.EnvironmentName });
}
