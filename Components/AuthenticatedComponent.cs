using System.Security.Claims;
using Lingua.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Lingua.Services;

namespace Lingua.Components;

/// <summary>
/// Base das páginas que dependem de quem está logado. Resolve o usuário antes do
/// carregamento de dados; as páginas escrevem sua carga em <see cref="OnReadyAsync"/>.
/// </summary>
public abstract class AuthenticatedComponent : ComponentBase
{
    [CascadingParameter]
    public Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    [Inject] private SessionGuard Guard { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    protected ClaimsPrincipal CurrentUser { get; private set; } = new(new ClaimsIdentity());

    protected int UserId { get; private set; }

    protected bool IsTeacher { get; private set; }

    protected sealed override async Task OnInitializedAsync()
    {
        if (AuthenticationStateTask != null)
        {
            var state = await AuthenticationStateTask;
            CurrentUser = state.User;
            UserId = CurrentUser.GetUserId();
            IsTeacher = CurrentUser.IsTeacher();

            // O circuito do Blazor não passa pelo cookie a cada navegação, então a página
            // confere por conta própria se a conta continua ativa.
            if (UserId != 0 && !await Guard.IsActiveAsync(UserId))
            {
                Nav.NavigateTo("/entrar?inativa=1", forceLoad: true);
                return;
            }
        }

        await OnReadyAsync();
    }

    protected virtual Task OnReadyAsync() => Task.CompletedTask;

    protected static string Initials(string? name)
        => string.IsNullOrWhiteSpace(name) ? "?" : name.Trim()[..1].ToUpperInvariant();

    /// <summary>Data curta em português, para não repetir o formato em cada página.</summary>
    protected static string ShortDate(DateTime value)
        => value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    protected static string RelativeDate(DateTime value)
    {
        var elapsed = DateTime.UtcNow - value;

        return elapsed switch
        {
            { TotalMinutes: < 1 } => "agora",
            { TotalMinutes: < 60 } => $"há {(int)elapsed.TotalMinutes} min",
            { TotalHours: < 24 } => $"há {(int)elapsed.TotalHours} h",
            { TotalDays: < 7 } => $"há {(int)elapsed.TotalDays} d",
            _ => value.ToLocalTime().ToString("dd/MM/yyyy")
        };
    }
}
