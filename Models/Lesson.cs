namespace Lingua.Models;

/// <summary>Aula de uma turma: primeiro agendada com link de encontro, depois com a gravação.</summary>
public class Lesson
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }

    public int ClassroomId { get; set; }
    public Classroom Classroom { get; set; } = null!;

    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 60;

    /// <summary>Link do encontro ao vivo.</summary>
    public string? MeetingUrl { get; set; }

    /// <summary>Link da gravação, preenchido pelo professor depois da aula.</summary>
    public string? RecordingUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public IList<LessonAttendance> Attendances { get; set; } = new List<LessonAttendance>();
}
