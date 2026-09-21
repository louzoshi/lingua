namespace Lingua.Models;

/// <summary>Registro de que um aluno participou de uma aula.</summary>
public class LessonAttendance
{
    public int Id { get; set; }

    public int LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    public DateTime AttendedAt { get; set; } = DateTime.UtcNow;
}
