using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lingua.Data;

/// <summary>
/// Fábricas usadas só pelo <c>dotnet ef</c>. A connection string aqui é descartável: as
/// migrations são geradas a partir do modelo, sem tocar em banco nenhum.
/// </summary>
public class SqliteDesignTimeFactory : IDesignTimeDbContextFactory<SqliteDataContext>
{
    public SqliteDataContext CreateDbContext(string[] args)
        => new(new DbContextOptionsBuilder<SqliteDataContext>()
            .UseSqlite("Data Source=lingua.db")
            .Options);
}

public class PostgresDesignTimeFactory : IDesignTimeDbContextFactory<PostgresDataContext>
{
    public PostgresDataContext CreateDbContext(string[] args)
        => new(new DbContextOptionsBuilder<PostgresDataContext>()
            .UseNpgsql("Host=localhost;Database=lingua;Username=postgres")
            .Options);
}
