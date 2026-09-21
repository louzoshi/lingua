using Lingua.Models;

namespace Lingua.ViewModels.Posts;

public class PostDetailsViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdateDate { get; set; }
    public ModerationStatus Status { get; set; }

    public int AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorSlug { get; set; } = string.Empty;
    public string? AuthorImage { get; set; }

    public int? ClassroomId { get; set; }
    public string? Classroom { get; set; }
    public string? Topic { get; set; }

    public List<string> Tags { get; set; } = new();
    public List<CommentViewModel> Comments { get; set; } = new();

    public int Reactions { get; set; }
    public bool ReactedByMe { get; set; }
    public bool CanEdit { get; set; }
    public bool CanModerate { get; set; }
}
