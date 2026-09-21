namespace Lingua.Models;

public class AssignmentSubmission
{
    public int Id { get; set; }

    public int AssignmentId { get; set; }
    public Assignment Assignment { get; set; } = null!;

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    /// <summary>Link do trabalho entregue pelo aluno.</summary>
    public string Url { get; set; } = null!;
    public string? Notes { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public decimal? Grade { get; set; }
    public string? Feedback { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
