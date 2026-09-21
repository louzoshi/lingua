using System.ComponentModel.DataAnnotations;
using Lingua.Models;

namespace Lingua.ViewModels.Accounts;

public class UpdateProfileViewModel
{
    [Required(ErrorMessage = "O nome é obrigatório")]
    [StringLength(80, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 80 caracteres")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "A bio deve ter no máximo 1000 caracteres")]
    public string? Bio { get; set; }

    [StringLength(120)]
    public string? Location { get; set; }

    public EnglishLevel Level { get; set; } = EnglishLevel.A1;
}
