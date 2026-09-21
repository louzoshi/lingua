using System.ComponentModel.DataAnnotations;

namespace Lingua.ViewModels.Study;

public class EditorLessonViewModel
{
    [Required(ErrorMessage = "O título é obrigatório")]
    [StringLength(160, MinimumLength = 3, ErrorMessage = "O título deve ter entre 3 e 160 caracteres")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Informe a turma")]
    public int ClassroomId { get; set; }

    [Required(ErrorMessage = "Informe a data da aula")]
    public DateTime ScheduledAt { get; set; } = DateTime.UtcNow.AddDays(1);

    [Range(15, 300, ErrorMessage = "A duração deve ficar entre 15 e 300 minutos")]
    public int DurationMinutes { get; set; } = 60;

    [Url(ErrorMessage = "O link do encontro é inválido")]
    public string? MeetingUrl { get; set; }

    [Url(ErrorMessage = "O link da gravação é inválido")]
    public string? RecordingUrl { get; set; }
}
