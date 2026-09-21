namespace Lingua.Models;

/// <summary>
/// Publicação em um mural. Sem <see cref="ClassroomId"/> o post vai para o mural geral,
/// visível a toda a escola; com turma, fica restrito aos matriculados nela.
/// </summary>
public class Post
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Summary { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string Slug { get; set; } = null!;

    public PostScope Scope { get; set; } = PostScope.General;
    public ModerationStatus Status { get; set; } = ModerationStatus.Published;

    public int? ClassroomId { get; set; }
    public Classroom? Classroom { get; set; }

    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public int? TopicId { get; set; }
    public Topic? Topic { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdateDate { get; set; } = DateTime.UtcNow;

    public IList<Tag> Tags { get; set; } = new List<Tag>();
    public IList<Comment> Comments { get; set; } = new List<Comment>();
    public IList<Reaction> Reactions { get; set; } = new List<Reaction>();
    public IList<PostMedia> Media { get; set; } = new List<PostMedia>();
}
