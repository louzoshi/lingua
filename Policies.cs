using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Lingua;

public static class Policies
{
    /// <summary>Professor ou admin: cria turmas, aulas, trabalhos e modera conteúdo.</summary>
    public const string Teacher = "teacher-only";

    /// <summary>Só admin: gerencia professores.</summary>
    public const string Admin = "admin-only";

    public const string JwtScheme = JwtBearerDefaults.AuthenticationScheme;
}
