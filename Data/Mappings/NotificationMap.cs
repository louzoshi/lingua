using Lingua.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingua.Data.Mappings;

public class NotificationMap : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notification");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Kind).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();

        builder.Property(x => x.ToName).IsRequired().HasMaxLength(80);
        builder.Property(x => x.ToEmail).IsRequired().HasMaxLength(160);
        builder.Property(x => x.Subject).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Body).IsRequired().HasMaxLength(8000);
        builder.Property(x => x.DedupeKey).HasMaxLength(120);
        builder.Property(x => x.LastError).HasMaxLength(500);

        // A varredura do worker é sempre "o que está pendente e já pode tentar".
        builder.HasIndex(x => new { x.Status, x.NextAttemptAt }, "IX_Notification_Status_NextAttempt");

        // Postgres e SQLite tratam NULL como distinto num índice único, então as mensagens
        // sem chave convivem à vontade e só a chave repetida é barrada.
        builder
            .HasIndex(x => x.DedupeKey, "IX_Notification_DedupeKey")
            .IsUnique();
    }
}
