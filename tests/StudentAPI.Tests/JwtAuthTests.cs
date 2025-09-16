using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StudentAPI.Data;
using Xunit;

namespace StudentAPI.Tests;

public class JwtAuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Issuer = "https://studentapi.local";
    private const string Audience = "https://studentapi.local";
    // Use >= 32 bytes key for HS256
    private const string Key = "ThisIsASufficientlyLongHS256SecretKey!!";

    private readonly WebApplicationFactory<Program> _factory;

    public JwtAuthTests(WebApplicationFactory<Program> factory)
    {
        // Ensure JWT env vars are available to Program
        Environment.SetEnvironmentVariable("JWT__Issuer", Issuer);
        Environment.SetEnvironmentVariable("JWT__Audience", Audience);
        Environment.SetEnvironmentVariable("JWT__Key", Key);

        // Provide dummy DB envs (won't be used after override)
        Environment.SetEnvironmentVariable("DB_SERVER", "localhost");
        Environment.SetEnvironmentVariable("DB_NAME", "StudentDB");
        Environment.SetEnvironmentVariable("DB_USER", "sa");
        Environment.SetEnvironmentVariable("DB_PASSWORD", "YourStrong!Passw0rd");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Replace SQL Server ApplicationDbContext with InMemory DB
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("JwtAuthTests"));
            });
        });
    }

    private static string CreateJwt()
    {
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "test-user"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            },
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task Without_Token_Returns_401()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/auth-test/ping");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task With_Valid_Token_Returns_200()
    {
        var client = _factory.CreateClient();
        var token = CreateJwt();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await client.GetAsync("/auth-test/ping");

        // Authorized should pass and controller returns OK
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().NotBeNull();
    }
}
