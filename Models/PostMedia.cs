namespace Lingua.Models;

/// <summary>Imagem, vídeo ou GIF anexado a um post. O arquivo fica em <c>wwwroot/uploads</c>.</summary>
public class PostMedia
{
    public int Id { get; set; }

    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    public string Url { get; set; } = null!;
    public MediaKind Kind { get; set; } = MediaKind.Image;

    /// <summary>Ordem de exibição dentro do post.</summary>
    public int Position { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
