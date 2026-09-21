using Lingua.Models;

namespace Lingua.ViewModels.Students;

/// <summary>Linha da tabela de alunos na tela de gerenciar: quem é, onde está e se tem acesso.</summary>
public class StudentOverviewViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Image { get; set; }
    public EnglishLevel Level { get; set; }
    public bool IsActive { get; set; }
    public DateTime? DeactivatedAt { get; set; }
    public DateTime LastSeenAt { get; set; }

    public List<EnrolledClassroom> Classrooms { get; set; } = new();
    public int Posts { get; set; }
    public int Comments { get; set; }

    public class EnrolledClassroom
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
