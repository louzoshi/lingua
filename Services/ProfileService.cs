using Lingua.Data;
using Lingua.Models;
using Lingua.ViewModels.Posts;
using Lingua.ViewModels.Profile;
using Microsoft.EntityFrameworkCore;

namespace Lingua.Services;

public class ProfileService
{
    private readonly LinguaDataContext _context;
    private readonly AccessService _access;
    private readonly InteractionService _interactions;

    public ProfileService(
        LinguaDataContext context,
        AccessService access,
        InteractionService interactions)
    {
        _context = context;
        _access = access;
        _interactions = interactions;
    }

    /// <param name="viewerId">Quem está olhando o perfil, para decidir o que aparece.</param>
    public async Task<StudentProfileViewModel?> GetAsync(string slug, int viewerId)
    {
        var user = await _context
            .Users
            .AsNoTracking()
            .Include(x => x.Roles)
            .Include(x => x.Enrollments.Where(e => e.IsActive))
            .ThenInclude(e => e.Classroom)
            .Include(x => x.TeachingClassrooms)
            .FirstOrDefaultAsync(x => x.Slug == slug);

        if (user == null)
            return null;

        var isTeacher = user.Roles.Any(r => r.Slug is Role.Teacher or Role.Admin);
        var viewerIsStaff = await _access.IsStaffAsync(viewerId);
        var viewerClassroomIds = await _access.VisibleClassroomIdsAsync(viewerId);

        var recentPosts = await _access
            .VisiblePosts(_context.Posts.AsNoTracking(), viewerId, viewerClassroomIds, viewerIsStaff)
            .Where(x => x.AuthorId == user.Id)
            .OrderByDescending(x => x.LastUpdateDate)
            .Take(5)
            .Select(x => new ListPostsViewModel
            {
                Id = x.Id,
                Title = x.Title,
                Summary = x.Summary,
                Slug = x.Slug,
                LastUpdateDate = x.LastUpdateDate,
                Status = x.Status,
                AuthorName = x.Author.Name,
                AuthorSlug = x.Author.Slug,
                AuthorImage = x.Author.Image,
                ClassroomId = x.ClassroomId,
                Classroom = x.Classroom != null ? x.Classroom.Name : null,
                Comments = x.Comments.Count,
                Reactions = x.Reactions.Count,
                ReactedByMe = x.Reactions.Any(r => r.UserId == viewerId)
            })
            .ToListAsync();

        var classrooms = isTeacher
            ? user.TeachingClassrooms.Select(c => c.Name).ToList()
            : user.Enrollments.Select(e => e.Classroom.Name).ToList();

        var ranking = await _interactions.GetRankingAsync(days: 30, take: 100);
        var position = ranking.FirstOrDefault(x => x.UserId == user.Id)?.Position ?? 0;

        return new StudentProfileViewModel
        {
            Id = user.Id,
            Name = user.Name,
            Slug = user.Slug,
            Image = user.Image,
            Bio = user.Bio,
            Location = user.Location,
            Level = user.Level,
            CreatedAt = user.CreatedAt,
            IsTeacher = isTeacher,
            Classrooms = classrooms,
            Points = await _interactions.GetTotalPointsAsync(user.Id),
            RankPosition = position,
            PostCount = await _context.Posts.CountAsync(x => x.AuthorId == user.Id),
            CommentCount = await _context.Comments.CountAsync(x => x.AuthorId == user.Id),
            LessonsAttended = await _context.LessonAttendances.CountAsync(x => x.StudentId == user.Id),
            AssignmentsSubmitted = await _context.AssignmentSubmissions.CountAsync(x => x.StudentId == user.Id),
            RecentPosts = recentPosts,
            CanMessage = await _access.CanMessageAsync(viewerId, user.Id)
        };
    }
}
