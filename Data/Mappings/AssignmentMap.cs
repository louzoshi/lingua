using Lingua.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingua.Data.Mappings;

public class AssignmentMap : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignment");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Title).IsRequired().HasMaxLength(160);
        builder.Property(x => x.Instructions).HasMaxLength(4000);
        builder.Property(x => x.Url).HasMaxLength(500);

        builder.HasIndex(x => new { x.ClassroomId, x.DueDate }, "IX_Assignment_Classroom_Due");

        builder
            .HasOne(x => x.Classroom)
            .WithMany(x => x.Assignments)
            .HasForeignKey(x => x.ClassroomId)
            .HasConstraintName("FK_Assignment_Classroom")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
