using Lingua.Models;

namespace Lingua.ViewModels.Posts;

public class CommentViewModel
{
    public int Id { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public ModerationStatus Status { get; set; }

    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorSlug { get; set; } = string.Empty;
    public string? AuthorImage { get; set; }
}
