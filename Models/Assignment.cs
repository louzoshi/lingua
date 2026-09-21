namespace Lingua.Models;

/// <summary>Trabalho passado para uma turma, com o link do enunciado e prazo.</summary>
public class Assignment
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Instructions { get; set; }

    /// <summary>Link do enunciado ou do formulário onde o trabalho é feito.</summary>
    public string? Url { get; set; }

    public int ClassroomId { get; set; }
    public Classroom Classroom { get; set; } = null!;

    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public IList<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
}
