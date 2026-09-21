namespace Lingua.Models;

/// <summary>
/// Evento pontuado que alimenta o ranking de interação. Guardado como histórico para que
/// o ranking possa ser recortado por turma e por período.
/// </summary>
public class InteractionEvent
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public InteractionType Type { get; set; }
    public int Points { get; set; }

    /// <summary>Turma a que o evento pertence, quando houver.</summary>
    public int? ClassroomId { get; set; }
    public Classroom? Classroom { get; set; }

    /// <summary>Id do registro que originou o evento (post, comentário, aula...).</summary>
    public int? ReferenceId { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
