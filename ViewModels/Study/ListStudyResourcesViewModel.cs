using Lingua.Models;

namespace Lingua.ViewModels.Study;

public class ListStudyResourcesViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Url { get; set; } = string.Empty;
    public ResourceKind Kind { get; set; }
    public EnglishLevel Level { get; set; }
    public string? Classroom { get; set; }
    public DateTime CreatedAt { get; set; }
}
