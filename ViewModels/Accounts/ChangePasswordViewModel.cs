using System.ComponentModel.DataAnnotations;

namespace Lingua.ViewModels.Accounts;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Informe a senha atual")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a nova senha")]
    [StringLength(60, MinimumLength = 8, ErrorMessage = "A nova senha deve ter ao menos 8 caracteres")]
    public string NewPassword { get; set; } = string.Empty;
}
