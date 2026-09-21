namespace Lingua.Notifications.Data;

/// <summary>O que originou uma notificação. Espelha <c>Lingua.Models.NotificationKind</c>.</summary>
public enum NotificationKind
{
    StudentInvite = 1,
    BillingReminder = 2
}

/// <summary>Situação na fila. Espelha <c>Lingua.Models.NotificationStatus</c>.</summary>
public enum NotificationStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3
}

/// <summary>Espelha <c>Lingua.Models.PlanStatus</c>.</summary>
public enum PlanStatus
{
    Active = 1,
    Paused = 2,
    Ended = 3
}

/// <summary>A fila de saída. Única tabela que o worker escreve.</summary>
public class Notification
{
    public int Id { get; set; }
    public NotificationKind Kind { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    public string ToName { get; set; } = null!;
    public string ToEmail { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string? DedupeKey { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;
    public int Attempts { get; set; }
    public DateTime? SentAt { get; set; }
    public string? LastError { get; set; }
}

/// <summary>
/// Leitura da tabela <c>User</c>. Só as colunas que o lembrete de mensalidade precisa —
/// o worker não tem nada que ver com senha, perfil ou matrícula.
/// </summary>
public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public bool IsActive { get; set; }
}

/// <summary>Leitura da tabela <c>StudentPlan</c>: quem paga quanto e em que dia.</summary>
public class StudentPlan
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public decimal MonthlyValue { get; set; }
    public int DueDay { get; set; }
    public PlanStatus Status { get; set; }
}

/// <summary>Leitura da tabela <c>Payment</c>, para saber quem já quitou o mês.</summary>
public class Payment
{
    public int Id { get; set; }
    public int PlanId { get; set; }
    public DateTime ReferenceMonth { get; set; }
}
