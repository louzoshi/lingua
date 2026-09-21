namespace Lingua.Models;

/// <summary>Conversa privada entre dois usuários da plataforma.</summary>
public class Conversation
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    public IList<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public IList<DirectMessage> Messages { get; set; } = new List<DirectMessage>();
}
