namespace Lingua.Models;

/// <summary>Pacote comercial que a escola vende: nome, preço de tabela e o que inclui.</summary>
public class Package
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>Preço de tabela por mês. O valor efetivo fica no plano de cada aluno.</summary>
    public decimal MonthlyPrice { get; set; }

    public int LessonsPerWeek { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public IList<StudentPlan> Plans { get; set; } = new List<StudentPlan>();
}
