using Lingua.Data;
using Lingua.Models;
using Lingua.ViewModels.Messages;
using Microsoft.EntityFrameworkCore;

namespace Lingua.Services;

public class ConversationService
{
    private readonly LinguaDataContext _context;
    private readonly AccessService _access;
    private readonly InteractionService _interactions;
    private readonly ChatNotifier _notifier;

    public ConversationService(
        LinguaDataContext context,
        AccessService access,
        InteractionService interactions,
        ChatNotifier notifier)
    {
        _context = context;
        _access = access;
        _interactions = interactions;
        _notifier = notifier;
    }

    /// <summary>Devolve a conversa entre os dois usuários, criando-a na primeira mensagem.</summary>
    public async Task<Conversation?> GetOrCreateAsync(int userId, int otherUserId)
    {
        if (!await _access.CanMessageAsync(userId, otherUserId))
            return null;

        var existing = await _context.Conversations
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x =>
                x.Participants.Count == 2 &&
                x.Participants.Any(p => p.UserId == userId) &&
                x.Participants.Any(p => p.UserId == otherUserId));

        if (existing != null)
            return existing;

        var conversation = new Conversation
        {
            Participants = new List<ConversationParticipant>
            {
                new() { UserId = userId },
                new() { UserId = otherUserId }
            }
        };

        await _context.Conversations.AddAsync(conversation);
        await _context.SaveChangesAsync();

        return conversation;
    }

    public async Task<bool> IsParticipantAsync(int conversationId, int userId)
        => await _context.ConversationParticipants
            .AsNoTracking()
            .AnyAsync(x => x.ConversationId == conversationId && x.UserId == userId);

    public async Task<DirectMessage?> SendAsync(int conversationId, int senderId, string body)
    {
        if (!await IsParticipantAsync(conversationId, senderId))
            return null;

        var conversation = await _context.Conversations.FirstOrDefaultAsync(x => x.Id == conversationId);
        if (conversation == null)
            return null;

        var message = new DirectMessage
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Body = body.Trim(),
            SentAt = DateTime.UtcNow
        };

        conversation.LastMessageAt = message.SentAt;

        await _context.DirectMessages.AddAsync(message);
        _interactions.Track(senderId, InteractionType.DirectMessageSent, referenceId: conversationId);
        await _context.SaveChangesAsync();

        await _context.Entry(message).Reference(x => x.Sender).LoadAsync();

        var participants = await _context
            .ConversationParticipants
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId)
            .Select(x => x.UserId)
            .ToListAsync();

        _notifier.Publish(new ChatNotification(ToViewModel(message), participants));

        return message;
    }

    public static DirectMessageViewModel ToViewModel(DirectMessage message)
        => new()
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            Body = message.Body,
            SentAt = message.SentAt,
            SenderId = message.SenderId,
            SenderName = message.Sender.Name,
            SenderImage = message.Sender.Image,
            IsFlagged = message.IsFlagged
        };

    public async Task MarkAsReadAsync(int conversationId, int userId)
    {
        var participant = await _context.ConversationParticipants
            .FirstOrDefaultAsync(x => x.ConversationId == conversationId && x.UserId == userId);

        if (participant == null)
            return;

        participant.LastReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    /// <summary>Ids dos outros participantes, para avisar pelo hub quem precisa receber a mensagem.</summary>
    public async Task<List<int>> RecipientsAsync(int conversationId, int senderId)
        => await _context.ConversationParticipants
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId && x.UserId != senderId)
            .Select(x => x.UserId)
            .ToListAsync();
}
