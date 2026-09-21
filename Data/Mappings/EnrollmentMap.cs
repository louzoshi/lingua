using Lingua.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingua.Data.Mappings;

public class EnrollmentMap : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("Enrollment");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        // Um aluno entra uma vez só em cada turma.
        builder
            .HasIndex(x => new { x.ClassroomId, x.StudentId }, "IX_Enrollment_Classroom_Student")
            .IsUnique();

        builder
            .HasOne(x => x.Classroom)
            .WithMany(x => x.Enrollments)
            .HasForeignKey(x => x.ClassroomId)
            .HasConstraintName("FK_Enrollment_Classroom")
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Student)
            .WithMany(x => x.Enrollments)
            .HasForeignKey(x => x.StudentId)
            .HasConstraintName("FK_Enrollment_Student")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
