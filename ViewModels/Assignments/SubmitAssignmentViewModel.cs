using System.ComponentModel.DataAnnotations;

namespace Lingua.ViewModels.Assignments;

public class SubmitAssignmentViewModel
{
    [Required(ErrorMessage = "Informe o link do trabalho")]
    [Url(ErrorMessage = "O link é inválido")]
    public string Url { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Notes { get; set; }
}
