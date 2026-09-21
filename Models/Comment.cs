namespace Lingua.Models;

public class Comment
{
    public int Id { get; set; }
    public string Body { get; set; } = null!;

    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;

    /// <summary>Comentário ao qual este responde. Nulo quando é um comentário raiz do post.</summary>
    public int? ParentId { get; set; }
    public Comment? Parent { get; set; }
    public IList<Comment> Replies { get; set; } = new List<Comment>();

    /// <summary>GIF ou imagem anexada ao comentário, quando houver.</summary>
    public string? MediaUrl { get; set; }

    public ModerationStatus Status { get; set; } = ModerationStatus.Published;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; set; }
}
