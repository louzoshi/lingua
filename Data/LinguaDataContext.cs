using Lingua.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Lingua.Data;

/// <summary>
/// Modelo da plataforma. As classes concretas por provider existem porque migration de EF
/// é específica de banco: cada uma carrega o próprio conjunto em <c>Migrations/</c>.
/// Os serviços dependem desta base e não sabem em qual banco estão rodando.
/// </summary>
public abstract class LinguaDataContext : DbContext
{
    protected LinguaDataContext(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<PostMedia> PostMedia => Set<PostMedia>();

    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<DirectMessage> DirectMessages => Set<DirectMessage>();

    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonAttendance> LessonAttendances => Set<LessonAttendance>();
    public DbSet<StudyResource> StudyResources => Set<StudyResource>();

    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<AssignmentSubmission> AssignmentSubmissions => Set<AssignmentSubmission>();

    public DbSet<InteractionEvent> InteractionEvents => Set<InteractionEvent>();

    public DbSet<Package> Packages => Set<Package>();
    public DbSet<StudentPlan> StudentPlans => Set<StudentPlan>();
    public DbSet<Payment> Payments => Set<Payment>();

    /// <summary>Fila de saída lida pelo worker <c>Lingua.Notifications</c>.</summary>
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
}
