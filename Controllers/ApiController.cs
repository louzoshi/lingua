using Lingua.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lingua.Controllers;

/// <summary>
/// Base dos endpoints REST. A API responde só a token JWT; o front em Blazor usa cookie,
/// então os dois esquemas convivem sem um invadir o outro.
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public abstract class ApiController : ControllerBase
{
    protected int CurrentUserId => User.GetUserId();

    protected IActionResult ServerError(string code)
        => StatusCode(500, ViewModels.ResultViewModel<string>.Fail($"{code} - Falha interna no servidor"));
}
