using System.ComponentModel.DataAnnotations;

namespace Lingua.ViewModels.Messages;

public class StartConversationViewModel
{
    [Required(ErrorMessage = "Informe com quem você quer conversar")]
    public int UserId { get; set; }
}
