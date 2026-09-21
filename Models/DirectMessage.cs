namespace Lingua.Models;

public class DirectMessage
{
    public int Id { get; set; }
    public string Body { get; set; } = null!;

    public int ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public int SenderId { get; set; }
    public User Sender { get; set; } = null!;

    /// <summary>GIF ou imagem enviada junto com a mensagem, quando houver.</summary>
    public string? MediaUrl { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public ModerationStatus Status { get; set; } = ModerationStatus.Published;

    /// <summary>Marcada por um aluno para o professor revisar.</summary>
    public bool IsFlagged { get; set; }
}
