namespace Lingua.Models;

/// <summary>Uma mensalidade recebida. <see cref="ReferenceMonth"/> é sempre o dia 1 do mês pago.</summary>
public class Payment
{
    public int Id { get; set; }

    public int PlanId { get; set; }
    public StudentPlan Plan { get; set; } = null!;

    public DateTime ReferenceMonth { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;

    /// <summary>Pix, cartão, dinheiro... texto livre.</summary>
    public string? Method { get; set; }
    public string? Note { get; set; }
}
