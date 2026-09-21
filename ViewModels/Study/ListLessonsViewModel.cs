namespace Lingua.ViewModels.Study;

public class ListLessonsViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ClassroomId { get; set; }
    public string Classroom { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public string? MeetingUrl { get; set; }
    public string? RecordingUrl { get; set; }

    public int Attendees { get; set; }

    /// <summary>Se o aluno que está olhando participou desta aula.</summary>
    public bool AttendedByMe { get; set; }
}
