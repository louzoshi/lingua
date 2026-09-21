using Lingua.Data;
using Lingua.Extensions;
using Lingua.Models;
using Lingua.ViewModels.Accounts;
using Microsoft.EntityFrameworkCore;
using SecureIdentity.Password;

namespace Lingua.Services;

/// <summary>
/// Criação de contas. Não existe cadastro aberto na plataforma: o professor convida e a
/// senha inicial é gerada aqui. A API e a tela de gerenciar usam este mesmo caminho para
/// que a regra não se divida em dois lugares.
/// </summary>
public static class AccountFactory
{
    public static async Task<(User? User, string? Password, string? Error)> InviteStudentAsync(
        LinguaDataContext context,
        NotificationService notifications,
        RegisterViewModel model,
        string roleSlug = Role.Student)
    {
        var role = await context.Roles.FirstOrDefaultAsync(x => x.Slug == roleSlug);
        if (role == null)
            return (null, null, "Perfil não encontrado");

        var email = model.Email.Trim().ToLowerInvariant();
        if (await context.Users.AnyAsync(x => x.Email == email))
            return (null, null, "Este e-mail já está cadastrado");

        var password = PasswordGenerator.Generate(16);

        var user = new User
        {
            Name = model.Name.Trim(),
            Email = email,
            Slug = await BuildUniqueSlugAsync(context, model.Name),
            Level = model.Level,
            PasswordHash = PasswordHasher.Hash(password)
        };
        user.Roles.Add(role);

        if (model.ClassroomId.HasValue)
        {
            if (!await context.Classrooms.AnyAsync(x => x.Id == model.ClassroomId.Value))
                return (null, null, "Turma não encontrada");

            user.Enrollments.Add(new Enrollment { ClassroomId = model.ClassroomId.Value });
        }

        await context.Users.AddAsync(user);

        // O convite entra na mesma transação do aluno. Quem entrega é o worker de notificações:
        // a professora não fica esperando o servidor de e-mail responder para ver a tela voltar.
        notifications.QueueStudentInvite(user, password);

        await context.SaveChangesAsync();

        return (user, password, null);
    }

    /// <summary>Gera um slug livre a partir do nome, numerando em caso de homônimo.</summary>
    private static async Task<string> BuildUniqueSlugAsync(LinguaDataContext context, string name)
    {
        var baseSlug = name.ToSlug();
        if (string.IsNullOrEmpty(baseSlug))
            baseSlug = "aluno";

        var slug = baseSlug;
        var suffix = 1;

        while (await context.Users.AnyAsync(x => x.Slug == slug))
            slug = $"{baseSlug}-{++suffix}";

        return slug;
    }
}
