using Lingua.Data;
using Lingua.Extensions;
using Lingua.Models;
using Lingua.ViewModels.Posts;
using Microsoft.EntityFrameworkCore;

namespace Lingua.Services;

/// <summary>
/// Leitura e escrita dos murais. A API e as páginas Blazor passam por aqui, para que a
/// regra de visibilidade e a pontuação sejam as mesmas nos dois caminhos.
/// </summary>
public class PostService
{
    private readonly LinguaDataContext _context;
    private readonly AccessService _access;
    private readonly InteractionService _interactions;

    public PostService(
        LinguaDataContext context,
        AccessService access,
        InteractionService interactions)
    {
        _context = context;
        _access = access;
        _interactions = interactions;
    }

    public async Task<List<ListPostsViewModel>> GetFeedAsync(
        int userId,
        int? classroomId = null,
        string? topicSlug = null,
        int page = 0,
        int pageSize = 20)
    {
        var isStaff = await _access.IsStaffAsync(userId);
        var visibleIds = await _access.VisibleClassroomIdsAsync(userId);

        var query = _access.VisiblePosts(_context.Posts.AsNoTracking(), userId, visibleIds, isStaff);

        if (classroomId.HasValue)
        {
            if (!visibleIds.Contains(classroomId.Value))
                return new List<ListPostsViewModel>();

            query = query.Where(x => x.ClassroomId == classroomId.Value);
        }

        if (!string.IsNullOrWhiteSpace(topicSlug))
            query = query.Where(x => x.Topic != null && x.Topic.Slug == topicSlug);

        return await query
            .OrderByDescending(x => x.LastUpdateDate)
            .Skip(page * pageSize)
            .Take(pageSize)
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
                Topic = x.Topic != null ? x.Topic.Name : null,
                Comments = x.Comments.Count(c => c.Status == ModerationStatus.Published),
                Reactions = x.Reactions.Count,
                ReactedByMe = x.Reactions.Any(r => r.UserId == userId)
            })
            .ToListAsync();
    }

    public async Task<int> CountFeedAsync(int userId, int? classroomId = null, string? topicSlug = null)
    {
        var isStaff = await _access.IsStaffAsync(userId);
        var visibleIds = await _access.VisibleClassroomIdsAsync(userId);

        var query = _access.VisiblePosts(_context.Posts.AsNoTracking(), userId, visibleIds, isStaff);

        if (classroomId.HasValue)
            query = query.Where(x => x.ClassroomId == classroomId.Value);

        if (!string.IsNullOrWhiteSpace(topicSlug))
            query = query.Where(x => x.Topic != null && x.Topic.Slug == topicSlug);

        return await query.CountAsync();
    }

    public async Task<PostDetailsViewModel?> GetDetailsAsync(int userId, int postId)
    {
        var isStaff = await _access.IsStaffAsync(userId);
        var visibleIds = await _access.VisibleClassroomIdsAsync(userId);

        var post = await _access
            .VisiblePosts(_context.Posts.AsNoTracking(), userId, visibleIds, isStaff)
            .Include(x => x.Author)
            .Include(x => x.Classroom)
            .Include(x => x.Topic)
            .Include(x => x.Tags)
            .Include(x => x.Reactions)
            .Include(x => x.Comments.OrderBy(c => c.CreatedAt))
            .ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(x => x.Id == postId);

        if (post == null)
            return null;

        return new PostDetailsViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Summary = post.Summary,
            Body = post.Body,
            Slug = post.Slug,
            CreatedAt = post.CreatedAt,
            LastUpdateDate = post.LastUpdateDate,
            Status = post.Status,
            AuthorId = post.AuthorId,
            AuthorName = post.Author.Name,
            AuthorSlug = post.Author.Slug,
            AuthorImage = post.Author.Image,
            ClassroomId = post.ClassroomId,
            Classroom = post.Classroom?.Name,
            Topic = post.Topic?.Name,
            Tags = post.Tags.Select(t => t.Name).ToList(),
            Reactions = post.Reactions.Count,
            ReactedByMe = post.Reactions.Any(r => r.UserId == userId),
            CanEdit = post.AuthorId == userId || isStaff,
            CanModerate = isStaff,
            Comments = post.Comments
                .Where(c => c.Status == ModerationStatus.Published || isStaff || c.AuthorId == userId)
                .Select(c => new CommentViewModel
                {
                    Id = c.Id,
                    Body = c.Body,
                    CreatedAt = c.CreatedAt,
                    Status = c.Status,
                    AuthorId = c.AuthorId,
                    AuthorName = c.Author.Name,
                    AuthorSlug = c.Author.Slug,
                    AuthorImage = c.Author.Image
                })
                .ToList()
        };
    }

    public async Task<(Post? Post, string? Error)> CreateAsync(int authorId, EditorPostViewModel model)
    {
        if (model.ClassroomId.HasValue &&
            !await _access.CanAccessClassroomAsync(authorId, model.ClassroomId.Value))
        {
            return (null, "Você não participa desta turma");
        }

        var post = new Post
        {
            Title = model.Title.Trim(),
            Summary = model.Summary.Trim(),
            Body = model.Body,
            Slug = await BuildUniqueSlugAsync(model.Title),
            Scope = model.ClassroomId.HasValue ? PostScope.Classroom : PostScope.General,
            ClassroomId = model.ClassroomId,
            TopicId = model.TopicId,
            AuthorId = authorId,
            CreatedAt = DateTime.UtcNow,
            LastUpdateDate = DateTime.UtcNow
        };

        foreach (var tag in await ResolveTagsAsync(model.Tags))
            post.Tags.Add(tag);

        await _context.Posts.AddAsync(post);
        _interactions.Track(authorId, InteractionType.PostCreated, model.ClassroomId);
        await _context.SaveChangesAsync();

        return (post, null);
    }

    public async Task<(bool Ok, string? Error)> UpdateAsync(int userId, int postId, EditorPostViewModel model)
    {
        var post = await _context.Posts.Include(x => x.Tags).FirstOrDefaultAsync(x => x.Id == postId);
        if (post == null)
            return (false, "Post não encontrado");

        if (post.AuthorId != userId && !await _access.IsStaffAsync(userId))
            return (false, "Você não pode editar este post");

        post.Title = model.Title.Trim();
        post.Summary = model.Summary.Trim();
        post.Body = model.Body;
        post.TopicId = model.TopicId;
        post.LastUpdateDate = DateTime.UtcNow;

        post.Tags.Clear();
        foreach (var tag in await ResolveTagsAsync(model.Tags))
            post.Tags.Add(tag);

        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> DeleteAsync(int userId, int postId)
    {
        var post = await _context.Posts.FirstOrDefaultAsync(x => x.Id == postId);
        if (post == null)
            return (false, "Post não encontrado");

        if (post.AuthorId != userId && !await _access.IsStaffAsync(userId))
            return (false, "Você não pode apagar este post");

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();

        return (true, null);
    }

    /// <summary>Liga e desliga a curtida do usuário no post. Devolve o estado final.</summary>
    public async Task<(bool Reacted, int Total)> ToggleReactionAsync(int userId, int postId)
    {
        var post = await _context.Posts.FirstOrDefaultAsync(x => x.Id == postId);
        if (post == null)
            return (false, 0);

        var reaction = await _context
            .Reactions
            .FirstOrDefaultAsync(x => x.PostId == postId && x.UserId == userId);

        if (reaction == null)
        {
            await _context.Reactions.AddAsync(new Reaction { PostId = postId, UserId = userId });
            _interactions.Track(userId, InteractionType.ReactionGiven, post.ClassroomId, postId);

            if (post.AuthorId != userId)
                _interactions.Track(post.AuthorId, InteractionType.ReactionReceived, post.ClassroomId, postId);
        }
        else
        {
            _context.Reactions.Remove(reaction);
        }

        await _context.SaveChangesAsync();

        var total = await _context.Reactions.CountAsync(x => x.PostId == postId);

        return (reaction == null, total);
    }

    public async Task<(Comment? Comment, string? Error)> AddCommentAsync(int userId, int postId, string body)
    {
        var post = await _context.Posts.FirstOrDefaultAsync(x => x.Id == postId);
        if (post == null)
            return (null, "Post não encontrado");

        if (post.ClassroomId.HasValue &&
            !await _access.CanAccessClassroomAsync(userId, post.ClassroomId.Value))
        {
            return (null, "Você não participa desta turma");
        }

        var comment = new Comment
        {
            PostId = postId,
            AuthorId = userId,
            Body = body.Trim()
        };

        await _context.Comments.AddAsync(comment);
        _interactions.Track(userId, InteractionType.CommentCreated, post.ClassroomId, postId);
        await _context.SaveChangesAsync();

        return (comment, null);
    }

    /// <summary>Moderação do professor: esconde ou devolve ao ar um post.</summary>
    public async Task<bool> SetPostStatusAsync(int postId, ModerationStatus status)
    {
        var post = await _context.Posts.FirstOrDefaultAsync(x => x.Id == postId);
        if (post == null)
            return false;

        post.Status = status;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> SetCommentStatusAsync(int commentId, ModerationStatus status)
    {
        var comment = await _context.Comments.FirstOrDefaultAsync(x => x.Id == commentId);
        if (comment == null)
            return false;

        comment.Status = status;
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>Aproveita as tags que já existem e cria só as novas.</summary>
    private async Task<List<Tag>> ResolveTagsAsync(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new List<Tag>();

        var names = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => new { Name = x, Slug = x.ToSlug() })
            .Where(x => !string.IsNullOrEmpty(x.Slug))
            .DistinctBy(x => x.Slug)
            .Take(8)
            .ToList();

        if (names.Count == 0)
            return new List<Tag>();

        var slugs = names.Select(x => x.Slug).ToList();
        var existing = await _context.Tags.Where(x => slugs.Contains(x.Slug)).ToListAsync();

        foreach (var item in names.Where(x => existing.All(t => t.Slug != x.Slug)))
        {
            var tag = new Tag { Name = item.Name, Slug = item.Slug };
            await _context.Tags.AddAsync(tag);
            existing.Add(tag);
        }

        return existing;
    }

    private async Task<string> BuildUniqueSlugAsync(string title)
    {
        var baseSlug = title.ToSlug();
        if (string.IsNullOrEmpty(baseSlug))
            baseSlug = "post";

        var slug = baseSlug;
        var suffix = 1;

        while (await _context.Posts.AnyAsync(x => x.Slug == slug))
            slug = $"{baseSlug}-{++suffix}";

        return slug;
    }
}
