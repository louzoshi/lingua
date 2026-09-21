using Lingua.Data;
using Lingua.Models;
using Lingua.ViewModels.Finance;
using Microsoft.EntityFrameworkCore;

namespace Lingua.Services;

/// <summary>
/// A parte financeira da escola, visível só para a professora: pacotes vendidos, o plano
/// de cada aluno (valor, vencimento, contrato) e as mensalidades recebidas.
/// </summary>
public class FinanceService
{
    private readonly LinguaDataContext _context;
    private readonly MediaStorage _storage;

    public FinanceService(LinguaDataContext context, MediaStorage storage)
    {
        _context = context;
        _storage = storage;
    }

    private static DateTime CurrentMonth
        => new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

    public async Task<FinanceSummaryViewModel> SummaryAsync()
    {
        var month = CurrentMonth;

        var students = _context.Users.AsNoTracking().Where(x => x.Roles.Any(r => r.Slug == Role.Student));
        var activePlans = _context.StudentPlans.AsNoTracking().Where(x => x.Status == PlanStatus.Active);

        var paidPlanIds = await _context.Payments
            .AsNoTracking()
            .Where(x => x.ReferenceMonth == month)
            .Select(x => x.PlanId)
            .Distinct()
            .ToListAsync();

        var activePlanCount = await activePlans.CountAsync();

        return new FinanceSummaryViewModel
        {
            StudentsRegistered = await students.CountAsync(),
            StudentsActive = await students.CountAsync(x => x.IsActive),
            PlansActive = activePlanCount,
            PlansPaused = await _context.StudentPlans.CountAsync(x => x.Status == PlanStatus.Paused),
            StudentsWithoutPlan = await students.CountAsync(x =>
                x.IsActive && !_context.StudentPlans.Any(p => p.StudentId == x.Id && p.Status != PlanStatus.Ended)),
            ExpectedMonthly = await activePlans.SumAsync(x => x.MonthlyValue),
            ReceivedThisMonth = await _context.Payments.Where(x => x.ReferenceMonth == month).SumAsync(x => x.Amount),
            PaidThisMonth = await activePlans.CountAsync(x => paidPlanIds.Contains(x.Id)),
            PendingThisMonth = await activePlans.CountAsync(x => !paidPlanIds.Contains(x.Id)),
            ContractsSigned = await _context.StudentPlans.CountAsync(x => x.Status != PlanStatus.Ended && x.ContractFile != null),
            ContractsMissing = await _context.StudentPlans.CountAsync(x => x.Status != PlanStatus.Ended && x.ContractFile == null)
        };
    }

    public async Task<List<Package>> PackagesAsync(bool includeInactive = false)
        => await _context.Packages
            .AsNoTracking()
            .Where(x => includeInactive || x.IsActive)
            .OrderBy(x => x.MonthlyPrice)
            .ToListAsync();

    public async Task<Package> CreatePackageAsync(EditorPackageViewModel model)
    {
        var package = new Package
        {
            Name = model.Name.Trim(),
            Description = model.Description?.Trim(),
            MonthlyPrice = model.MonthlyPrice,
            LessonsPerWeek = model.LessonsPerWeek
        };

        await _context.Packages.AddAsync(package);
        await _context.SaveChangesAsync();

        return package;
    }

    public async Task SetPackageActiveAsync(int packageId, bool active)
    {
        var package = await _context.Packages.FirstOrDefaultAsync(x => x.Id == packageId);
        if (package == null)
            return;

        package.IsActive = active;
        await _context.SaveChangesAsync();
    }

