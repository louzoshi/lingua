using Microsoft.EntityFrameworkCore;

namespace Lingua.Data;

/// <summary>Contexto usado em produção. Migrations em <c>Migrations/Postgres</c>.</summary>
public class PostgresDataContext : LinguaDataContext
{
    public PostgresDataContext(DbContextOptions<PostgresDataContext> options)
        : base(options)
    {
    }
}
