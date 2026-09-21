namespace Lingua.Models;

/// <summary>Uma turma da escola: tem um professor, alunos matriculados e seu próprio mural.</summary>
public class Classroom
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Description { get; set; }
    public EnglishLevel Level { get; set; } = EnglishLevel.A1;

    public int TeacherId { get; set; }
    public User Teacher { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsArchived { get; set; }

    public IList<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public IList<Post> Posts { get; set; } = new List<Post>();
    public IList<Lesson> Lessons { get; set; } = new List<Lesson>();
    public IList<Assignment> Assignments { get; set; } = new List<Assignment>();
}
