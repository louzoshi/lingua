using Lingua.Data;
using Lingua.Extensions;
using Lingua.Models;
using Lingua.ViewModels;
using Lingua.ViewModels.Posts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Lingua.Controllers;

public class TopicController : ApiController
{
    private const string CacheKey = "TopicsCache";

    [HttpGet("v1/topics")]
    public async Task<IActionResult> GetAsync(
        [FromServices] IMemoryCache cache,
        [FromServices] LinguaDataContext context)
    {
        var topics = await cache.GetOrCreateAsync(CacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return context.Topics.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        });

        return Ok(ResultViewModel<List<Topic>>.Success(topics ?? new List<Topic>()));
    }

    [HttpPost("v1/topics")]
    [Authorize(Policy = Policies.Teacher, AuthenticationSchemes = Policies.JwtScheme)]
    public async Task<IActionResult> PostAsync(
        [FromBody] EditorTopicViewModel model,
        [FromServices] LinguaDataContext context,
        [FromServices] IMemoryCache cache)
    {
        if (!ModelState.IsValid)
            return BadRequest(ResultViewModel<string>.Fail(ModelState.GetErrors()));

        var topic = new Topic { Name = model.Name.Trim(), Slug = model.Name.ToSlug() };

        try
        {
            await context.Topics.AddAsync(topic);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest(ResultViewModel<string>.Fail("Já existe um assunto com esse nome"));
        }

        cache.Remove(CacheKey);

        return Created($"v1/topics/{topic.Id}", ResultViewModel<Topic>.Success(topic));
    }

    [HttpDelete("v1/topics/{id:int}")]
    [Authorize(Policy = Policies.Teacher, AuthenticationSchemes = Policies.JwtScheme)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] int id,
        [FromServices] LinguaDataContext context,
        [FromServices] IMemoryCache cache)
    {
        var topic = await context.Topics.FirstOrDefaultAsync(x => x.Id == id);
        if (topic == null)
            return NotFound(ResultViewModel<string>.Fail("Assunto não encontrado"));

        context.Topics.Remove(topic);
        await context.SaveChangesAsync();
        cache.Remove(CacheKey);

        return Ok(ResultViewModel<string>.Success("Assunto removido"));
    }
}
