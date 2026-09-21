using System.Net;
using System.Net.Mail;

namespace Lingua.Services;

public class EmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
        => _logger = logger;

    public bool Send(string toName, string toEmail, string subject, string body)
    {
        var smtp = Configuration.Smtp;

        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            // Sem SMTP configurado a plataforma continua funcionando; o convite fica no log.
            _logger.LogWarning("SMTP não configurado. E-mail para {Email} não foi enviado.", toEmail);
            return false;
        }

        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            Credentials = new NetworkCredential(smtp.UserName, smtp.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            EnableSsl = true
        };

        using var mail = new MailMessage
        {
            From = new MailAddress(smtp.FromEmail, smtp.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        mail.To.Add(new MailAddress(toEmail, toName));

        try
        {
            client.Send(mail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail para {Email}.", toEmail);
            return false;
        }
    }
}
