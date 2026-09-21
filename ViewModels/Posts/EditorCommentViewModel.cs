using System.ComponentModel.DataAnnotations;

namespace Lingua.ViewModels.Posts;

public class EditorCommentViewModel
{
    [StringLength(2000, ErrorMessage = "O comentário deve ter no máximo 2000 caracteres")]
    public string Body { get; set; } = string.Empty;

    /// <summary>Comentário que está sendo respondido, quando for uma resposta.</summary>
    public int? ParentId { get; set; }

    /// <summary>GIF ou imagem anexada.</summary>
    public string? MediaUrl { get; set; }
}
