using System.ComponentModel.DataAnnotations;
using Lingua.Models;

namespace Lingua.ViewModels.Finance;

public class EditorPlanViewModel
{
    public int? Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Escolha o aluno")]
    public int StudentId { get; set; }

    public int? PackageId { get; set; }

    [Range(0, 100000, ErrorMessage = "Informe um valor válido")]
    public decimal MonthlyValue { get; set; }

    [Range(1, 28, ErrorMessage = "O vencimento deve ser entre o dia 1 e 28")]
    public int DueDay { get; set; } = 10;

    public DateTime StartedAt { get; set; } = DateTime.Today;
    public PlanStatus Status { get; set; } = PlanStatus.Active;
    public DateTime? ContractSignedAt { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}
