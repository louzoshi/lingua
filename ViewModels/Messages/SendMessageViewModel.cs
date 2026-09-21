using System.ComponentModel.DataAnnotations;

namespace Lingua.ViewModels.Messages;

public class SendMessageViewModel
{
    [Required(ErrorMessage = "Escreva algo antes de enviar")]
    [StringLength(4000, MinimumLength = 1, ErrorMessage = "A mensagem deve ter no máximo 4000 caracteres")]
    public string Body { get; set; } = string.Empty;
}
