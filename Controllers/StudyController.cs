using Lingua.Data;
using Lingua.Extensions;
using Lingua.Models;
using Lingua.Services;
using Lingua.ViewModels;
using Lingua.ViewModels.Study;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lingua.Controllers;

/// <summary>Área de estudo: aulas agendadas, gravações e materiais.</summary>
public class StudyController : ApiController
{
    [HttpGet("v1/lessons")]
    public async Task<IActionResult> GetLessonsAsync(
        [FromServices] LinguaDataContext context,
        [FromServices] AccessService access,
        [FromQuery] int? classroomId = null,
        [FromQuery] bool onlyRecorded = false)
    {
        var userId = CurrentUserId;
        var visibleIds = await access.VisibleClassroomIdsAsync(userId);

        if (classroomId.HasValue)
        {
            if (!visibleIds.Contains(classroomId.Value))
                return Forbid();

            visibleIds = new List<int> { classroomId.Value };
        }

        var query = context.Lessons.AsNoTracking().Where(x => visibleIds.Contains(x.ClassroomId));

        if (onlyRecorded)
            query = query.Where(x => x.RecordingUrl != null);

        var lessons = await query
            .OrderByDescending(x => x.ScheduledAt)
            .Select(x => new ListLessonsViewModel
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                ClassroomId = x.ClassroomId,
                Classroom = x.Classroom.Name,
                ScheduledAt = x.ScheduledAt,
                DurationMinutes = x.DurationMinutes,
                MeetingUrl = x.MeetingUrl,
                RecordingUrl = x.RecordingUrl,
                Attendees = x.Attendances.Count,
                AttendedByMe = x.Attendances.Any(a => a.StudentId == userId)
            })
            .ToListAsync();

