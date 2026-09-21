namespace Lingua.Models;

/// <summary>
/// Acordo financeiro de um aluno com a escola: qual pacote, quanto paga por mês, em que dia
/// vence e o contrato assinado. Um aluno tem no máximo um plano ativo por vez.
/// </summary>
public class StudentPlan
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    public int? PackageId { get; set; }
    public Package? Package { get; set; }

    /// <summary>Valor efetivamente cobrado por mês, que pode diferir do preço de tabela.</summary>
    public decimal MonthlyValue { get; set; }

    /// <summary>Dia do mês em que a mensalidade vence (1 a 28).</summary>
    public int DueDay { get; set; } = 10;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public PlanStatus Status { get; set; } = PlanStatus.Active;

    /// <summary>Nome do arquivo do contrato em <c>App_Data/contracts</c>. Nulo sem contrato.</summary>
    public string? ContractFile { get; set; }
    public string? ContractOriginalName { get; set; }
    public DateTime? ContractSignedAt { get; set; }

    public string? Notes { get; set; }

    public IList<Payment> Payments { get; set; } = new List<Payment>();
}
