using Lingua.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingua.Data.Mappings;

public class AssignmentSubmissionMap : IEntityTypeConfiguration<AssignmentSubmission>
{
    public void Configure(EntityTypeBuilder<AssignmentSubmission> builder)
    {
        builder.ToTable("AssignmentSubmission");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Url).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.Feedback).HasMaxLength(2000);
        builder.Property(x => x.Grade).HasPrecision(5, 2);

        // Uma entrega por aluno em cada trabalho; reenvio atualiza a mesma linha.
        builder
            .HasIndex(x => new { x.AssignmentId, x.StudentId }, "IX_Submission_Assignment_Student")
            .IsUnique();

        builder
            .HasOne(x => x.Assignment)
            .WithMany(x => x.Submissions)
            .HasForeignKey(x => x.AssignmentId)
            .HasConstraintName("FK_Submission_Assignment")
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .HasConstraintName("FK_Submission_Student")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
