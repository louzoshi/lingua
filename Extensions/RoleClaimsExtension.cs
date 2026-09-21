using System.Security.Claims;
using Lingua.Models;

namespace Lingua.Extensions;

public static class RoleClaimsExtension
{
    public static IEnumerable<Claim> GetClaims(this User user)
    {
        var result = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Email),
            new("name", user.Name),
            new("slug", user.Slug)
        };

        result.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role.Slug)));

        return result;
    }

    /// <summary>Id do usuário autenticado, ou zero quando não há sessão.</summary>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    public static bool IsTeacher(this ClaimsPrincipal principal)
        => principal.IsInRole(Role.Teacher) || principal.IsInRole(Role.Admin);
}
