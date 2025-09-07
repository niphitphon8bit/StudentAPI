using Microsoft.AspNetCore.Mvc;
using StudentAPI.Data;

namespace StudentAPI.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public HealthController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("db")]
    public async Task<IActionResult> Db()
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync();
            if (canConnect)
            {
                return Ok(new { status = "Healthy" });
            }
            return Problem("Cannot connect to database", statusCode: 503);
        }
        catch (Exception ex)
        {
            return Problem(ex.Message, statusCode: 503);
        }
    }
}

