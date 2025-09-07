using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentAPI.Controllers;
using StudentAPI.Data;
using StudentAPI.Models;
using Xunit;

namespace StudentAPI.Tests;

public class StudentControllerTests
{
    private static (ApplicationDbContext ctx, StudentController controller) Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new ApplicationDbContext(options);
        var controller = new StudentController(ctx);
        return (ctx, controller);
    }

    [Fact]
    public async Task CreateStudent_Returns_Created()
    {
        var (ctx, controller) = Create();
        var student = new Student { Name = "Alice", Age = 20 };

        var result = await controller.CreateStudent(student);

        result.Should().BeOfType<CreatedAtActionResult>();
        ctx.Students.Count().Should().Be(1);
        ctx.Students.Single().Name.Should().Be("Alice");
    }

    [Fact]
    public async Task CreateStudent_Conflict_On_Duplicate()
    {
        var (ctx, controller) = Create();
        ctx.Students.Add(new Student { Name = "Bob", Age = 22 });
        await ctx.SaveChangesAsync();

        var result = await controller.CreateStudent(new Student { Name = "Bob", Age = 22 });

        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task GetAllStudents_Returns_Ok_With_List()
    {
        var (ctx, controller) = Create();
        ctx.Students.AddRange(new Student { Name = "A", Age = 18 }, new Student { Name = "B", Age = 19 });
        await ctx.SaveChangesAsync();

        var result = await controller.GetAllStudents();

        result.Should().BeOfType<OkObjectResult>();
        var ok = result as OkObjectResult;
        ok!.Value.Should().BeAssignableTo<IEnumerable<Student>>();
        (ok!.Value as IEnumerable<Student>)!.Count().Should().Be(2);
    }
}
