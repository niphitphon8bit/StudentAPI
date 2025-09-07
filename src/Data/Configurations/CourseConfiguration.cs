using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentAPI.Models;

namespace StudentAPI.Data.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> entity)
    {
        entity.ToTable("Courses");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        entity.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        // Configure many-to-many relationship with Student
        entity.HasMany(e => e.Students)
            .WithMany(s => s.Courses)
            .UsingEntity<Dictionary<string, object>>(
                "CourseStudent",
                j => j.HasOne<Student>()
                      .WithMany()
                      .HasForeignKey("StudentId")
                      .HasConstraintName("FK_CourseStudent_Students_StudentId")
                      .OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<Course>()
                      .WithMany()
                      .HasForeignKey("CourseId")
                      .HasConstraintName("FK_CourseStudent_Courses_CourseId")
                      .OnDelete(DeleteBehavior.ClientCascade),
                j =>
                {
                    j.HasKey("CourseId", "StudentId");
                    j.ToTable("CourseStudents");
                });
    }
}
