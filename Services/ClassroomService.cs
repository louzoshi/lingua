using Lingua.Data;
using Lingua.Extensions;
using Lingua.Models;
using Lingua.ViewModels.Classrooms;
using Microsoft.EntityFrameworkCore;

namespace Lingua.Services;

public class ClassroomService
{
    private readonly LinguaDataContext _context;
    private readonly AccessService _access;

    public ClassroomService(LinguaDataContext context, AccessService access)
    {
        _context = context;
        _access = access;
    }

    /// <summary>Turmas que o usuário pode abrir, com os números que aparecem no card.</summary>
    public async Task<List<ListClassroomsViewModel>> ForUserAsync(int userId)
    {
        var visibleIds = await _access.VisibleClassroomIdsAsync(userId);

        return await _context
            .Classrooms
            .AsNoTracking()
            .Where(x => visibleIds.Contains(x.Id))
            .OrderBy(x => x.Name)
            .Select(x => new ListClassroomsViewModel
            {
                Id = x.Id,
                Name = x.Name,
                Slug = x.Slug,
                Description = x.Description,
                Level = x.Level,
                TeacherName = x.Teacher.Name,
                Students = x.Enrollments.Count(e => e.IsActive),
                Posts = x.Posts.Count,
                IsArchived = x.IsArchived
            })
            .ToListAsync();
    }

    public async Task<Classroom?> GetAsync(int userId, int classroomId)
        => await _access.CanAccessClassroomAsync(userId, classroomId)
            ? await _context
                .Classrooms
                .AsNoTracking()
                .Include(x => x.Teacher)
                .FirstOrDefaultAsync(x => x.Id == classroomId)
            : null;

    public async Task<List<User>> StudentsAsync(int classroomId)
        => await _context
            .Enrollments
            .AsNoTracking()
            .Where(x => x.ClassroomId == classroomId && x.IsActive)
            .OrderBy(x => x.Student.Name)
            .Select(x => x.Student)
            .ToListAsync();

    public async Task<(Classroom? Classroom, string? Error)> CreateAsync(
        int teacherId,
        EditorClassroomViewModel model)
    {
        var slug = model.Name.ToSlug();

        if (await _context.Classrooms.AnyAsync(x => x.Slug == slug))
            return (null, "Já existe uma turma com esse nome");

        var classroom = new Classroom
        {
            Name = model.Name.Trim(),
            Slug = slug,
            Description = model.Description,
            Level = model.Level,
            TeacherId = teacherId
        };

        await _context.Classrooms.AddAsync(classroom);
        await _context.SaveChangesAsync();

        return (classroom, null);
    }

    public async Task<string?> EnrollAsync(int classroomId, int studentId)
    {
        if (!await _context.Classrooms.AnyAsync(x => x.Id == classroomId))
            return "Turma não encontrada";

        if (!await _context.Users.AnyAsync(x => x.Id == studentId))
            return "Aluno não encontrado";

        var enrollment = await _context
            .Enrollments
            .FirstOrDefaultAsync(x => x.ClassroomId == classroomId && x.StudentId == studentId);

        if (enrollment == null)
            await _context.Enrollments.AddAsync(new Enrollment { ClassroomId = classroomId, StudentId = studentId });
        else
            enrollment.IsActive = true;

        await _context.SaveChangesAsync();

        return null;
    }

    public async Task RemoveStudentAsync(int classroomId, int studentId)
    {
        var enrollment = await _context
            .Enrollments
            .FirstOrDefaultAsync(x => x.ClassroomId == classroomId && x.StudentId == studentId);

        if (enrollment == null)
            return;

        enrollment.IsActive = false;
        await _context.SaveChangesAsync();
    }

    /// <summary>Alunos da escola, para a tela de matrícula do professor.</summary>
    public async Task<List<User>> AllStudentsAsync()
        => await _context
            .Users
            .AsNoTracking()
            .Where(x => x.IsActive && x.Roles.Any(r => r.Slug == Role.Student))
            .OrderBy(x => x.Name)
            .ToListAsync();
}
