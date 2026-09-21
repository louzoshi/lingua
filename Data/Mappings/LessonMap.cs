using Lingua.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingua.Data.Mappings;

public class LessonMap : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("Lesson");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Title).IsRequired().HasMaxLength(160);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.MeetingUrl).HasMaxLength(500);
        builder.Property(x => x.RecordingUrl).HasMaxLength(500);

        builder.HasIndex(x => new { x.ClassroomId, x.ScheduledAt }, "IX_Lesson_Classroom_Date");

        builder
            .HasOne(x => x.Classroom)
            .WithMany(x => x.Lessons)
            .HasForeignKey(x => x.ClassroomId)
            .HasConstraintName("FK_Lesson_Classroom")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
