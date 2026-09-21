using Lingua.Services;
using Lingua.ViewModels;
using Lingua.ViewModels.Ranking;
using Microsoft.AspNetCore.Mvc;

namespace Lingua.Controllers;

public class RankingController : ApiController
{
    /// <summary>Ranking de interação, opcionalmente recortado por turma e por período.</summary>
    [HttpGet("v1/ranking")]
    public async Task<IActionResult> GetAsync(
        [FromServices] InteractionService interactions,
        [FromServices] AccessService access,
        [FromQuery] int? classroomId = null,
        [FromQuery] int days = 30,
        [FromQuery] int take = 20)
    {
        if (classroomId.HasValue && !await access.CanAccessClassroomAsync(CurrentUserId, classroomId.Value))
            return Forbid();

        var ranking = await interactions.GetRankingAsync(
            classroomId,
            days <= 0 ? null : days,
            Math.Clamp(take, 1, 100));

        return Ok(ResultViewModel<List<RankingEntryViewModel>>.Success(ranking));
    }
}