    public async Task<List<PlanOverviewViewModel>> PlansAsync()
    {
        var month = CurrentMonth;

        return await _context.StudentPlans
            .AsNoTracking()
            .OrderBy(x => x.Status)
            .ThenBy(x => x.Student.Name)
            .Select(x => new PlanOverviewViewModel
            {
                Id = x.Id,
                StudentId = x.StudentId,
                StudentName = x.Student.Name,
                StudentEmail = x.Student.Email,
                StudentImage = x.Student.Image,
                StudentActive = x.Student.IsActive,
                PackageName = x.Package != null ? x.Package.Name : null,
                MonthlyValue = x.MonthlyValue,
                DueDay = x.DueDay,
                StartedAt = x.StartedAt,
                EndedAt = x.EndedAt,
                Status = x.Status,
                HasContract = x.ContractFile != null,
                ContractOriginalName = x.ContractOriginalName,
                ContractSignedAt = x.ContractSignedAt,
                Notes = x.Notes,
                PaidThisMonth = x.Payments.Any(p => p.ReferenceMonth == month),
                LastPaymentAt = x.Payments.OrderByDescending(p => p.PaidAt).Select(p => (DateTime?)p.PaidAt).FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<EditorPlanViewModel?> GetPlanForEditAsync(int planId)
        => await _context.StudentPlans
            .AsNoTracking()
            .Where(x => x.Id == planId)
            .Select(x => new EditorPlanViewModel
            {
                Id = x.Id,
                StudentId = x.StudentId,
                PackageId = x.PackageId,
                MonthlyValue = x.MonthlyValue,
                DueDay = x.DueDay,
                StartedAt = x.StartedAt,
                Status = x.Status,
                ContractSignedAt = x.ContractSignedAt,
                Notes = x.Notes
            })
            .FirstOrDefaultAsync();

    /// <summary>Cria ou atualiza o plano. Um aluno só pode ter um plano que não esteja encerrado.</summary>
    public async Task<(StudentPlan? Plan, string? Error)> SavePlanAsync(EditorPlanViewModel model)
    {
        if (!await _context.Users.AnyAsync(x => x.Id == model.StudentId))
            return (null, "Aluno não encontrado");

        if (model.PackageId.HasValue && !await _context.Packages.AnyAsync(x => x.Id == model.PackageId.Value))
            return (null, "Pacote não encontrado");

        StudentPlan plan;

        if (model.Id.HasValue)
        {
            var existing = await _context.StudentPlans.FirstOrDefaultAsync(x => x.Id == model.Id.Value);
            if (existing == null)
                return (null, "Plano não encontrado");

            plan = existing;
        }
        else
        {
            var open = await _context.StudentPlans.AnyAsync(x =>
                x.StudentId == model.StudentId && x.Status != PlanStatus.Ended);

            if (open)
                return (null, "Este aluno já tem um plano em aberto. Encerre-o antes de criar outro.");

            plan = new StudentPlan { StudentId = model.StudentId };
            await _context.StudentPlans.AddAsync(plan);
        }

        plan.PackageId = model.PackageId;
        plan.MonthlyValue = model.MonthlyValue;
        plan.DueDay = model.DueDay;
        plan.StartedAt = DateTime.SpecifyKind(model.StartedAt, DateTimeKind.Utc);
        plan.Status = model.Status;
        plan.EndedAt = model.Status == PlanStatus.Ended ? plan.EndedAt ?? DateTime.UtcNow : null;
        plan.ContractSignedAt = model.ContractSignedAt.HasValue
            ? DateTime.SpecifyKind(model.ContractSignedAt.Value, DateTimeKind.Utc)
            : null;
        plan.Notes = model.Notes?.Trim();

        await _context.SaveChangesAsync();

        return (plan, null);
    }

    public async Task<string?> AttachContractAsync(int planId, Stream content, string contentType, string originalName)
    {
        var plan = await _context.StudentPlans.FirstOrDefaultAsync(x => x.Id == planId);
        if (plan == null)
            return "Plano não encontrado";

        var (fileName, error) = await _storage.SaveContractAsync(content, contentType);
        if (error != null)
            return error;

        // Substituir o contrato apaga o arquivo anterior para não acumular PDFs órfãos.
        _storage.DeleteContract(plan.ContractFile);

        plan.ContractFile = fileName;
        plan.ContractOriginalName = originalName;
        plan.ContractSignedAt ??= DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return null;
    }

    public async Task RemoveContractAsync(int planId)
    {
        var plan = await _context.StudentPlans.FirstOrDefaultAsync(x => x.Id == planId);
        if (plan == null)
            return;

        _storage.DeleteContract(plan.ContractFile);

        plan.ContractFile = null;
        plan.ContractOriginalName = null;
        plan.ContractSignedAt = null;

        await _context.SaveChangesAsync();
    }

    /// <summary>Caminho do PDF no disco e o nome para o download, ou nulo sem contrato.</summary>
    public async Task<(string Path, string DownloadName)?> ContractFileAsync(int planId)
    {
        var plan = await _context.StudentPlans
            .AsNoTracking()
            .Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == planId);

        var path = _storage.ContractPath(plan?.ContractFile);
        if (plan == null || path == null)
            return null;

        return (path, plan.ContractOriginalName ?? $"contrato-{plan.Student.Slug}.pdf");
    }

    public async Task<List<PaymentViewModel>> PaymentsAsync(int take = 60)
        => await _context.Payments
            .AsNoTracking()
            .OrderByDescending(x => x.PaidAt)
            .Take(take)
            .Select(x => new PaymentViewModel
            {
                Id = x.Id,
                PlanId = x.PlanId,
                StudentName = x.Plan.Student.Name,
                ReferenceMonth = x.ReferenceMonth,
                Amount = x.Amount,
                PaidAt = x.PaidAt,
                Method = x.Method,
                Note = x.Note
            })
            .ToListAsync();

    public async Task<string?> RegisterPaymentAsync(EditorPaymentViewModel model)
    {
        if (!await _context.StudentPlans.AnyAsync(x => x.Id == model.PlanId))
            return "Plano não encontrado";

        var month = new DateTime(model.ReferenceMonth.Year, model.ReferenceMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        await _context.Payments.AddAsync(new Payment
        {
            PlanId = model.PlanId,
            ReferenceMonth = month,
            Amount = model.Amount,
            PaidAt = DateTime.SpecifyKind(model.PaidAt, DateTimeKind.Utc),
            Method = model.Method?.Trim(),
            Note = model.Note?.Trim()
        });

        await _context.SaveChangesAsync();

        return null;
    }

    public async Task DeletePaymentAsync(int paymentId)
    {
        await _context.Payments.Where(x => x.Id == paymentId).ExecuteDeleteAsync();
    }
}
