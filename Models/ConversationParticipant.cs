namespace Lingua.Models;

public class ConversationParticipant
{
    public int Id { get; set; }

    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Marca d'água de leitura: mensagens posteriores a isso contam como não lidas.</summary>
    public DateTime LastReadAt { get; set; } = DateTime.MinValue;
}
