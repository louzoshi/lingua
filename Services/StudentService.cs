using Lingua.Data;
using Lingua.Models;
using Lingua.ViewModels.Students;
using Microsoft.EntityFrameworkCore;

namespace Lingua.Services;

/// <summary>
/// Administração das contas de aluno pela professora: quem está na escola, em quais turmas,
/// e o poder de tirar o acesso de quem saiu. Desativar não apaga nada — o histórico do aluno
/// permanece, mas ele não entra mais e some das listas de contato e do ranking.
/// </summary>
public class StudentService
{
    private readonly LinguaDataContext _context;
    private readonly SessionGuard _sessions;

    public StudentService(LinguaDataContext context, SessionGuard sessions)
    {
        _context = context;
        _sessions = sessions;
    }

    public async Task<List<StudentOverviewViewModel>> OverviewAsync()
        => await _context.Users
            .AsNoTracking()
            .Where(x => x.Roles.Any(r => r.Slug == Role.Student))
            .OrderByDescending(x => x.IsActive)
            .ThenBy(x => x.Name)
            .Select(x => new StudentOverviewViewModel
            {
                Id = x.Id,
                Name = x.Name,
                Email = x.Email,
                Slug = x.Slug,
                Image = x.Image,
                Level = x.Level,
                IsActive = x.IsActive,
                DeactivatedAt = x.DeactivatedAt,
                LastSeenAt = x.LastSeenAt,
                Posts = x.Posts.Count,
                Comments = x.Comments.Count,
                Classrooms = x.Enrollments
                    .Where(e => e.IsActive && !e.Classroom.IsArchived)
                    .Select(e => new StudentOverviewViewModel.EnrolledClassroom
                    {
                        Id = e.ClassroomId,
                        Name = e.Classroom.Name
                    })
                    .ToList()
            })
            .ToListAsync();

    /// <summary>Tira o acesso: login recusado, sessão derrubada, some dos contatos e do ranking.</summary>
    public async Task<string?> DeactivateAsync(int studentId)
    {
        var user = await _context.Users
            .Include(x => x.Roles)
            .FirstOrDefaultAsync(x => x.Id == studentId);

        if (user == null)
            return "Aluno não encontrado";

        if (user.Roles.Any(r => r.Slug == Role.Teacher || r.Slug == Role.Admin))
            return "Contas de professor não podem ser desativadas por aqui";

        user.IsActive = false;
        user.DeactivatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        _sessions.Invalidate(studentId);

        return null;
    }

    public async Task<string?> ReactivateAsync(int studentId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == studentId);
        if (user == null)
            return "Aluno não encontrado";

        user.IsActive = true;
        user.DeactivatedAt = null;

        await _context.SaveChangesAsync();
        _sessions.Invalidate(studentId);

        return null;
    }

    /// <summary>
    /// Curadoria em lote: esconde todos os posts e comentários de um aluno. Nada é apagado, e
    /// a professora consegue republicar item a item se mudar de ideia.
    /// </summary>
    public async Task<(int Posts, int Comments)> HideAllContentAsync(int studentId)
    {
        var posts = await _context.Posts
            .Where(x => x.AuthorId == studentId && x.Status != ModerationStatus.Hidden)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, ModerationStatus.Hidden));

        var comments = await _context.Comments
            .Where(x => x.AuthorId == studentId && x.Status != ModerationStatus.Hidden)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, ModerationStatus.Hidden));

        return (posts, comments);
    }
}
