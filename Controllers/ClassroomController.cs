using Lingua.Data;
using Lingua.Extensions;
using Lingua.Models;
using Lingua.Services;
using Lingua.ViewModels;
using Lingua.ViewModels.Classrooms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lingua.Controllers;

public class ClassroomController : ApiController
{
    /// <summary>Turmas que o usuário logado pode abrir.</summary>
    [HttpGet("v1/classrooms")]
    public async Task<IActionResult> GetAsync(
        [FromServices] LinguaDataContext context,
        [FromServices] AccessService access)
    {
        var visibleIds = await access.VisibleClassroomIdsAsync(CurrentUserId);

        var classrooms = await context
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

        return Ok(ResultViewModel<List<ListClassroomsViewModel>>.Success(classrooms));
    }

    [HttpGet("v1/classrooms/{id:int}")]
    public async Task<IActionResult> GetByIdAsync(
        [FromRoute] int id,
        [FromServices] LinguaDataContext context,
        [FromServices] AccessService access)
    {
        if (!await access.CanAccessClassroomAsync(CurrentUserId, id))
            return Forbid();

        var classroom = await context
            .Classrooms
            .AsNoTracking()
            .Include(x => x.Teacher)
            .Include(x => x.Enrollments.Where(e => e.IsActive))
            .ThenInclude(e => e.Student)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (classroom == null)
            return NotFound(ResultViewModel<string>.Fail("Turma não encontrada"));

        return Ok(ResultViewModel<dynamic>.Success(new
        {
            classroom.Id,
            classroom.Name,
            classroom.Slug,
            classroom.Description,
            classroom.Level,
            Teacher = new { classroom.Teacher.Id, classroom.Teacher.Name, classroom.Teacher.Slug },
            Students = classroom.Enrollments.Select(e => new
            {
                e.Student.Id,
                e.Student.Name,
                e.Student.Slug,
                e.Student.Image,
                e.Student.Level
            })
        }));
    }

    [HttpPost("v1/classrooms")]
    [Authorize(Policy = Policies.Teacher, AuthenticationSchemes = Policies.JwtScheme)]
    public async Task<IActionResult> PostAsync(
        [FromBody] EditorClassroomViewModel model,
        [FromServices] LinguaDataContext context)
    {
        if (!ModelState.IsValid)
            return BadRequest(ResultViewModel<string>.Fail(ModelState.GetErrors()));

        var classroom = new Classroom
        {
            Name = model.Name.Trim(),
            Slug = model.Name.ToSlug(),
            Description = model.Description,
            Level = model.Level,
            TeacherId = CurrentUserId
        };

        try
        {
            await context.Classrooms.AddAsync(classroom);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest(ResultViewModel<string>.Fail("Já existe uma turma com esse nome"));
        }

        return Created($"v1/classrooms/{classroom.Id}", ResultViewModel<Classroom>.Success(classroom));
    }

    [HttpPut("v1/classrooms/{id:int}")]
    [Authorize(Policy = Policies.Teacher, AuthenticationSchemes = Policies.JwtScheme)]
    public async Task<IActionResult> PutAsync(
        [FromRoute] int id,
        [FromBody] EditorClassroomViewModel model,
        [FromServices] LinguaDataContext context)
    {
        if (!ModelState.IsValid)
            return BadRequest(ResultViewModel<string>.Fail(ModelState.GetErrors()));

        var classroom = await context.Classrooms.FirstOrDefaultAsync(x => x.Id == id);
        if (classroom == null)
            return NotFound(ResultViewModel<string>.Fail("Turma não encontrada"));

        classroom.Name = model.Name.Trim();
        classroom.Slug = model.Name.ToSlug();
        classroom.Description = model.Description;
        classroom.Level = model.Level;

        await context.SaveChangesAsync();

        return Ok(ResultViewModel<Classroom>.Success(classroom));
    }

    /// <summary>Arquiva a turma em vez de apagar, para não perder o histórico dos alunos.</summary>
    [HttpDelete("v1/classrooms/{id:int}")]
    [Authorize(Policy = Policies.Teacher, AuthenticationSchemes = Policies.JwtScheme)]
    public async Task<IActionResult> ArchiveAsync(
        [FromRoute] int id,
        [FromServices] LinguaDataContext context)
    {
        var classroom = await context.Classrooms.FirstOrDefaultAsync(x => x.Id == id);
        if (classroom == null)
            return NotFound(ResultViewModel<string>.Fail("Turma não encontrada"));

        classroom.IsArchived = true;
        await context.SaveChangesAsync();

        return Ok(ResultViewModel<string>.Success("Turma arquivada"));
    }

    [HttpPost("v1/classrooms/{id:int}/students")]
    [Authorize(Policy = Policies.Teacher, AuthenticationSchemes = Policies.JwtScheme)]
    public async Task<IActionResult> EnrollAsync(
        [FromRoute] int id,
        [FromBody] EnrollViewModel model,
        [FromServices] LinguaDataContext context)
    {
        if (!ModelState.IsValid)
            return BadRequest(ResultViewModel<string>.Fail(ModelState.GetErrors()));

        if (!await context.Classrooms.AnyAsync(x => x.Id == id))
            return NotFound(ResultViewModel<string>.Fail("Turma não encontrada"));

        if (!await context.Users.AnyAsync(x => x.Id == model.StudentId))
            return NotFound(ResultViewModel<string>.Fail("Aluno não encontrado"));

        var enrollment = await context
            .Enrollments
            .FirstOrDefaultAsync(x => x.ClassroomId == id && x.StudentId == model.StudentId);

        if (enrollment == null)
        {
            enrollment = new Enrollment { ClassroomId = id, StudentId = model.StudentId };
            await context.Enrollments.AddAsync(enrollment);
        }
        else
        {
            // Rematrícula reativa o vínculo antigo e preserva o histórico.
            enrollment.IsActive = true;
            enrollment.EnrolledAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        return Ok(ResultViewModel<string>.Success("Aluno matriculado"));
    }

    [HttpDelete("v1/classrooms/{id:int}/students/{studentId:int}")]
    [Authorize(Policy = Policies.Teacher, AuthenticationSchemes = Policies.JwtScheme)]
    public async Task<IActionResult> UnenrollAsync(
        [FromRoute] int id,
        [FromRoute] int studentId,
        [FromServices] LinguaDataContext context)
    {
        var enrollment = await context
            .Enrollments
            .FirstOrDefaultAsync(x => x.ClassroomId == id && x.StudentId == studentId);

        if (enrollment == null)
            return NotFound(ResultViewModel<string>.Fail("Matrícula não encontrada"));

        enrollment.IsActive = false;
        await context.SaveChangesAsync();

        return Ok(ResultViewModel<string>.Success("Aluno removido da turma"));
    }
}
