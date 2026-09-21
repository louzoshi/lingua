using Lingua.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingua.Data.Mappings;

public class LessonAttendanceMap : IEntityTypeConfiguration<LessonAttendance>
{
    public void Configure(EntityTypeBuilder<LessonAttendance> builder)
    {
        builder.ToTable("LessonAttendance");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder
            .HasIndex(x => new { x.LessonId, x.StudentId }, "IX_Attendance_Lesson_Student")
            .IsUnique();

        builder
            .HasOne(x => x.Lesson)
            .WithMany(x => x.Attendances)
            .HasForeignKey(x => x.LessonId)
            .HasConstraintName("FK_Attendance_Lesson")
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .HasConstraintName("FK_Attendance_Student")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
