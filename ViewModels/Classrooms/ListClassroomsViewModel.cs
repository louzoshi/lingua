using Lingua.Models;

namespace Lingua.ViewModels.Classrooms;

public class ListClassroomsViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public EnglishLevel Level { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public int Students { get; set; }
    public int Posts { get; set; }
    public bool IsArchived { get; set; }
}
