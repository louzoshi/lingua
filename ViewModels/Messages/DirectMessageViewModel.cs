using Lingua.Models;

namespace Lingua.ViewModels.Messages;

public class DirectMessageViewModel
{
    public int Id { get; set; }
    public int ConversationId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }

    public int SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderImage { get; set; }
    public string? MediaUrl { get; set; }
    public bool IsFlagged { get; set; }
    public ModerationStatus Status { get; set; } = ModerationStatus.Published;
}
