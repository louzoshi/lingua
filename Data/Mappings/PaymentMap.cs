using Lingua.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingua.Data.Mappings;

public class PaymentMap : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payment");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Amount).HasPrecision(10, 2);
        builder.Property(x => x.Method).HasMaxLength(60);
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasIndex(x => new { x.PlanId, x.ReferenceMonth }, "IX_Payment_Plan_Month");

        builder
            .HasOne(x => x.Plan)
            .WithMany(x => x.Payments)
            .HasForeignKey(x => x.PlanId)
            .HasConstraintName("FK_Payment_Plan")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
