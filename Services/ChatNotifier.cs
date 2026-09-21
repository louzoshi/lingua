using Lingua.ViewModels.Messages;

namespace Lingua.Services;

/// <param name="Message">A mensagem recém-gravada.</param>
/// <param name="Participants">Ids de quem participa da conversa; ninguém mais deve receber.</param>
public record ChatNotification(DirectMessageViewModel Message, IReadOnlyCollection<int> Participants);

/// <summary>
/// Avisa os circuitos Blazor abertos neste servidor que chegou mensagem nova.
/// <para>
/// A página de mensagens roda no próprio servidor, então não faz sentido ela abrir um
/// cliente SignalR contra o hub local: basta escutar aqui. O <see cref="Hubs.ChatHub"/>
/// continua existindo para clientes externos que consomem a API.
/// </para>
/// </summary>
public class ChatNotifier
{
    public event Action<ChatNotification>? MessageSent;

    public void Publish(ChatNotification notification)
        => MessageSent?.Invoke(notification);
}
