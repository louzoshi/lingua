using Lingua.Models;

namespace Lingua.ViewModels.Ranking;

public class RankingEntryViewModel
{
    public int Position { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Image { get; set; }
    public EnglishLevel Level { get; set; }
    public int Points { get; set; }
    public int Interactions { get; set; }
}
