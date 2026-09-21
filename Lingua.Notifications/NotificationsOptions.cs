namespace Lingua.Notifications;

/// <summary>Como o worker fala com o servidor de e-mail.</summary>
public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromName { get; set; } = "Lingua";
    public string FromEmail { get; set; } = "no-reply@lingua.local";

    /// <summary>
    /// STARTTLS. Ligado, que é o que todo servidor de verdade exige. Desligar só faz sentido
    /// contra um relay local de desenvolvimento, como o MailHog, que fala em texto puro.
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}

/// <summary>Ritmo da entrega e política de retentativa.</summary>
public class DispatcherOptions
{
    /// <summary>Espera entre varreduras quando a fila está vazia.</summary>
    public int PollSeconds { get; set; } = 15;

    /// <summary>Quantas mensagens o worker pega de uma vez.</summary>
    public int BatchSize { get; set; } = 20;

    /// <summary>Desistir depois de tantas tentativas. A linha vira <c>Failed</c> e fica no banco.</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Por quanto tempo a mensagem fica reservada ao pegá-la. Se o worker morrer no meio do
    /// envio, a linha volta a ser elegível quando essa reserva vence — em vez de ficar presa.
    /// </summary>
    public int LeaseMinutes { get; set; } = 5;

    /// <summary>Base do backoff exponencial entre tentativas.</summary>
    public int RetryBaseSeconds { get; set; } = 30;
}

/// <summary>Lembrete de mensalidade.</summary>
public class BillingOptions
{
    /// <summary>
    /// Desligado por padrão: cobrar aluno automaticamente é decisão da escola, não default
    /// de infraestrutura. Ligue só quando a professora quiser.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>Hora UTC em que o agendador roda. 12 UTC ≈ 9h de Brasília.</summary>
    public int RunAtHourUtc { get; set; } = 12;

    /// <summary>Avisar com esta antecedência, em dias, até o dia do vencimento.</summary>
    public int DaysBefore { get; set; } = 3;

    /// <summary>
    /// Passar uma vez logo após subir, além do horário fixo. Sem isso, um worker que reinicia
    /// todo dia depois da hora marcada nunca avisaria ninguém. Repetir é inofensivo: a chave
    /// de idempotência segura.
    /// </summary>
    public bool RunOnStartup { get; set; } = true;
}
