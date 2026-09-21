using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Lingua.Notifications.Services;

/// <summary>Resultado de uma tentativa de entrega.</summary>
public enum DeliveryOutcome
{
    Sent,

    /// <summary>Sem SMTP configurado. Não é falha da mensagem: ela espera sem gastar tentativa.</summary>
    NotConfigured,

    /// <summary>O servidor recusou ou não respondeu. Vale nova tentativa.</summary>
    Failed
}

/// <summary>
/// O único ponto da plataforma que abre conexão com servidor de e-mail. Antes isso acontecia
/// dentro da requisição do app; agora só aqui, onde demorar não trava ninguém.
/// </summary>
public class EmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IOptions<SmtpOptions> options, ILogger<EmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task<(DeliveryOutcome Outcome, string? Error)> SendAsync(
        string toName,
        string toEmail,
        string subject,
        string body,
        CancellationToken token)
    {
        if (!_options.IsConfigured)
            return (DeliveryOutcome.NotConfigured, null);

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            Credentials = new NetworkCredential(_options.UserName, _options.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            EnableSsl = _options.EnableSsl
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        mail.To.Add(new MailAddress(toEmail, toName));

        try
        {
            await client.SendMailAsync(mail, token);
            return (DeliveryOutcome.Sent, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Falha ao entregar e-mail para {Email}.", toEmail);
            return (DeliveryOutcome.Failed, Truncate(ex.Message, 500));
        }
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
