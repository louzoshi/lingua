using Lingua.Models;

namespace Lingua.ViewModels.Posts;

public class ListPostsViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime LastUpdateDate { get; set; }
    public ModerationStatus Status { get; set; }

    public string AuthorName { get; set; } = string.Empty;
    public string AuthorSlug { get; set; } = string.Empty;
    public string? AuthorImage { get; set; }

    /// <summary>Nome da turma, ou nulo quando o post é do mural geral.</summary>
    public string? Classroom { get; set; }
    public int? ClassroomId { get; set; }
    public string? Topic { get; set; }

    public int Comments { get; set; }
    public int Reactions { get; set; }
    public bool ReactedByMe { get; set; }
}
