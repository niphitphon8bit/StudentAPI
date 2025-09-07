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
    public async Task CreateStudent_BadRequest_On_Invalid_ModelState()
    {
        var (ctx, controller) = Create();
        controller.ModelState.AddModelError("Name", "Required");

        var result = await controller.CreateStudent(new Student { Name = "", Age = 20 });

        result.Should().BeOfType<BadRequestObjectResult>();
        ctx.Students.Count().Should().Be(0);
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

    [Fact]
    public async Task GetAllStudents_Returns_Ok_With_Empty_List()
    {
        var (_, controller) = Create();

        var result = await controller.GetAllStudents();

        result.Should().BeOfType<OkObjectResult>();
        var ok = result as OkObjectResult;
        ok!.Value.Should().BeAssignableTo<IEnumerable<Student>>();
        (ok!.Value as IEnumerable<Student>)!.Count().Should().Be(0);
    }

    [Fact]
    public async Task GetAllStudents_Includes_Courses()
    {
        var (ctx, controller) = Create();
        var course = new Course { Title = "Math" };
        var student = new Student { Name = "Stu", Age = 18 };
        student.Courses.Add(course);
        ctx.Students.Add(student);
        await ctx.SaveChangesAsync();

        var result = await controller.GetAllStudents();

        var ok = result as OkObjectResult;
        var list = ok!.Value as IEnumerable<Student>;
        list!.Single().Courses.Should().HaveCount(1);
        list!.Single().Courses.Single().Title.Should().Be("Math");
    }

    [Fact]
    public async Task GetStudentById_Returns_Ok_When_Found()
    {
        var (ctx, controller) = Create();
        var s = new Student { Name = "Alice", Age = 20 };
        ctx.Students.Add(s);
        await ctx.SaveChangesAsync();

        var result = await controller.GetStudentById(s.Id);

        result.Should().BeOfType<OkObjectResult>();
        var ok = result as OkObjectResult;
        ok!.Value.Should().BeAssignableTo<Student>();
        (ok!.Value as Student)!.Id.Should().Be(s.Id);
    }

    [Fact]
    public async Task GetStudentById_Returns_NotFound_When_Missing()
    {
        var (_, controller) = Create();

        var result = await controller.GetStudentById(12345);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetStudentById_Includes_Courses()
    {
        var (ctx, controller) = Create();
        var course = new Course { Title = "Science" };
        var s = new Student { Name = "Alice", Age = 20 };
        s.Courses.Add(course);
        ctx.Students.Add(s);
        await ctx.SaveChangesAsync();

        var result = await controller.GetStudentById(s.Id);

        var ok = result as OkObjectResult;
        var returned = ok!.Value as Student;
        returned!.Courses.Should().HaveCount(1);
        returned!.Courses.Single().Title.Should().Be("Science");
    }

    [Fact]
    public async Task GetAllStudents_InternalServerError_On_Exception()
    {
        var (ctx, controller) = Create();
        await ctx.DisposeAsync();

        var result = await controller.GetAllStudents();

        var obj = result as ObjectResult;
        obj!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task CreateStudent_InternalServerError_On_Exception()
    {
        var (ctx, controller) = Create();
        await ctx.DisposeAsync();

        var result = await controller.CreateStudent(new Student { Name = "X", Age = 1 });

        var obj = result as ObjectResult;
        obj!.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task UpdateStudent_Returns_NoContent_On_Success()
    {
        var (ctx, controller) = Create();
        var s = new Student { Name = "Bob", Age = 22 };
        ctx.Students.Add(s);
        await ctx.SaveChangesAsync();

        var updated = new Student { Id = s.Id, Name = "Bobby", Age = 23 };
        var result = await controller.UpdateStudent(s.Id, updated);

        result.Should().BeOfType<NoContentResult>();
        var inDb = await ctx.Students.FindAsync(s.Id);
        inDb!.Name.Should().Be("Bobby");
        inDb.Age.Should().Be(23);
    }

    [Fact]
    public async Task UpdateStudent_Returns_NotFound_When_Missing()
    {
        var (_, controller) = Create();
        var updated = new Student { Id = 999, Name = "Nobody", Age = 1 };

        var result = await controller.UpdateStudent(999, updated);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateStudent_Returns_BadRequest_On_Id_Mismatch()
    {
        var (_, controller) = Create();
        var updated = new Student { Id = 2, Name = "X", Age = 1 };

        var result = await controller.UpdateStudent(1, updated);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateStudent_Returns_BadRequest_On_Invalid_ModelState()
    {
        var (ctx, controller) = Create();
        var s = new Student { Name = "Carl", Age = 30 };
        ctx.Students.Add(s);
        await ctx.SaveChangesAsync();

        controller.ModelState.AddModelError("Name", "Required");
        var updated = new Student { Id = s.Id, Name = "", Age = 31 };

        var result = await controller.UpdateStudent(s.Id, updated);

        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
