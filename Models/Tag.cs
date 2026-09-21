namespace Lingua.Models;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;

    public IList<Post> Posts { get; set; } = new List<Post>();
}
