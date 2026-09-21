namespace Lingua.Services;

/// <summary>
/// Executa um trecho de trabalho em um escopo de DI próprio.
/// <para>
/// No Blazor Server o escopo dura o circuito inteiro, então um DbContext injetado direto no
/// componente viveria horas acumulando entidades e quebraria se dois renders concorressem.
/// As páginas pedem os serviços por aqui e cada operação ganha um contexto novo e descartável.
/// </para>
/// </summary>
public class ScopeRunner
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ScopeRunner(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    public async Task<TResult> RunAsync<TResult>(Func<IServiceProvider, Task<TResult>> work)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        return await work(scope.ServiceProvider);
    }

    public async Task RunAsync(Func<IServiceProvider, Task> work)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        await work(scope.ServiceProvider);
    }

    /// <summary>Atalho para o caso mais comum: usar um único serviço.</summary>
    public Task<TResult> UseAsync<TService, TResult>(Func<TService, Task<TResult>> work)
        where TService : notnull
        => RunAsync(sp => work(sp.GetRequiredService<TService>()));

    public Task UseAsync<TService>(Func<TService, Task> work)
        where TService : notnull
        => RunAsync(sp => work(sp.GetRequiredService<TService>()));
}
