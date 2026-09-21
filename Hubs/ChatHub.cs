using Lingua.Extensions;
using Lingua.Services;
using Lingua.ViewModels.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Lingua.Hubs;

/// <summary>
/// Entrega as DMs em tempo real. Cada usuário entra em um grupo com o próprio id, então
/// uma mensagem só chega a quem participa da conversa.
/// </summary>
[Authorize(AuthenticationSchemes = Policies.JwtScheme)]
public class ChatHub : Hub
{
    private readonly ConversationService _conversations;

    public ChatHub(ConversationService conversations)
        => _conversations = conversations;

    public static string GroupFor(int userId) => $"user-{userId}";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.GetUserId() ?? 0;

        if (userId > 0)
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupFor(userId));

        await base.OnConnectedAsync();
    }

    public async Task SendMessage(int conversationId, string body)
    {
        var senderId = Context.User?.GetUserId() ?? 0;
        if (senderId == 0 || string.IsNullOrWhiteSpace(body))
            return;

        var message = await _conversations.SendAsync(conversationId, senderId, body);
        if (message == null)
            return;

        var payload = ConversationService.ToViewModel(message);

        var recipients = await _conversations.RecipientsAsync(conversationId, senderId);
        var groups = recipients.Select(GroupFor).Append(GroupFor(senderId)).ToList();

        await Clients.Groups(groups).SendAsync("ReceiveMessage", payload);
    }
}
