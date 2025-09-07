using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StudentAPI.Models;

namespace StudentAPI.Data.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> entity)
    {
        entity.ToTable("Students");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        entity.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(e => e.Age)
            .IsRequired();

        entity.ToTable(t => t.HasCheckConstraint(
            "CK_Students_Age_NonNegative",
            "[Age] >= 0"));

        // Configure many-to-many relationship with Course
        entity.HasMany(e => e.Courses)
            .WithMany(c => c.Students)
            .UsingEntity<Dictionary<string, object>>(
                "CourseStudent",
                j => j.HasOne<Course>()
                      .WithMany()
                      .HasForeignKey("CourseId")
                      .HasConstraintName("FK_CourseStudent_Courses_CourseId")
                      .OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<Student>()
                      .WithMany()
                      .HasForeignKey("StudentId")
                      .HasConstraintName("FK_CourseStudent_Students_StudentId")
                      .OnDelete(DeleteBehavior.ClientCascade),
                j =>
                {
                    j.HasKey("StudentId", "CourseId");
                    j.ToTable("CourseStudents");
                });
    }
}
