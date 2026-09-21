using System.ComponentModel.DataAnnotations;
using Lingua.Models;

namespace Lingua.ViewModels.Classrooms;

public class EditorClassroomViewModel
{
    [Required(ErrorMessage = "O nome da turma é obrigatório")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 120 caracteres")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public EnglishLevel Level { get; set; } = EnglishLevel.A1;
}
