using System.Globalization;
using Lingua.Notifications.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Lingua.Notifications.Workers;

/// <summary>
/// Uma vez por dia, olha quem tem mensalidade vencendo e ainda não pagou o mês, e enfileira o
/// aviso. É o trabalho que não tinha onde morar: um app web só faz o que alguém pediu, e
/// ninguém abre a tela do financeiro para o sistema lembrar de cobrar.
/// <para>
/// Não envia nada: escreve na mesma fila que o app escreve, e o despachante entrega.
/// </para>
/// </summary>
public class BillingReminderScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly BillingOptions _options;
    private readonly ILogger<BillingReminderScheduler> _logger;

    /// <summary>Folga para o banco estar de pé antes da primeira passada.</summary>
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);

    public BillingReminderScheduler(
        IServiceScopeFactory scopeFactory,
        IOptions<BillingOptions> options,
        ILogger<BillingReminderScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation(
                "Lembrete de mensalidade desligado (Billing:Enabled). Nenhum aviso automático será enfileirado.");
            return;
        }

        _logger.LogInformation(
            "Lembrete de mensalidade ligado: roda às {Hour}h UTC, avisando {Days} dias antes.",
            _options.RunAtHourUtc, _options.DaysBefore);

        if (_options.RunOnStartup && !await DelayAsync(StartupDelay, stoppingToken))
            return;

        if (_options.RunOnStartup)
            await RunOnceAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            if (!await DelayAsync(TimeUntilNextRun(DateTime.UtcNow), stoppingToken))
                break;

            await RunOnceAsync(stoppingToken);
        }
    }

    /// <summary>Espera sem estourar no shutdown. Devolve false quando é hora de parar.</summary>
    private static async Task<bool> DelayAsync(TimeSpan wait, CancellationToken token)
    {
        try
        {
            await Task.Delay(wait, token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task RunOnceAsync(CancellationToken token)
    {
        try
        {
            var queued = await QueueRemindersAsync(token);

            if (queued > 0)
                _logger.LogInformation("{Count} lembrete(s) de mensalidade na fila.", queued);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            // Perder a rodada de hoje é aceitável: a chave de idempotência garante que a de
            // amanhã ainda avisa quem não foi avisado, sem repetir quem já foi.
            _logger.LogError(ex, "Erro ao montar os lembretes de mensalidade.");
        }
    }

    /// <summary>Próxima ocorrência da hora configurada, hoje se ainda não passou, senão amanhã.</summary>
    private TimeSpan TimeUntilNextRun(DateTime nowUtc)
    {
        var hour = Math.Clamp(_options.RunAtHourUtc, 0, 23);
        var next = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, hour, 0, 0, DateTimeKind.Utc);

        if (next <= nowUtc)
            next = next.AddDays(1);

        return next - nowUtc;
    }

    private async Task<int> QueueRemindersAsync(CancellationToken token)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationsDataContext>();

        var today = DateTime.UtcNow.Date;
        var month = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var daysBefore = Math.Max(0, _options.DaysBefore);

        var plans = await context
            .StudentPlans
            .AsNoTracking()
            .Where(x => x.Status == PlanStatus.Active && x.Student.IsActive)
            .Select(x => new
            {
                x.Id,
                x.DueDay,
                x.MonthlyValue,
                x.Student.Name,
                x.Student.Email
            })
            .ToListAsync(token);

        if (plans.Count == 0)
            return 0;

        var paidPlanIds = await context
            .Payments
            .AsNoTracking()
            .Where(x => x.ReferenceMonth == month)
            .Select(x => x.PlanId)
            .ToListAsync(token);

        var paid = paidPlanIds.ToHashSet();
        var due = new List<(string Key, string Name, string Email, decimal Value, DateTime DueDate)>();

        foreach (var plan in plans)
        {
            if (paid.Contains(plan.Id))
                continue;

            // O plano guarda 1 a 28, mas um valor fora disso não pode explodir o agendador.
            var day = Math.Clamp(plan.DueDay, 1, DateTime.DaysInMonth(today.Year, today.Month));
            var dueDate = new DateTime(today.Year, today.Month, day, 0, 0, 0, DateTimeKind.Utc);
            var daysUntil = (dueDate - today).Days;

            // Só a janela antes do vencimento. Quem já venceu é conversa da professora, não robô.
            if (daysUntil < 0 || daysUntil > daysBefore)
                continue;

            due.Add(($"billing:{plan.Id}:{month:yyyy-MM}", plan.Name, plan.Email, plan.MonthlyValue, dueDate));
        }

        if (due.Count == 0)
            return 0;

        // A chave única no banco já barra repetição, mas conferir antes evita gastar uma
        // transação inteira só para ela quebrar.
        var keys = due.Select(x => x.Key).ToList();
        var alreadyQueued = await context
            .Notifications
            .AsNoTracking()
            .Where(x => x.DedupeKey != null && keys.Contains(x.DedupeKey))
            .Select(x => x.DedupeKey!)
            .ToListAsync(token);

        var known = alreadyQueued.ToHashSet();
        var fresh = due
            .Where(x => !known.Contains(x.Key))
            .Select(x => new Notification
            {
                Kind = NotificationKind.BillingReminder,
                ToName = x.Name,
                ToEmail = x.Email,
                Subject = "Sua mensalidade está chegando",
                Body = BuildBody(x.Name, x.Value, x.DueDate),
                DedupeKey = x.Key
            })
            .ToList();

        if (fresh.Count == 0)
            return 0;

        context.Notifications.AddRange(fresh);

        try
        {
            await context.SaveChangesAsync(token);
        }
        catch (DbUpdateException ex)
        {
            // Dois workers rodando ao mesmo tempo esbarram na chave única. O outro já enfileirou.
            _logger.LogWarning(ex, "Lembretes já enfileirados por outra rodada; nada a fazer.");
            return 0;
        }

        return fresh.Count;
    }

    private static string BuildBody(string name, decimal value, DateTime dueDate)
    {
        var money = value.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));

        return $"<p>Olá, {name}!</p>" +
               $"<p>Sua mensalidade de <b>{money}</b> vence em <b>{dueDate:dd/MM/yyyy}</b>.</p>" +
               "<p>Se já pagou, pode ignorar este aviso.</p>";
    }
}
