using Lingua.Notifications.Data;
using Lingua.Notifications.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lingua.Notifications.Workers;

/// <summary>
/// Tira da fila e entrega. É a razão de este serviço existir em outro processo: o app grava a
/// linha e responde na hora; a lentidão e a instabilidade do servidor de e-mail ficam deste lado.
/// <para>
/// A reserva é feita empurrando <c>NextAttemptAt</c> para a frente antes de sair enviando. Se o
/// processo cair no meio do lote, as mensagens voltam sozinhas quando a reserva vence — não
/// precisa de lápide nem de limpeza manual.
/// </para>
/// </summary>
public class OutboxDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly EmailSender _sender;
    private readonly DispatcherOptions _options;
    private readonly ILogger<OutboxDispatcher> _logger;

    private bool _warnedAboutSmtp;

    public OutboxDispatcher(
        IServiceScopeFactory scopeFactory,
        EmailSender sender,
        IOptions<DispatcherOptions> options,
        ILogger<OutboxDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _sender = sender;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var idle = TimeSpan.FromSeconds(Math.Max(1, _options.PollSeconds));

        _logger.LogInformation(
            "Despachante de notificações no ar. Varredura a cada {Seconds}s, lote de {Batch}.",
            idle.TotalSeconds, _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            int handled;

            try
            {
                handled = await DispatchBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Banco fora do ar não pode derrubar o worker: espera e tenta de novo.
                _logger.LogError(ex, "Erro ao varrer a fila de notificações.");
                handled = 0;
            }

            // Lote cheio provavelmente significa mais coisa esperando; só descansa quando esvazia.
            if (handled >= _options.BatchSize)
                continue;

            try
            {
                await Task.Delay(idle, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task<int> DispatchBatchAsync(CancellationToken token)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationsDataContext>();

        var now = DateTime.UtcNow;
        var lease = TimeSpan.FromMinutes(Math.Max(1, _options.LeaseMinutes));

        var batch = await context
            .Notifications
            .Where(x => x.Status == NotificationStatus.Pending && x.NextAttemptAt <= now)
            .OrderBy(x => x.Id)
            .Take(Math.Max(1, _options.BatchSize))
            .ToListAsync(token);

        if (batch.Count == 0)
            return 0;

        foreach (var notification in batch)
            notification.NextAttemptAt = now + lease;

        await context.SaveChangesAsync(token);

        foreach (var notification in batch)
        {
            var (outcome, error) = await _sender.SendAsync(
                notification.ToName, notification.ToEmail, notification.Subject, notification.Body, token);

            switch (outcome)
            {
                case DeliveryOutcome.Sent:
                    notification.Status = NotificationStatus.Sent;
                    notification.SentAt = DateTime.UtcNow;
                    notification.Attempts++;
                    notification.LastError = null;
                    break;

                case DeliveryOutcome.NotConfigured:
                    // Sem SMTP a plataforma continua funcionando, como sempre funcionou: a
                    // mensagem espera na fila, sem gastar tentativa, até alguém configurar.
                    WarnAboutMissingSmtpOnce();
                    break;

                case DeliveryOutcome.Failed:
                    notification.Attempts++;
                    notification.LastError = error;

                    if (notification.Attempts >= _options.MaxAttempts)
                    {
                        notification.Status = NotificationStatus.Failed;
                        _logger.LogError(
                            "Notificação {Id} para {Email} desistiu depois de {Attempts} tentativas: {Error}",
                            notification.Id, notification.ToEmail, notification.Attempts, error);
                    }
                    else
                    {
                        notification.NextAttemptAt = DateTime.UtcNow + BackoffFor(notification.Attempts);
                    }

                    break;
            }
        }

        await context.SaveChangesAsync(token);

        return batch.Count;
    }

    /// <summary>Backoff exponencial: 30s, 1min30, 4min30, 13min30... limitado a uma hora.</summary>
    private TimeSpan BackoffFor(int attempts)
    {
        var seconds = Math.Max(1, _options.RetryBaseSeconds) * Math.Pow(3, attempts - 1);
        return TimeSpan.FromSeconds(Math.Min(seconds, TimeSpan.FromHours(1).TotalSeconds));
    }

    private void WarnAboutMissingSmtpOnce()
    {
        if (_warnedAboutSmtp)
            return;

        _warnedAboutSmtp = true;
        _logger.LogWarning(
            "SMTP não configurado. As notificações ficam na fila e saem assim que houver servidor.");
    }
}
