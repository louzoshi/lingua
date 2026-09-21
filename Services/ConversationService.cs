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

    public async Task<DirectMessage?> SendAsync(int conversationId, int senderId, string body, string? mediaUrl = null)
    {
        if (!await IsParticipantAsync(conversationId, senderId))
            return null;

        if (string.IsNullOrWhiteSpace(body) && string.IsNullOrWhiteSpace(mediaUrl))
            return null;

        var conversation = await _context.Conversations.FirstOrDefaultAsync(x => x.Id == conversationId);
        if (conversation == null)
            return null;

        var message = new DirectMessage
        {
            ConversationId = conversationId,
            SenderId = senderId,
            Body = body.Trim(),
            MediaUrl = mediaUrl,
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
            MediaUrl = message.MediaUrl,
            IsFlagged = message.IsFlagged,
            Status = message.Status
        };

    /// <summary>
    /// Mensagens de uma conversa. O participante lê a própria; a professora lê qualquer uma —
    /// é o que permite intervir quando há problema entre alunos.
    /// </summary>
    public async Task<List<DirectMessageViewModel>?> MessagesAsync(int conversationId, int userId)
    {
        var isStaff = await _access.IsStaffAsync(userId);

        if (!isStaff && !await IsParticipantAsync(conversationId, userId))
            return null;

        return await _context.DirectMessages
            .AsNoTracking()
            .Where(x => x.ConversationId == conversationId && (isStaff || x.Status != ModerationStatus.Hidden))
            .OrderBy(x => x.SentAt)
            .Select(x => new DirectMessageViewModel
            {
                Id = x.Id,
                ConversationId = x.ConversationId,
                Body = x.Body,
                MediaUrl = x.MediaUrl,
                SentAt = x.SentAt,
                SenderId = x.SenderId,
                SenderName = x.Sender.Name,
                SenderImage = x.Sender.Image,
                IsFlagged = x.IsFlagged,
                Status = x.Status
            })
            .ToListAsync();
    }

    /// <summary>Aluno pede que a professora olhe uma mensagem que recebeu.</summary>
    public async Task<bool> FlagAsync(int messageId, int userId)
    {
        var message = await _context.DirectMessages.FirstOrDefaultAsync(x => x.Id == messageId);
        if (message == null || !await IsParticipantAsync(message.ConversationId, userId))
            return false;

        message.IsFlagged = true;
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>Professora esconde uma DM ofensiva; ela continua no histórico da moderação.</summary>
    public async Task SetMessageStatusAsync(int messageId, ModerationStatus status)
    {
        var message = await _context.DirectMessages.FirstOrDefaultAsync(x => x.Id == messageId);
        if (message == null)
            return;

        message.Status = status;
        message.IsFlagged = false;
        await _context.SaveChangesAsync();
    }

    /// <summary>Todas as conversas da escola, para a professora acompanhar.</summary>
    public async Task<List<ConversationOverviewViewModel>> AllConversationsAsync()
        => await _context.Conversations
            .AsNoTracking()
            .Where(x => x.Messages.Any())
            .OrderByDescending(x => x.LastMessageAt)
            .Select(x => new ConversationOverviewViewModel
            {
                Id = x.Id,
                LastMessageAt = x.LastMessageAt,
                Participants = x.Participants.Select(p => p.User.Name).ToList(),
                Messages = x.Messages.Count,
                Flagged = x.Messages.Count(m => m.IsFlagged),
                LastMessage = x.Messages.OrderByDescending(m => m.SentAt).Select(m => m.Body).FirstOrDefault()
            })
            .ToListAsync();

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
