using System.ComponentModel.DataAnnotations;

namespace Lingua.ViewModels.Posts;

public class EditorTopicViewModel
{
    [Required(ErrorMessage = "O nome é obrigatório")]
    [StringLength(80, MinimumLength = 3, ErrorMessage = "O nome deve ter entre 3 e 80 caracteres")]
    public string Name { get; set; } = string.Empty;
}
