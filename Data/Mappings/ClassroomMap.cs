using Lingua.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingua.Data.Mappings;

public class ClassroomMap : IEntityTypeConfiguration<Classroom>
{
    public void Configure(EntityTypeBuilder<Classroom> builder)
    {
        builder.ToTable("Classroom");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(120);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Level).HasConversion<int>();

        builder.HasIndex(x => x.Slug, "IX_Classroom_Slug").IsUnique();

        builder
            .HasOne(x => x.Teacher)
            .WithMany(x => x.TeachingClassrooms)
            .HasForeignKey(x => x.TeacherId)
            .HasConstraintName("FK_Classroom_Teacher")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
