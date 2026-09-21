using Lingua.Extensions;
using Lingua.Models;
using Microsoft.EntityFrameworkCore;
using SecureIdentity.Password;

namespace Lingua.Data;

/// <summary>
/// Deixa o banco utilizável logo na primeira execução: papéis, assuntos e a conta do
/// professor. Sem isso ninguém consegue entrar, já que não existe cadastro aberto.
/// </summary>
public static class Seeder
{
    private static readonly (string Name, string Slug)[] Roles =
    {
        ("Aluno", Role.Student),
        ("Professor", Role.Teacher),
        ("Administrador", Role.Admin)
    };

    private static readonly string[] Topics =
    {
        "Free Talk", "Grammar", "Vocabulary", "Culture", "Movies & Series", "Doubts"
    };

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<LinguaDataContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Seeder");

        await context.Database.MigrateAsync();

        foreach (var (name, slug) in Roles)
        {
            if (!await context.Roles.AnyAsync(x => x.Slug == slug))
                await context.Roles.AddAsync(new Role { Name = name, Slug = slug });
        }

        foreach (var topic in Topics)
        {
            var slug = topic.ToSlug();
            if (!await context.Topics.AnyAsync(x => x.Slug == slug))
                await context.Topics.AddAsync(new Topic { Name = topic, Slug = slug });
        }

        await context.SaveChangesAsync();

        await EnsureTeacherAsync(context, configuration, logger);
    }

    private static async Task EnsureTeacherAsync(
        LinguaDataContext context,
        IConfiguration configuration,
        ILogger logger)
    {
        var email = (configuration["Seed:TeacherEmail"] ?? "teacher@lingua.local").ToLowerInvariant();

        if (await context.Users.AnyAsync(x => x.Email == email))
            return;

        // Sem senha configurada geramos uma e mostramos no log do primeiro boot.
        var configured = configuration["Seed:TeacherPassword"];
        var password = string.IsNullOrWhiteSpace(configured) ? PasswordGenerator.Generate(16) : configured;

        var teacherRole = await context.Roles.FirstAsync(x => x.Slug == Role.Teacher);
        var adminRole = await context.Roles.FirstAsync(x => x.Slug == Role.Admin);

        var teacher = new User
        {
            Name = configuration["Seed:TeacherName"] ?? "Teacher",
            Email = email,
            Slug = (configuration["Seed:TeacherName"] ?? "teacher").ToSlug(),
            Level = EnglishLevel.C2,
            PasswordHash = PasswordHasher.Hash(password)
        };
        teacher.Roles.Add(teacherRole);
        teacher.Roles.Add(adminRole);

        await context.Users.AddAsync(teacher);
        await context.SaveChangesAsync();

        if (string.IsNullOrWhiteSpace(configured))
            logger.LogWarning("Conta de professor criada: {Email} / senha inicial: {Password}", email, password);
    }
}
