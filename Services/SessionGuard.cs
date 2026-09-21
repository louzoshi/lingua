using Lingua.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Lingua.Services;

/// <summary>
/// Confere a cada requisição se a conta por trás de um cookie ou token ainda está ativa.
/// Sem isso, desativar um aluno só teria efeito quando a sessão dele expirasse — dias
/// depois. O resultado fica em cache por pouco tempo para não bater no banco em toda
/// página; <see cref="Invalidate"/> zera o cache no momento da desativação.
/// </summary>
public class SessionGuard
{
    private static readonly TimeSpan CacheFor = TimeSpan.FromSeconds(30);

    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;

    public SessionGuard(IMemoryCache cache, IServiceScopeFactory scopeFactory)
    {
        _cache = cache;
        _scopeFactory = scopeFactory;
    }

    public async Task<bool> IsActiveAsync(int userId)
    {
        if (userId == 0)
            return false;

        if (_cache.TryGetValue(Key(userId), out bool cached))
            return cached;

        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LinguaDataContext>();

        var active = await context.Users
            .AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => x.IsActive)
            .FirstOrDefaultAsync();

        _cache.Set(Key(userId), active, CacheFor);

        return active;
    }

    public void Invalidate(int userId)
        => _cache.Remove(Key(userId));

    private static string Key(int userId) => $"session-active:{userId}";
}
