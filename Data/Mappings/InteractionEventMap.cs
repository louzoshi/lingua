using Lingua.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingua.Data.Mappings;

public class InteractionEventMap : IEntityTypeConfiguration<InteractionEvent>
{
    public void Configure(EntityTypeBuilder<InteractionEvent> builder)
    {
        builder.ToTable("InteractionEvent");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Type).HasConversion<int>();

        // O ranking agrega por usuário dentro de um período.
        builder.HasIndex(x => new { x.UserId, x.OccurredAt }, "IX_Interaction_User_Date");
        builder.HasIndex(x => new { x.ClassroomId, x.OccurredAt }, "IX_Interaction_Classroom_Date");

        builder
            .HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .HasConstraintName("FK_Interaction_User")
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Classroom)
            .WithMany()
            .HasForeignKey(x => x.ClassroomId)
            .HasConstraintName("FK_Interaction_Classroom")
            .OnDelete(DeleteBehavior.SetNull);
    }
}
