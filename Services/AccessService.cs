using Lingua.Data;
using Lingua.Models;
using Microsoft.EntityFrameworkCore;

namespace Lingua.Services;

/// <summary>
/// Concentra as regras de quem enxerga o quê. Nenhum controller ou página decide
/// visibilidade por conta própria: tudo passa por aqui.
/// </summary>
public class AccessService
{
    private readonly LinguaDataContext _context;

    public AccessService(LinguaDataContext context)
        => _context = context;

    /// <summary>Professor ou admin enxerga qualquer turma e modera qualquer conteúdo.</summary>
    public async Task<bool> IsStaffAsync(int userId)
        => await _context.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .AnyAsync(x => x.Roles.Any(r => r.Slug == Role.Teacher || r.Slug == Role.Admin));

    /// <summary>Turmas que o usuário pode abrir: as que leciona e as em que está matriculado.</summary>
    public async Task<List<int>> VisibleClassroomIdsAsync(int userId)
    {
        if (await IsStaffAsync(userId))
        {
            return await _context.Classrooms
                .AsNoTracking()
                .Where(x => !x.IsArchived)
                .Select(x => x.Id)
                .ToListAsync();
        }

        return await _context.Enrollments
            .AsNoTracking()
            .Where(x => x.StudentId == userId && x.IsActive && !x.Classroom.IsArchived)
            .Select(x => x.ClassroomId)
            .ToListAsync();
    }

    public async Task<bool> CanAccessClassroomAsync(int userId, int classroomId)
    {
        if (await IsStaffAsync(userId))
            return true;

        return await _context.Enrollments
            .AsNoTracking()
            .AnyAsync(x => x.StudentId == userId && x.ClassroomId == classroomId && x.IsActive);
    }

    /// <summary>
    /// Filtra um conjunto de posts para o que o usuário tem direito de ler: o mural geral
    /// mais os murais das suas turmas, sempre sem o que está escondido pela moderação.
    /// </summary>
    public IQueryable<Post> VisiblePosts(
        IQueryable<Post> source,
        int userId,
        IReadOnlyCollection<int> classroomIds,
        bool isStaff)
    {
        if (!isStaff)
        {
            source = source.Where(x =>
                x.Status == ModerationStatus.Published || x.AuthorId == userId);
        }

        return source.Where(x =>
            x.ClassroomId == null || classroomIds.Contains(x.ClassroomId.Value));
    }

    /// <summary>Alunos com quem este usuário divide alguma turma — o catálogo de DM permitido.</summary>
    public async Task<List<User>> ContactsForAsync(int userId)
    {
        var isStaff = await IsStaffAsync(userId);
        var classroomIds = await VisibleClassroomIdsAsync(userId);

        var query = _context.Users
            .AsNoTracking()
            .Where(x => x.IsActive && x.Id != userId);

        if (!isStaff)
        {
            // Aluno só conversa com colegas de turma e com o professor responsável.
            query = query.Where(x =>
                x.Enrollments.Any(e => e.IsActive && classroomIds.Contains(e.ClassroomId)) ||
                x.TeachingClassrooms.Any(c => classroomIds.Contains(c.Id)));
        }

        return await query.OrderBy(x => x.Name).ToListAsync();
    }

    /// <summary>A DM só abre entre pessoas que se encontram em alguma turma.</summary>
    public async Task<bool> CanMessageAsync(int userId, int otherUserId)
    {
        if (userId == otherUserId)
            return false;

        if (await IsStaffAsync(userId) || await IsStaffAsync(otherUserId))
            return true;

        var mine = await VisibleClassroomIdsAsync(userId);

        return await _context.Enrollments
            .AsNoTracking()
            .AnyAsync(x => x.StudentId == otherUserId && x.IsActive && mine.Contains(x.ClassroomId));
    }
}