        return Ok(ResultViewModel<List<ListLessonsViewModel>>.Success(lessons));
    }

    [HttpPost("v1/lessons")]
    [Authorize(Policy = Policies.Teacher, AuthenticationSchemes = Policies.JwtScheme)]
    public async Task<IActionResult> PostLessonAsync(
        [FromBody] EditorLessonViewModel model,
        [FromServices] LinguaDataContext context)
    {
        if (!ModelState.IsValid)
            return BadRequest(ResultViewModel<string>.Fail(ModelState.GetErrors()));

        if (!await context.Classrooms.AnyAsync(x => x.Id == model.ClassroomId))
            return NotFound(ResultViewModel<string>.Fail("Turma não encontrada"));

        var lesson = new Lesson
        {
            Title = model.Title.Trim(),
            Description = model.Description,
            ClassroomId = model.ClassroomId,
            ScheduledAt = model.ScheduledAt,
            DurationMinutes = model.DurationMinutes,
            MeetingUrl = model.MeetingUrl,
            RecordingUrl = model.RecordingUrl
        };

        await context.Lessons.AddAsync(lesson);
        await context.SaveChangesAsync();

        return Created($"v1/lessons/{lesson.Id}", ResultViewModel<Lesson>.Success(lesson));
    }

    [HttpPut("v1/lessons/{id:int}")]
    [Authorize(Policy = Policies.Teacher, AuthenticationSchemes = Policies.JwtScheme)]
    public async Task<IActionResult> PutLessonAsync(
        [FromRoute] int id,
        [FromBody] EditorLessonViewModel model,
        [FromServices] LinguaDataContext context)
    {
        if (!ModelState.IsValid)
            return BadRequest(ResultViewModel<string>.Fail(ModelState.GetErrors()));

        var lesson = await context.Lessons.FirstOrDefaultAsync(x => x.Id == id);
        if (lesson == null)
            return NotFound(ResultViewModel<string>.Fail("Aula não encontrada"));

        lesson.Title = model.Title.Trim();
        lesson.Description = model.Description;
        lesson.ScheduledAt = model.ScheduledAt;
        lesson.DurationMinutes = model.DurationMinutes;
        lesson.MeetingUrl = model.MeetingUrl;
        lesson.RecordingUrl = model.RecordingUrl;

        await context.SaveChangesAsync();

        return Ok(ResultViewModel<Lesson>.Success(lesson));
    }

    [HttpDelete("v1/lessons/{id:int}")]
    [Authorize(Policy = Policies.Teacher, AuthenticationSchemes = Policies.JwtScheme)]
    public async Task<IActionResult> DeleteLessonAsync(
        [FromRoute] int id,
        [FromServices] LinguaDataContext context)
    {
        var lesson = await context.Lessons.FirstOrDefaultAsync(x => x.Id == id);
        if (lesson == null)
            return NotFound(ResultViewModel<string>.Fail("Aula não encontrada"));

        context.Lessons.Remove(lesson);
        await context.SaveChangesAsync();

        return Ok(ResultViewModel<string>.Success("Aula removida"));
    }

    /// <summary>O aluno marca presença; é o que alimenta o histórico de aulas do perfil.</summary>
    [HttpPost("v1/lessons/{id:int}/attendance")]
    public async Task<IActionResult> AttendAsync(
        [FromRoute] int id,
        [FromServices] LinguaDataContext context,
        [FromServices] AccessService access,
        [FromServices] InteractionService interactions)
    {
        var lesson = await context.Lessons.FirstOrDefaultAsync(x => x.Id == id);
        if (lesson == null)
            return NotFound(ResultViewModel<string>.Fail("Aula não encontrada"));

        if (!await access.CanAccessClassroomAsync(CurrentUserId, lesson.ClassroomId))
            return Forbid();

        var already = await context
            .LessonAttendances
            .AnyAsync(x => x.LessonId == id && x.StudentId == CurrentUserId);

        if (already)
            return Ok(ResultViewModel<string>.Success("Presença já registrada"));

        await context.LessonAttendances.AddAsync(new LessonAttendance
        {
            LessonId = id,
            StudentId = CurrentUserId
        });

        interactions.Track(CurrentUserId, InteractionType.LessonAttended, lesson.ClassroomId, id);
        await context.SaveChangesAsync();

        return Ok(ResultViewModel<string>.Success("Presença registrada"));
    }

    [HttpGet("v1/resources")]
    public async Task<IActionResult> GetResourcesAsync(
        [FromServices] LinguaDataContext context,
        [FromServices] AccessService access,
        [FromQuery] int? classroomId = null,
        [FromQuery] ResourceKind? kind = null)
    {
        var visibleIds = await access.VisibleClassroomIdsAsync(CurrentUserId);

        var query = context
            .StudyResources
            .AsNoTracking()
            .Where(x => x.ClassroomId == null || visibleIds.Contains(x.ClassroomId.Value));

        if (classroomId.HasValue)
            query = query.Where(x => x.ClassroomId == classroomId.Value);

        if (kind.HasValue)
            query = query.Where(x => x.Kind == kind.Value);

        var resources = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ListStudyResourcesViewModel
            {
                Id = x.Id,
                Title = x.Title,
                Description = x.Description,
                Url = x.Url,
                Kind = x.Kind,
                Level = x.Level,
                Classroom = x.Classroom != null ? x.Classroom.Name : null,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return Ok(ResultViewModel<List<ListStudyResourcesViewModel>>.Success(resources));
    }

    [HttpPost("v1/resources")]
    [Authorize(Policy = Policies.Teacher, AuthenticationSchemes = Policies.JwtScheme)]
    public async Task<IActionResult> PostResourceAsync(
        [FromBody] EditorStudyResourceViewModel model,
        [FromServices] LinguaDataContext context)
    {
        if (!ModelState.IsValid)
            return BadRequest(ResultViewModel<string>.Fail(ModelState.GetErrors()));

        var resource = new StudyResource
        {
            Title = model.Title.Trim(),
            Description = model.Description,
            Url = model.Url,
            Kind = model.Kind,
            Level = model.Level,
            ClassroomId = model.ClassroomId,
            CreatedById = CurrentUserId
        };

        await context.StudyResources.AddAsync(resource);
        await context.SaveChangesAsync();

        return Created($"v1/resources/{resource.Id}", ResultViewModel<StudyResource>.Success(resource));
    }

    [HttpDelete("v1/resources/{id:int}")]
    [Authorize(Policy = Policies.Teacher, AuthenticationSchemes = Policies.JwtScheme)]
    public async Task<IActionResult> DeleteResourceAsync(
        [FromRoute] int id,
        [FromServices] LinguaDataContext context)
    {
        var resource = await context.StudyResources.FirstOrDefaultAsync(x => x.Id == id);
        if (resource == null)
            return NotFound(ResultViewModel<string>.Fail("Material não encontrado"));

        context.StudyResources.Remove(resource);
        await context.SaveChangesAsync();

        return Ok(ResultViewModel<string>.Success("Material removido"));
    }
}
