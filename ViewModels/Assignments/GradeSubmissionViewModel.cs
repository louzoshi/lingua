using System.ComponentModel.DataAnnotations;

namespace Lingua.ViewModels.Assignments;

public class GradeSubmissionViewModel
{
    [Range(0, 100, ErrorMessage = "A nota deve ficar entre 0 e 100")]
    public decimal? Grade { get; set; }

    [StringLength(2000)]
    public string? Feedback { get; set; }
}
