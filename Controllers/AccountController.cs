using System.Text.RegularExpressions;
using Lingua.Data;
using Lingua.Extensions;
using Lingua.Models;
using Lingua.Services;
using Lingua.ViewModels;
using Lingua.ViewModels.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureIdentity.Password;

namespace Lingua.Controllers;

public class AccountController : ApiController
{
    private static readonly Regex Base64Prefix = new(@"^data:image\/[a-z]+;base64,", RegexOptions.Compiled);
    private static readonly string[] AllowedRoles = { Role.Student, Role.Teacher };

    /// <summary>Convida um aluno. Só professor cria conta: não há cadastro aberto na plataforma.</summary>
    [HttpPost("v1/accounts")]
    [Authorize(Policy = Policies.Teacher, AuthenticationSchemes = Policies.JwtScheme)]
    public async Task<IActionResult> InviteAsync(
        [FromBody] RegisterViewModel model,
        [FromQuery] string? role,
        [FromServices] LinguaDataContext context,
        [FromServices] EmailService emailService)
    {
        if (!ModelState.IsValid)
            return BadRequest(ResultViewModel<string>.Fail(ModelState.GetErrors()));

        var roleSlug = string.IsNullOrWhiteSpace(role) ? Role.Student : role.ToLowerInvariant();
        if (!AllowedRoles.Contains(roleSlug))
            return BadRequest(ResultViewModel<string>.Fail("Perfil inválido"));

        var (user, password, error) = await AccountFactory.InviteStudentAsync(
            context, emailService, model, roleSlug);

        return error != null
            ? BadRequest(ResultViewModel<string>.Fail(error))
            : Ok(ResultViewModel<dynamic>.Success(new { user = user!.Email, password }));
    }

    [HttpPost("v1/accounts/login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginAsync(
        [FromBody] LoginViewModel model,
        [FromServices] LinguaDataContext context,
        [FromServices] TokenService tokenService)
    {
        if (!ModelState.IsValid)
            return BadRequest(ResultViewModel<string>.Fail(ModelState.GetErrors()));

        var user = await context
            .Users
            .Include(x => x.Roles)
            .FirstOrDefaultAsync(x => x.Email == model.Email.Trim().ToLower());

        if (user == null || !PasswordHasher.Verify(user.PasswordHash, model.Password))
            return StatusCode(401, ResultViewModel<string>.Fail("Usuário ou senha inválidos"));

        if (!user.IsActive)
            return StatusCode(401, ResultViewModel<string>.Fail("Esta conta está desativada"));

        user.LastSeenAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return Ok(ResultViewModel<string>.Success(tokenService.GenerateToken(user)));
    }

    [HttpPut("v1/accounts/me")]
    public async Task<IActionResult> UpdateProfileAsync(
        [FromBody] UpdateProfileViewModel model,
        [FromServices] LinguaDataContext context)
    {
        if (!ModelState.IsValid)
            return BadRequest(ResultViewModel<string>.Fail(ModelState.GetErrors()));

        var user = await context.Users.FirstOrDefaultAsync(x => x.Id == CurrentUserId);
        if (user == null)
            return NotFound(ResultViewModel<string>.Fail("Usuário não encontrado"));

        user.Name = model.Name.Trim();
        user.Bio = model.Bio;
        user.Location = model.Location;
        user.Level = model.Level;

        await context.SaveChangesAsync();

        return Ok(ResultViewModel<string>.Success("Perfil atualizado"));
    }

    [HttpPut("v1/accounts/me/password")]
    public async Task<IActionResult> ChangePasswordAsync(
        [FromBody] ChangePasswordViewModel model,
        [FromServices] LinguaDataContext context)
    {
        if (!ModelState.IsValid)
            return BadRequest(ResultViewModel<string>.Fail(ModelState.GetErrors()));

        var user = await context.Users.FirstOrDefaultAsync(x => x.Id == CurrentUserId);
        if (user == null)
            return NotFound(ResultViewModel<string>.Fail("Usuário não encontrado"));

        if (!PasswordHasher.Verify(user.PasswordHash, model.CurrentPassword))
            return BadRequest(ResultViewModel<string>.Fail("Senha atual incorreta"));

        user.PasswordHash = PasswordHasher.Hash(model.NewPassword);
        await context.SaveChangesAsync();

        return Ok(ResultViewModel<string>.Success("Senha alterada"));
    }

    [HttpPost("v1/accounts/me/image")]
    public async Task<IActionResult> UploadImageAsync(
        [FromBody] UploadImageViewModel model,
        [FromServices] LinguaDataContext context,
        [FromServices] IWebHostEnvironment environment)
    {
        if (!ModelState.IsValid)
            return BadRequest(ResultViewModel<string>.Fail(ModelState.GetErrors()));

        var user = await context.Users.FirstOrDefaultAsync(x => x.Id == CurrentUserId);
        if (user == null)
            return NotFound(ResultViewModel<string>.Fail("Usuário não encontrado"));

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(Base64Prefix.Replace(model.Base64Image, string.Empty));
        }
        catch (FormatException)
        {
            return BadRequest(ResultViewModel<string>.Fail("Imagem inválida"));
        }

        var fileName = $"{Guid.NewGuid()}.jpg";
        var folder = Path.Combine(environment.WebRootPath, "images");
        Directory.CreateDirectory(folder);

        try
        {
            await System.IO.File.WriteAllBytesAsync(Path.Combine(folder, fileName), bytes);
        }
        catch (IOException)
        {
            return ServerError("05X04");
        }

        user.Image = $"{Configuration.PublicUrl}/images/{fileName}";
        await context.SaveChangesAsync();

        return Ok(ResultViewModel<string>.Success(user.Image));
    }
}
