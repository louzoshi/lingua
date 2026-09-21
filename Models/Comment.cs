namespace Lingua.Models;

public class Comment
{
    public int Id { get; set; }
    public string Body { get; set; } = null!;

    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    public int AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public ModerationStatus Status { get; set; } = ModerationStatus.Published;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
