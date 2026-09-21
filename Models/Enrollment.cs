namespace Lingua.Models;

/// <summary>Matrícula de um aluno em uma turma.</summary>
public class Enrollment
{
    public int Id { get; set; }

    public int ClassroomId { get; set; }
    public Classroom Classroom { get; set; } = null!;

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
