using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentAPI.Controllers;

[ApiController]
[Route("auth-test")]
public class AuthTestController : ControllerBase
{
    [Authorize]
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { ok = true });
    }
}

