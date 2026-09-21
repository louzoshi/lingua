using Lingua.Data;
using Lingua.Extensions;
using Lingua.Models;
using Lingua.Services;
using Lingua.ViewModels;
using Lingua.ViewModels.Assignments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lingua.Controllers;

public class AssignmentController : ApiController
{
    [HttpGet("v1/assignments")]
    public async Task<IActionResult> GetAsync(
        [FromServices] LinguaDataContext context,
        [FromServices] AccessService access,
        [FromQuery] int? classroomId = null,
        [FromQuery] bool onlyOpen = false)
    {
        var userId = CurrentUserId;
        var visibleIds = await access.VisibleClassroomIdsAsync(userId);

        if (classroomId.HasValue)
        {
            if (!visibleIds.Contains(classroomId.Value))
                return Forbid();

            visibleIds = new List<int> { classroomId.Value };
        }

        var query = context.Assignments.AsNoTracking().Where(x => visibleIds.Contains(x.ClassroomId));

        if (onlyOpen)
            query = query.Where(x => x.DueDate == null || x.DueDate >= DateTime.UtcNow);

        var assignments = await query
            .OrderBy(x => x.DueDate ?? DateTime.MaxValue)
            .Select(x => new ListAssignmentsViewModel
            {
                Id = x.Id,
                Title = x.Title,
                Instructions = x.Instructions,
                Url = x.Url,
                ClassroomId = x.ClassroomId,
                Classroom = x.Classroom.Name,
                DueDate = x.DueDate,
                Submissions = x.Submissions.Count,
                SubmittedByMe = x.Submissions.Any(s => s.StudentId == userId),
                MyGrade = x.Submissions
                    .Where(s => s.StudentId == userId)
                    .Select(s => s.Grade)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return Ok(ResultViewModel<List<ListAssignmentsViewModel>>.Success(assignments));
    }

    [HttpPost("v1/assignments")]
    [Authorize(Policy = Policies.Teacher, AuthenticationSchemes = Policies.JwtScheme)]
    public async Task<IActionResult> PostAsync(
        [FromBody] EditorAssignmentViewModel model,
        [FromServices] LinguaDataContext context)
    {
        if (!ModelState.IsValid)
            return BadRequest(ResultViewModel<string>.Fail(ModelState.GetErrors()));

        if (!await context.Classrooms.AnyAsync(x => x.Id == model.ClassroomId))
            return NotFound(ResultViewModel<string>.Fail("Turma não encontrada"));

        var assignment = new Assignment
        {
            Title = model.Title.Trim(),
            Instructions = model.Instructions,
            Url = model.Url,
            ClassroomId = model.ClassroomId,
            DueDate = model.DueDate
        };

        await context.Assignments.AddAsync(assignment);
        await context.SaveChangesAsync();

        return Created($"v1/assignments/{assignment.Id}", ResultViewModel<Assignment>.Success(assignment));
    }

    [HttpPut("v1/assignments/{id:int}")]
    [Authorize(Policy = Policies.Teacher, AuthenticationSchemes = Policies.JwtScheme)]
    public async Task<IActionResult> PutAsync(
        [FromRoute] int id,
        [FromBody] EditorAssignmentViewModel model,
        [FromServices] LinguaDataContext context)
    {
        if (!ModelState.IsValid)
            return BadRequest(ResultViewModel<string>.Fail(ModelState.GetErrors()));

        var assignment = await context.Assignments.FirstOrDefaultAsync(x => x.Id == id);
        if (assignment == null)
            return NotFound(ResultViewModel<string>.Fail("Trabalho não encontrado"));

        assignment.Title = model.Title.Trim();
        assignment.Instructions = model.Instructions;
        assignment.Url = model.Url;
        assignment.DueDate = model.DueDate;

        await context.SaveChangesAsync();

        return Ok(ResultViewModel<Assignment>.Success(assignment));
    }

    [HttpDelete("v1/assignments/{id:int}")]
    [Authorize(Policy = Policies.Teacher, AuthenticationSchemes = Policies.JwtScheme)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] int id,
        [FromServices] LinguaDataContext context)
    {
        var assignment = await context.Assignments.FirstOrDefaultAsync(x => x.Id == id);
        if (assignment == null)
            return NotFound(ResultViewModel<string>.Fail("Trabalho não encontrado"));

        context.Assignments.Remove(assignment);
        await context.SaveChangesAsync();

        return Ok(ResultViewModel<string>.Success("Trabalho removido"));
    }

    /// <summary>Entrega do aluno. Reenviar antes da correção atualiza a mesma entrega.</summary>
    [HttpPost("v1/assignments/{id:int}/submissions")]
    public async Task<IActionResult> SubmitAsync(
        [FromRoute] int id,
        [FromBody] SubmitAssignmentViewModel model,
        [FromServices] LinguaDataContext context,
        [FromServices] AccessService access,
        [FromServices] InteractionService interactions)
    {
        if (!ModelState.IsValid)
            return BadRequest(ResultViewModel<string>.Fail(ModelState.GetErrors()));

        var assignment = await context.Assignments.FirstOrDefaultAsync(x => x.Id == id);
        if (assignment == null)
            return NotFound(ResultViewModel<string>.Fail("Trabalho não encontrado"));

        if (!await access.CanAccessClassroomAsync(CurrentUserId, assignment.ClassroomId))
            return Forbid();

        var submission = await context
            .AssignmentSubmissions
            .FirstOrDefaultAsync(x => x.AssignmentId == id && x.StudentId == CurrentUserId);

        if (submission == null)
        {
            submission = new AssignmentSubmission
            {
                AssignmentId = id,
                StudentId = CurrentUserId,
                Url = model.Url,
                Notes = model.Notes
            };

            await context.AssignmentSubmissions.AddAsync(submission);
            interactions.Track(CurrentUserId, InteractionType.AssignmentSubmitted, assignment.ClassroomId, id);
        }
        else
        {
            submission.Url = model.Url;
            submission.Notes = model.Notes;
            submission.SubmittedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        return Ok(ResultViewModel<string>.Success("Trabalho entregue"));
    }

    [HttpGet("v1/assignments/{id:int}/submissions")]
    [Authorize(Policy = Policies.Teacher, AuthenticationSchemes = Policies.JwtScheme)]
    public async Task<IActionResult> GetSubmissionsAsync(
        [FromRoute] int id,
        [FromServices] LinguaDataContext context)
    {
        var submissions = await context
            .AssignmentSubmissions
            .AsNoTracking()
            .Where(x => x.AssignmentId == id)
            .OrderBy(x => x.Student.Name)
            .Select(x => new SubmissionViewModel
            {
                Id = x.Id,
                AssignmentId = x.AssignmentId,
                AssignmentTitle = x.Assignment.Title,
                StudentId = x.StudentId,
                StudentName = x.Student.Name,
                Url = x.Url,
                Notes = x.Notes,
                SubmittedAt = x.SubmittedAt,
                Grade = x.Grade,
                Feedback = x.Feedback,
                ReviewedAt = x.ReviewedAt
            })
            .ToListAsync();

        return Ok(ResultViewModel<List<SubmissionViewModel>>.Success(submissions));
    }

    [HttpPut("v1/submissions/{id:int}/grade")]
    [Authorize(Policy = Policies.Teacher, AuthenticationSchemes = Policies.JwtScheme)]
    public async Task<IActionResult> GradeAsync(
        [FromRoute] int id,
        [FromBody] GradeSubmissionViewModel model,
        [FromServices] LinguaDataContext context)
    {
        if (!ModelState.IsValid)
            return BadRequest(ResultViewModel<string>.Fail(ModelState.GetErrors()));

        var submission = await context.AssignmentSubmissions.FirstOrDefaultAsync(x => x.Id == id);
        if (submission == null)
            return NotFound(ResultViewModel<string>.Fail("Entrega não encontrada"));

        submission.Grade = model.Grade;
        submission.Feedback = model.Feedback;
        submission.ReviewedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        return Ok(ResultViewModel<string>.Success("Entrega corrigida"));
    }
}
