using Lingua.Services;
using Lingua.ViewModels;
using Lingua.ViewModels.Profile;
using Microsoft.AspNetCore.Mvc;

namespace Lingua.Controllers;

public class ProfileController : ApiController
{
    [HttpGet("v1/profiles/{slug}")]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string slug,
        [FromServices] ProfileService profiles)
    {
        var profile = await profiles.GetAsync(slug, CurrentUserId);

        return profile == null
            ? NotFound(ResultViewModel<string>.Fail("Perfil não encontrado"))
            : Ok(ResultViewModel<StudentProfileViewModel>.Success(profile));
    }
}
