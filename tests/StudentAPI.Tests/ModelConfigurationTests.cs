using System;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using StudentAPI.Data;
using StudentAPI.Models;
using Xunit;

namespace StudentAPI.Tests;

public class ModelConfigurationTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void Student_Config_Is_Applied()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(Student));
        entity.Should().NotBeNull();
        entity!.GetTableName().Should().Be("Students");

        var nameProp = entity.FindProperty(nameof(Student.Name));
        nameProp.Should().NotBeNull();
        nameProp!.IsNullable.Should().BeFalse();
        nameProp.GetMaxLength().Should().Be(100);

        var ageProp = entity.FindProperty(nameof(Student.Age));
        ageProp.Should().NotBeNull();
        ageProp!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Course_Config_Is_Applied()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(Course));
        entity.Should().NotBeNull();
        entity!.GetTableName().Should().Be("Courses");

        var titleProp = entity.FindProperty(nameof(Course.Title));
        titleProp.Should().NotBeNull();
        titleProp!.IsNullable.Should().BeFalse();
        titleProp.GetMaxLength().Should().Be(200);
    }

    [Fact]
    public void ManyToMany_Relationship_Configured()
    {
        using var ctx = CreateContext();
        var studentType = ctx.Model.FindEntityType(typeof(Student))!;
        var courseType = ctx.Model.FindEntityType(typeof(Course))!;

        // Verify skip navigations exist on both sides
        studentType.GetSkipNavigations().Any(n => n.Name == nameof(Student.Courses)).Should().BeTrue();
        courseType.GetSkipNavigations().Any(n => n.Name == nameof(Course.Students)).Should().BeTrue();
    }
}
