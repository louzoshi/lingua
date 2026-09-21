using Lingua.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingua.Data.Mappings;

public class ReactionMap : IEntityTypeConfiguration<Reaction>
{
    public void Configure(EntityTypeBuilder<Reaction> builder)
    {
        builder.ToTable("Reaction");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        // Uma curtida por aluno em cada post.
        builder.HasIndex(x => new { x.PostId, x.UserId }, "IX_Reaction_Post_User").IsUnique();

        builder
            .HasOne(x => x.Post)
            .WithMany(x => x.Reactions)
            .HasForeignKey(x => x.PostId)
            .HasConstraintName("FK_Reaction_Post")
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("FK_Reaction_User")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
