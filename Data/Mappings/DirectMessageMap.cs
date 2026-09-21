using Lingua.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingua.Data.Mappings;

public class DirectMessageMap : IEntityTypeConfiguration<DirectMessage>
{
    public void Configure(EntityTypeBuilder<DirectMessage> builder)
    {
        builder.ToTable("DirectMessage");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Body).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.Status).HasConversion<int>();

        builder.HasIndex(x => new { x.ConversationId, x.SentAt }, "IX_DirectMessage_Conversation_Date");

        builder
            .HasOne(x => x.Conversation)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.ConversationId)
            .HasConstraintName("FK_DirectMessage_Conversation")
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Sender)
            .WithMany()
            .HasForeignKey(x => x.SenderId)
            .HasConstraintName("FK_DirectMessage_Sender")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
