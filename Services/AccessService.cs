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

    /// <summary>
    /// Se o usuário ainda faz parte da escola. Para aluno isso significa ter pelo menos uma
    /// matrícula ativa: quem foi removido de todas as turmas perde o mural geral, os materiais
    /// da escola e os contatos — a conta continua existindo, mas sem nada para ver.
    /// </summary>
    public async Task<bool> HasSchoolAccessAsync(int userId)
    {
        if (await IsStaffAsync(userId))
            return true;

        return await _context.Enrollments
            .AsNoTracking()
            .AnyAsync(x => x.StudentId == userId && x.IsActive && !x.Classroom.IsArchived);
    }

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
            // Sem turma ativa o aluno não enxerga nem o mural geral.
            if (classroomIds.Count == 0)
                return source.Where(_ => false);

            source = source.Where(x =>
                x.Status == ModerationStatus.Published || x.AuthorId == userId);
        }

        return source.Where(x =>
            x.ClassroomId == null || classroomIds.Contains(x.ClassroomId.Value));
    }

    /// <summary>
    /// Com quem este usuário pode abrir DM. Qualquer aluno ativo da escola fala com qualquer
    /// outro e com a professora; quem não tem turma ativa só fala com a professora. As
    /// conversas ficam visíveis para a professora na moderação.
    /// </summary>
    public async Task<List<User>> ContactsForAsync(int userId)
    {
        var isStaff = await IsStaffAsync(userId);
        var hasSchoolAccess = await HasSchoolAccessAsync(userId);

        var query = _context.Users
            .AsNoTracking()
            .Where(x => x.IsActive && x.Id != userId);

        if (!isStaff)
        {
            query = hasSchoolAccess
                ? query.Where(x =>
                    x.Roles.Any(r => r.Slug == Role.Teacher || r.Slug == Role.Admin) ||
                    x.Enrollments.Any(e => e.IsActive && !e.Classroom.IsArchived))
                : query.Where(x => x.Roles.Any(r => r.Slug == Role.Teacher || r.Slug == Role.Admin));
        }

        return await query.OrderBy(x => x.Name).ToListAsync();
    }

    /// <summary>A DM abre entre qualquer par de pessoas ativas da escola.</summary>
    public async Task<bool> CanMessageAsync(int userId, int otherUserId)
    {
        if (userId == otherUserId)
            return false;

        var other = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == otherUserId);
        if (other == null || !other.IsActive)
            return false;

        if (await IsStaffAsync(userId) || await IsStaffAsync(otherUserId))
            return true;

        return await HasSchoolAccessAsync(userId) && await HasSchoolAccessAsync(otherUserId);
    }
}
