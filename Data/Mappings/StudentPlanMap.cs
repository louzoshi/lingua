using Lingua.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lingua.Data.Mappings;

public class StudentPlanMap : IEntityTypeConfiguration<StudentPlan>
{
    public void Configure(EntityTypeBuilder<StudentPlan> builder)
    {
        builder.ToTable("StudentPlan");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.MonthlyValue).HasPrecision(10, 2);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.ContractFile).HasMaxLength(120);
        builder.Property(x => x.ContractOriginalName).HasMaxLength(255);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.HasIndex(x => new { x.StudentId, x.Status }, "IX_StudentPlan_Student_Status");

        builder
            .HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .HasConstraintName("FK_StudentPlan_Student")
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Package)
            .WithMany(x => x.Plans)
            .HasForeignKey(x => x.PackageId)
            .HasConstraintName("FK_StudentPlan_Package")
            .OnDelete(DeleteBehavior.SetNull);
    }
}
