using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace StudentAPI.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config)
    {
        _config = config;
    }

    public record LoginRequest(string Username, string Password);
    public record TokenResponse(string Token, DateTime ExpiresAtUtc);

    [AllowAnonymous]
    [HttpPost("token")]
    public IActionResult CreateToken([FromBody] LoginRequest request)
    {
        // Demo-only: accept any non-empty username/password. Replace with real validation.
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Username and password are required.");
        }

        var issuer = _config["JWT:Issuer"] ?? Environment.GetEnvironmentVariable("JWT__Issuer");
        var audience = _config["JWT:Audience"] ?? Environment.GetEnvironmentVariable("JWT__Audience");
        var key = _config["JWT:Key"] ?? Environment.GetEnvironmentVariable("JWT__Key");
        if (string.IsNullOrWhiteSpace(key))
        {
            return StatusCode(500, "JWT key is not configured.");
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;
        var expires = now.AddHours(1);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, request.Username)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now.AddMinutes(-1),
            expires: expires,
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(new TokenResponse(tokenString, expires));
    }
}

