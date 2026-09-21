using Microsoft.EntityFrameworkCore;

namespace Lingua.Data;

/// <summary>Contexto usado em desenvolvimento. Migrations em <c>Migrations/Sqlite</c>.</summary>
public class SqliteDataContext : LinguaDataContext
{
    public SqliteDataContext(DbContextOptions<SqliteDataContext> options)
        : base(options)
    {
    }
}
