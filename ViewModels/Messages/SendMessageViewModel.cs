using System.ComponentModel.DataAnnotations;

namespace Lingua.ViewModels.Messages;

public class SendMessageViewModel
{
    [StringLength(4000, ErrorMessage = "A mensagem deve ter no máximo 4000 caracteres")]
    public string Body { get; set; } = string.Empty;

    public string? MediaUrl { get; set; }
}
