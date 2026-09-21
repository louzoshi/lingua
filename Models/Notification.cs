namespace Lingua.Models;

/// <summary>
/// Uma mensagem esperando para sair da plataforma. A aplicação só grava a linha e segue;
/// quem entrega é o worker <c>Lingua.Notifications</c>, em outro processo.
/// <para>
/// É a fila: em vez de um broker à parte, a tabela vive no mesmo banco, então gravar a
/// notificação entra na mesma transação do que a originou — convidar um aluno e enfileirar
/// o e-mail dele não podem dar certo pela metade.
/// </para>
/// </summary>
public class Notification
{
    public int Id { get; set; }

    public NotificationKind Kind { get; set; }
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;

    public string ToName { get; set; } = null!;
    public string ToEmail { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;

    /// <summary>
    /// Chave de idempotência do que gerou a mensagem, única quando preenchida. O lembrete de
    /// mensalidade usa <c>billing:{planId}:{ano-mês}</c>, então rodar o agendador duas vezes no
    /// mesmo dia — ou reiniciar o worker — não manda o aviso de novo. Nulo em mensagem avulsa.
    /// </summary>
    public string? DedupeKey { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Quando o worker pode tentar de novo. O dispatcher empurra esse valor para a frente ao pegar a linha.</summary>
    public DateTime NextAttemptAt { get; set; } = DateTime.UtcNow;

    public int Attempts { get; set; }
    public DateTime? SentAt { get; set; }

    /// <summary>Último erro de entrega, para a professora entender o que falhou.</summary>
    public string? LastError { get; set; }
}
