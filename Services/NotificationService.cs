using Lingua.Data;
using Lingua.Models;

namespace Lingua.Services;

/// <summary>
/// Ponta de escrita da fila de saída. A plataforma não fala com servidor de e-mail: ela
/// grava a notificação e devolve a tela para quem clicou. Entregar é problema do worker
/// <c>Lingua.Notifications</c>.
/// <para>
/// <see cref="Queue"/> só marca a linha no contexto, sem salvar, porque o serviço recebe o
/// mesmo <see cref="LinguaDataContext"/> do escopo de quem chamou: o <c>SaveChanges</c> que
/// grava o aluno grava também o convite dele, os dois ou nenhum.
/// </para>
/// </summary>
public class NotificationService
{
    private readonly LinguaDataContext _context;

    public NotificationService(LinguaDataContext context)
        => _context = context;

    /// <summary>Enfileira sem salvar. Quem chamou decide quando fechar a transação.</summary>
    public Notification Queue(
        NotificationKind kind,
        string toName,
        string toEmail,
        string subject,
        string body,
        string? dedupeKey = null)
    {
        var notification = new Notification
        {
            Kind = kind,
            ToName = toName,
            ToEmail = toEmail,
            Subject = subject,
            Body = body,
            DedupeKey = dedupeKey
        };

        _context.Notifications.Add(notification);

        return notification;
    }

    /// <summary>Enfileira o convite com a senha inicial. O texto do e-mail mora aqui, em um lugar só.</summary>
    public Notification QueueStudentInvite(User user, string password)
        => Queue(
            NotificationKind.StudentInvite,
            user.Name,
            user.Email,
            "Welcome to Lingua!",
            $"<p>Olá, {user.Name}!</p><p>Sua conta foi criada. Entre com <b>{user.Email}</b> " +
            $"e a senha <b>{password}</b>, e troque a senha no primeiro acesso.</p>");
}
