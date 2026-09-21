using Microsoft.EntityFrameworkCore;

namespace Lingua.Notifications.Data;

/// <summary>
/// A visão que o worker tem do banco da plataforma. Não tem migration: quem cria e versiona
/// as tabelas é o app Lingua. Aqui só se declara o que este serviço lê e escreve —
/// <see cref="Notifications"/> de ida e volta, o resto só para montar o lembrete de mensalidade.
/// <para>
/// O mapeamento repete o do app de propósito: é o preço de o worker rodar em outro processo,
/// e o contrato entre os dois é o formato dessas tabelas, não um assembly compartilhado.
/// </para>
/// </summary>
public class NotificationsDataContext : DbContext
{
    public NotificationsDataContext(DbContextOptions<NotificationsDataContext> options)
        : base(options)
    {
    }

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<StudentPlan> StudentPlans => Set<StudentPlan>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(builder =>
        {
            builder.ToTable("Notification");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Kind).HasConversion<int>();
            builder.Property(x => x.Status).HasConversion<int>();
            builder.Property(x => x.ToName).HasMaxLength(80);
            builder.Property(x => x.ToEmail).HasMaxLength(160);
            builder.Property(x => x.Subject).HasMaxLength(200);
            builder.Property(x => x.Body).HasMaxLength(8000);
            builder.Property(x => x.DedupeKey).HasMaxLength(120);
            builder.Property(x => x.LastError).HasMaxLength(500);
        });

        modelBuilder.Entity<Student>(builder =>
        {
            builder.ToTable("User");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).HasMaxLength(80);
            builder.Property(x => x.Email).HasMaxLength(160);
        });

        modelBuilder.Entity<StudentPlan>(builder =>
        {
            builder.ToTable("StudentPlan");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.MonthlyValue).HasPrecision(10, 2);
            builder.Property(x => x.Status).HasConversion<int>();

            builder
                .HasOne(x => x.Student)
                .WithMany()
                .HasForeignKey(x => x.StudentId);
        });

        modelBuilder.Entity<Payment>(builder =>
        {
            builder.ToTable("Payment");
            builder.HasKey(x => x.Id);
        });
    }
}
