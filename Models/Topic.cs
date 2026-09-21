namespace Lingua.Models;

/// <summary>Assunto de um post (Grammar, Culture, Free Talk...). Serve para filtrar o mural.</summary>
public class Topic
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;

    public IList<Post> Posts { get; set; } = new List<Post>();
}
