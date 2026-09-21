using System.ComponentModel.DataAnnotations;

namespace Lingua.ViewModels.Finance;

public class EditorPackageViewModel
{
    [Required(ErrorMessage = "O nome é obrigatório")]
    [StringLength(80, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 80 caracteres")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(0, 100000, ErrorMessage = "Informe um valor válido")]
    public decimal MonthlyPrice { get; set; }

    [Range(1, 14, ErrorMessage = "Entre 1 e 14 aulas por semana")]
    public int LessonsPerWeek { get; set; } = 1;
}
