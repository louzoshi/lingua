namespace Lingua.Models;

/// <summary>Material da área de estudo. Sem turma, vale para a escola inteira.</summary>
public class StudyResource
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string Url { get; set; } = null!;
    public ResourceKind Kind { get; set; } = ResourceKind.Video;
    public EnglishLevel Level { get; set; } = EnglishLevel.A1;

    public int? ClassroomId { get; set; }
    public Classroom? Classroom { get; set; }

    public int CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
