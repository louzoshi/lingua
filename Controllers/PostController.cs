using Lingua.Data;
using Lingua.Extensions;
using Lingua.Models;
using Lingua.Services;
using Lingua.ViewModels;
using Lingua.ViewModels.Posts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lingua.Controllers;

public class PostController : ApiController
{
    /// <summary>
    /// Feed do usuário. Sem filtro traz o mural geral somado aos murais das turmas dele;
    /// com classroomId, traz só aquela turma.
    /// </summary>
    [HttpGet("v1/posts")]
    public async Task<IActionResult> GetAsync(
        [FromServices] PostService posts,
        [FromQuery] int? classroomId = null,
        [FromQuery] string? topic = null,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);

        var total = await posts.CountFeedAsync(CurrentUserId, classroomId, topic);
        var items = await posts.GetFeedAsync(CurrentUserId, classroomId, topic, page, pageSize);

        return Ok(ResultViewModel<dynamic>.Success(new { total, page, pageSize, posts = items }));
    }

    [HttpGet("v1/posts/{id:int}")]
    public async Task<IActionResult> DetailsAsync(
        [FromRoute] int id,
        [FromServices] PostService posts)
    {
        var post = await posts.GetDetailsAsync(CurrentUserId, id);

        return post == null
            ? NotFound(ResultViewModel<string>.Fail("Post não encontrado"))
            : Ok(ResultViewModel<PostDetailsViewModel>.Success(post));
    }

    [HttpPost("v1/posts")]
    public async Task<IActionResult> PostAsync(
        [FromBody] EditorPostViewModel model,
        [FromServices] PostService posts)
    {
        if (!ModelState.IsValid)
            return BadRequest(ResultViewModel<string>.Fail(ModelState.GetErrors()));

        var (post, error) = await posts.CreateAsync(CurrentUserId, model);

        if (error != null)
            return BadRequest(ResultViewModel<string>.Fail(error));

        return Created($"v1/posts/{post!.Id}", ResultViewModel<dynamic>.Success(new { post.Id, post.Slug }));
    }

    [HttpPut("v1/posts/{id:int}")]
    public async Task<IActionResult> PutAsync(
        [FromRoute] int id,
        [FromBody] EditorPostViewModel model,
        [FromServices] PostService posts)
    {
        if (!ModelState.IsValid)
            return BadRequest(ResultViewModel<string>.Fail(ModelState.GetErrors()));

        var (ok, error) = await posts.UpdateAsync(CurrentUserId, id, model);

        return ok
            ? Ok(ResultViewModel<string>.Success("Post atualizado"))
            : BadRequest(ResultViewModel<string>.Fail(error!));
    }

    [HttpDelete("v1/posts/{id:int}")]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] int id,
        [FromServices] PostService posts)
    {
        var (ok, error) = await posts.DeleteAsync(CurrentUserId, id);

        return ok
            ? Ok(ResultViewModel<string>.Success("Post removido"))
            : BadRequest(ResultViewModel<string>.Fail(error!));
    }

    [HttpPost("v1/posts/{id:int}/reactions")]
    public async Task<IActionResult> ToggleReactionAsync(
        [FromRoute] int id,
        [FromServices] PostService posts)
    {
        var (reacted, total) = await posts.ToggleReactionAsync(CurrentUserId, id);

        return Ok(ResultViewModel<dynamic>.Success(new { reacted, total }));
    }

    [HttpPost("v1/posts/{id:int}/comments")]
    public async Task<IActionResult> CommentAsync(
        [FromRoute] int id,
        [FromBody] EditorCommentViewModel model,
        [FromServices] PostService posts)
    {
        if (!ModelState.IsValid)
            return BadRequest(ResultViewModel<string>.Fail(ModelState.GetErrors()));

        var (comment, error) = await posts.AddCommentAsync(CurrentUserId, id, model.Body);

        return error != null
            ? BadRequest(ResultViewModel<string>.Fail(error))
            : Created($"v1/posts/{id}", ResultViewModel<dynamic>.Success(new { comment!.Id }));
    }

    /// <summary>Tira do ar ou devolve um post ao mural. Só professor.</summary>
    [HttpPut("v1/posts/{id:int}/moderation")]
    [Authorize(Policy = Policies.Teacher, AuthenticationSchemes = Policies.JwtScheme)]
    public async Task<IActionResult> ModeratePostAsync(
        [FromRoute] int id,
        [FromQuery] ModerationStatus status,
        [FromServices] PostService posts)
        => await posts.SetPostStatusAsync(id, status)
            ? Ok(ResultViewModel<string>.Success("Post atualizado"))
            : NotFound(ResultViewModel<string>.Fail("Post não encontrado"));

    [HttpPut("v1/comments/{id:int}/moderation")]
    [Authorize(Policy = Policies.Teacher, AuthenticationSchemes = Policies.JwtScheme)]
    public async Task<IActionResult> ModerateCommentAsync(
        [FromRoute] int id,
        [FromQuery] ModerationStatus status,
        [FromServices] PostService posts)
        => await posts.SetCommentStatusAsync(id, status)
            ? Ok(ResultViewModel<string>.Success("Comentário atualizado"))
            : NotFound(ResultViewModel<string>.Fail("Comentário não encontrado"));
}
