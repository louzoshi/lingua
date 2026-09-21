namespace Lingua.ViewModels.Messages;

public class ConversationSummaryViewModel
{
    public int Id { get; set; }
    public int OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string OtherUserSlug { get; set; } = string.Empty;
    public string? OtherUserImage { get; set; }

    public string? LastMessage { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}
