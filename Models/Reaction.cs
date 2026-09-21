namespace Lingua.Models;

/// <summary>Curtida de um aluno em um post. Um aluno reage no máximo uma vez por post.</summary>
public class Reaction
{
    public int Id { get; set; }

    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
