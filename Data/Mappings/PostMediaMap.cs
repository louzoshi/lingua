using Lingua.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingua.Data.Mappings;

public class PostMediaMap : IEntityTypeConfiguration<PostMedia>
{
    public void Configure(EntityTypeBuilder<PostMedia> builder)
    {
        builder.ToTable("PostMedia");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Url).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Kind).HasConversion<int>();

        builder.HasIndex(x => new { x.PostId, x.Position }, "IX_PostMedia_Post_Position");

        builder
            .HasOne(x => x.Post)
            .WithMany(x => x.Media)
            .HasForeignKey(x => x.PostId)
            .HasConstraintName("FK_PostMedia_Post")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
