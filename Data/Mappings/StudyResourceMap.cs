using Lingua.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingua.Data.Mappings;

public class StudyResourceMap : IEntityTypeConfiguration<StudyResource>
{
    public void Configure(EntityTypeBuilder<StudyResource> builder)
    {
        builder.ToTable("StudyResource");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Title).IsRequired().HasMaxLength(160);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Url).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Kind).HasConversion<int>();
        builder.Property(x => x.Level).HasConversion<int>();

        builder
            .HasOne(x => x.Classroom)
            .WithMany()
            .HasForeignKey(x => x.ClassroomId)
            .HasConstraintName("FK_StudyResource_Classroom")
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.CreatedBy)
            .WithMany()
            .HasForeignKey(x => x.CreatedById)
            .HasConstraintName("FK_StudyResource_CreatedBy")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
