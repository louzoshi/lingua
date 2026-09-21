namespace Lingua.ViewModels.Finance;

public class FinanceSummaryViewModel
{
    public int StudentsRegistered { get; set; }
    public int StudentsActive { get; set; }
    public int PlansActive { get; set; }
    public int PlansPaused { get; set; }
    public int StudentsWithoutPlan { get; set; }

    /// <summary>Soma das mensalidades dos planos ativos.</summary>
    public decimal ExpectedMonthly { get; set; }

    /// <summary>Quanto já entrou no mês corrente.</summary>
    public decimal ReceivedThisMonth { get; set; }
    public int PaidThisMonth { get; set; }
    public int PendingThisMonth { get; set; }

    public int ContractsSigned { get; set; }
    public int ContractsMissing { get; set; }
}
