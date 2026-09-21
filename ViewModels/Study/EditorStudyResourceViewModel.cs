using System.ComponentModel.DataAnnotations;
using Lingua.Models;

namespace Lingua.ViewModels.Study;

public class EditorStudyResourceViewModel
{
    [Required(ErrorMessage = "O título é obrigatório")]
    [StringLength(160, MinimumLength = 3, ErrorMessage = "O título deve ter entre 3 e 160 caracteres")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "O link é obrigatório")]
    [Url(ErrorMessage = "O link é inválido")]
    public string Url { get; set; } = string.Empty;

    public ResourceKind Kind { get; set; } = ResourceKind.Video;
    public EnglishLevel Level { get; set; } = EnglishLevel.A1;

    /// <summary>Nulo deixa o material disponível para a escola inteira.</summary>
    public int? ClassroomId { get; set; }
}
