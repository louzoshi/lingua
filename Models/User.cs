namespace Lingua.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Image { get; set; }
    public string? Bio { get; set; }

    /// <summary>País ou cidade, usado no perfil para os alunos se apresentarem.</summary>
    public string? Location { get; set; }

    public EnglishLevel Level { get; set; } = EnglishLevel.A1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public IList<Role> Roles { get; set; } = new List<Role>();
    public IList<Post> Posts { get; set; } = new List<Post>();
    public IList<Comment> Comments { get; set; } = new List<Comment>();
    public IList<Enrollment> Enrollments { get; set; } = new List<Enrollment>();

    /// <summary>Turmas em que este usuário é o professor responsável.</summary>
    public IList<Classroom> TeachingClassrooms { get; set; } = new List<Classroom>();
}
