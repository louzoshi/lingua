namespace Lingua.ViewModels.Assignments;

public class ListAssignmentsViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public string? Url { get; set; }
    public int ClassroomId { get; set; }
    public string Classroom { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }

    public int Submissions { get; set; }
    public bool SubmittedByMe { get; set; }
    public decimal? MyGrade { get; set; }
}
