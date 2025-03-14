namespace Frontend.Models;

public class User
{
    public long Id { get; set; }
    public List<long> LearnedWords { get; set; } = new();
    public List<long> MyWords { get; set; } = new();
    public Dictionary<DateTime, int> UserAiUsage { get; set; } = new();
} 