using Lingua.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingua.Data.Mappings;

public class CommentMap : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comment");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Body).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<int>();

        builder.HasIndex(x => new { x.PostId, x.CreatedAt }, "IX_Comment_Post_Date");

        builder
            .HasOne(x => x.Post)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.PostId)
            .HasConstraintName("FK_Comment_Post")
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Author)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.AuthorId)
            .HasConstraintName("FK_Comment_Author")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
