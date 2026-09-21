namespace Lingua.ViewModels.Messages;

/// <summary>Uma conversa vista pela professora na moderação: quem fala com quem e se há alerta.</summary>
public class ConversationOverviewViewModel
{
    public int Id { get; set; }
    public List<string> Participants { get; set; } = new();
    public int Messages { get; set; }
    public int Flagged { get; set; }
    public string? LastMessage { get; set; }
    public DateTime LastMessageAt { get; set; }
}
