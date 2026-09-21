namespace Lingua.ViewModels.Finance;

public class PaymentViewModel
{
    public int Id { get; set; }
    public int PlanId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public DateTime ReferenceMonth { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; }
    public string? Method { get; set; }
    public string? Note { get; set; }
}
