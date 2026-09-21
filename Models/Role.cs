namespace Lingua.Models;

public class Role
{
    /// <summary>Slugs usados nas policies de autorização.</summary>
    public const string Student = "student";
    public const string Teacher = "teacher";
    public const string Admin = "admin";

    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;

    public IList<User> Users { get; set; } = new List<User>();
}
