using Lingua.Data;
using Lingua.Models;
using Lingua.ViewModels.Ranking;
using Microsoft.EntityFrameworkCore;

namespace Lingua.Services;

/// <summary>
/// Registra o que cada aluno faz na plataforma e monta o ranking de interação a partir
/// desse histórico. A pontuação premia o que gera conversa em inglês, não só volume.
/// </summary>
public class InteractionService
{
    private static readonly IReadOnlyDictionary<InteractionType, int> Points =
        new Dictionary<InteractionType, int>
        {
            [InteractionType.PostCreated] = 10,
            [InteractionType.CommentCreated] = 4,
            [InteractionType.ReactionGiven] = 1,
            [InteractionType.ReactionReceived] = 2,
            [InteractionType.DirectMessageSent] = 1,
            [InteractionType.LessonAttended] = 15,
            [InteractionType.AssignmentSubmitted] = 20
        };

    private readonly LinguaDataContext _context;

    public InteractionService(LinguaDataContext context)
        => _context = context;

    public static int PointsFor(InteractionType type)
        => Points.TryGetValue(type, out var points) ? points : 0;

    /// <summary>Enfileira o evento no contexto. O SaveChanges fica a cargo de quem chamou.</summary>
    public void Track(int userId, InteractionType type, int? classroomId = null, int? referenceId = null)
    {
        _context.InteractionEvents.Add(new InteractionEvent
        {
            UserId = userId,
            Type = type,
            Points = PointsFor(type),
            ClassroomId = classroomId,
            ReferenceId = referenceId,
            OccurredAt = DateTime.UtcNow
        });
    }

    /// <param name="classroomId">Quando informado, restringe o ranking a uma turma.</param>
    /// <param name="days">Janela em dias; nulo traz o histórico inteiro.</param>
    public async Task<List<RankingEntryViewModel>> GetRankingAsync(
        int? classroomId = null,
        int? days = 30,
        int take = 20)
    {
        var query = _context.InteractionEvents.AsNoTracking();

        if (classroomId.HasValue)
            query = query.Where(x => x.ClassroomId == classroomId.Value);

        if (days.HasValue)
        {
            var since = DateTime.UtcNow.AddDays(-days.Value);
            query = query.Where(x => x.OccurredAt >= since);
        }

        var totals = await query
            .GroupBy(x => x.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Points = g.Sum(x => x.Points),
                Events = g.Count()
            })
            .OrderByDescending(x => x.Points)
            .Take(take)
            .ToListAsync();

        var userIds = totals.Select(x => x.UserId).ToList();
        var users = await _context.Users
            .AsNoTracking()
            .Where(x => userIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name, x.Slug, x.Image, x.Level })
            .ToListAsync();

        return totals
            .Select((total, index) =>
            {
                var user = users.First(u => u.Id == total.UserId);
                return new RankingEntryViewModel
                {
                    Position = index + 1,
                    UserId = user.Id,
                    Name = user.Name,
                    Slug = user.Slug,
                    Image = user.Image,
                    Level = user.Level,
                    Points = total.Points,
                    Interactions = total.Events
                };
            })
            .ToList();
    }

    /// <summary>Total de pontos de um aluno, usado no cabeçalho do perfil.</summary>
    public async Task<int> GetTotalPointsAsync(int userId, int? days = null)
    {
        var query = _context.InteractionEvents.AsNoTracking().Where(x => x.UserId == userId);

        if (days.HasValue)
        {
            var since = DateTime.UtcNow.AddDays(-days.Value);
            query = query.Where(x => x.OccurredAt >= since);
        }

        return await query.SumAsync(x => (int?)x.Points) ?? 0;
    }
}
