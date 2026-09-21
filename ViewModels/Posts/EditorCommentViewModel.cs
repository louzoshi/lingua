using System.ComponentModel.DataAnnotations;

namespace Lingua.ViewModels.Posts;

public class EditorCommentViewModel
{
    [Required(ErrorMessage = "Escreva algo antes de enviar")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "O comentário deve ter no máximo 2000 caracteres")]
    public string Body { get; set; } = string.Empty;
}
