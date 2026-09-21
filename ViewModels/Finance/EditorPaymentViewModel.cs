using System.ComponentModel.DataAnnotations;

namespace Lingua.ViewModels.Finance;

public class EditorPaymentViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Escolha o plano")]
    public int PlanId { get; set; }

    public DateTime ReferenceMonth { get; set; } = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    [Range(0.01, 100000, ErrorMessage = "Informe o valor recebido")]
    public decimal Amount { get; set; }

    public DateTime PaidAt { get; set; } = DateTime.Today;

    [StringLength(60)]
    public string? Method { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}
