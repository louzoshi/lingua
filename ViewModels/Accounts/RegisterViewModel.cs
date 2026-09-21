using System.ComponentModel.DataAnnotations;
using Lingua.Models;

namespace Lingua.ViewModels.Accounts;

/// <summary>Convite de aluno, enviado pelo professor. A senha inicial vai por e-mail.</summary>
public class RegisterViewModel
{
    [Required(ErrorMessage = "O nome é obrigatório")]
    [StringLength(80, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 80 caracteres")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório")]
    [EmailAddress(ErrorMessage = "O e-mail é inválido")]
    public string Email { get; set; } = string.Empty;

    public EnglishLevel Level { get; set; } = EnglishLevel.A1;

    /// <summary>Turma em que o aluno já entra matriculado, quando houver.</summary>
    public int? ClassroomId { get; set; }
}
