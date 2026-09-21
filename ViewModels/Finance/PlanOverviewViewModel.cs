using Lingua.Models;

namespace Lingua.ViewModels.Finance;

/// <summary>Linha da tabela financeira: um aluno, o que ele paga e como está o contrato.</summary>
public class PlanOverviewViewModel
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string? StudentImage { get; set; }
    public bool StudentActive { get; set; }

    public string? PackageName { get; set; }
    public decimal MonthlyValue { get; set; }
    public int DueDay { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public PlanStatus Status { get; set; }

    public bool HasContract { get; set; }
    public string? ContractOriginalName { get; set; }
    public DateTime? ContractSignedAt { get; set; }
    public string? Notes { get; set; }

    /// <summary>Se o mês corrente já foi pago.</summary>
    public bool PaidThisMonth { get; set; }
    public DateTime? LastPaymentAt { get; set; }
}
