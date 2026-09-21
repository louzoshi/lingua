using System.ComponentModel.DataAnnotations;

namespace Lingua.ViewModels.Posts;

public class EditorPostViewModel
{
    [Required(ErrorMessage = "O título é obrigatório")]
    [StringLength(160, MinimumLength = 3, ErrorMessage = "O título deve ter entre 3 e 160 caracteres")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "O resumo é obrigatório")]
    [StringLength(255, MinimumLength = 3, ErrorMessage = "O resumo deve ter entre 3 e 255 caracteres")]
    public string Summary { get; set; } = string.Empty;

    [Required(ErrorMessage = "O texto é obrigatório")]
    public string Body { get; set; } = string.Empty;

    /// <summary>Nulo publica no mural geral; preenchido publica no mural da turma.</summary>
    public int? ClassroomId { get; set; }

    public int? TopicId { get; set; }

    /// <summary>Tags separadas por vírgula, criadas na hora se ainda não existirem.</summary>
    public string? Tags { get; set; }

    /// <summary>Mídia já enviada para o servidor, na ordem em que deve aparecer.</summary>
    public List<MediaItem> Media { get; set; } = new();

    public class MediaItem
    {
        public string Url { get; set; } = string.Empty;
        public Models.MediaKind Kind { get; set; }
    }
}
