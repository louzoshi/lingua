using Lingua.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingua.Data.Mappings;

public class PostMap : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("Post");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Title).IsRequired().HasMaxLength(160);
        builder.Property(x => x.Summary).IsRequired().HasMaxLength(255);
        builder.Property(x => x.Body).IsRequired();
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(180);
        builder.Property(x => x.Scope).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();

        builder.HasIndex(x => x.Slug, "IX_Post_Slug").IsUnique();

        // O feed sempre lê por mural e data, então o índice acompanha essa ordem.
        builder.HasIndex(x => new { x.ClassroomId, x.LastUpdateDate }, "IX_Post_Classroom_Date");

        builder
            .HasOne(x => x.Author)
            .WithMany(x => x.Posts)
            .HasForeignKey(x => x.AuthorId)
            .HasConstraintName("FK_Post_Author")
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Classroom)
            .WithMany(x => x.Posts)
            .HasForeignKey(x => x.ClassroomId)
            .HasConstraintName("FK_Post_Classroom")
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Topic)
            .WithMany(x => x.Posts)
            .HasForeignKey(x => x.TopicId)
            .HasConstraintName("FK_Post_Topic")
            .OnDelete(DeleteBehavior.SetNull);

        builder
            .HasMany(x => x.Tags)
            .WithMany(x => x.Posts)
            .UsingEntity<Dictionary<string, object>>(
                "PostTag",
                tag => tag
                    .HasOne<Tag>()
                    .WithMany()
                    .HasForeignKey("TagId")
                    .HasConstraintName("FK_PostTag_TagId")
                    .OnDelete(DeleteBehavior.Cascade),
                post => post
                    .HasOne<Post>()
                    .WithMany()
                    .HasForeignKey("PostId")
                    .HasConstraintName("FK_PostTag_PostId")
                    .OnDelete(DeleteBehavior.Cascade));
    }
}
