using System;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentAPI.Controllers;
using StudentAPI.Data;
using Xunit;

namespace StudentAPI.Tests;

public class HealthControllerTests
{
    private static (ApplicationDbContext ctx, HealthController controller) Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new ApplicationDbContext(options);
        var controller = new HealthController(ctx);
        return (ctx, controller);
    }

    [Fact]
    public async Task Db_Returns_Ok_When_CanConnect()
    {
        var (_, controller) = Create();

        var result = await controller.Db();

        result.Should().BeOfType<OkObjectResult>();
        var ok = result as OkObjectResult;
        ok!.Value.Should().NotBeNull();

        var statusProp = ok!.Value!.GetType().GetProperty("status", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        statusProp.Should().NotBeNull();
        statusProp!.GetValue(ok!.Value)!.ToString().Should().Be("Healthy");
    }

    [Fact]
    public async Task Db_Returns_503_On_Exception()
    {
        var (ctx, controller) = Create();
        await ctx.DisposeAsync();

        var result = await controller.Db();

        var obj = result as ObjectResult;
        obj!.StatusCode.Should().Be(503);
    }
}

