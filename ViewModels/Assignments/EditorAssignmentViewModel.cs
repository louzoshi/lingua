using System.ComponentModel.DataAnnotations;

namespace Lingua.ViewModels.Assignments;

public class EditorAssignmentViewModel
{
    [Required(ErrorMessage = "O título é obrigatório")]
    [StringLength(160, MinimumLength = 3, ErrorMessage = "O título deve ter entre 3 e 160 caracteres")]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Instructions { get; set; }

    [Url(ErrorMessage = "O link do trabalho é inválido")]
    public string? Url { get; set; }

    [Required(ErrorMessage = "Informe a turma")]
    public int ClassroomId { get; set; }

    public DateTime? DueDate { get; set; }
}
