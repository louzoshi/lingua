using Lingua.Models;
using Lingua.ViewModels.Posts;

namespace Lingua.ViewModels.Profile;

public class StudentProfileViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Image { get; set; }
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public EnglishLevel Level { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsTeacher { get; set; }

    public List<string> Classrooms { get; set; } = new();

    public int Points { get; set; }
    public int RankPosition { get; set; }
    public int PostCount { get; set; }
    public int CommentCount { get; set; }
    public int LessonsAttended { get; set; }
    public int AssignmentsSubmitted { get; set; }

    public List<ListPostsViewModel> RecentPosts { get; set; } = new();

    /// <summary>Quem está olhando pode abrir uma DM com este aluno.</summary>
    public bool CanMessage { get; set; }
}
