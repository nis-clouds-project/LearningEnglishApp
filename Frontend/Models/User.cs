namespace Frontend.Models;

public class User
{
    public long Id { get; set; }
    public List<long> learned_words { get; set; } = new();
    public List<long> my_words { get; set; } = new();
    public Dictionary<DateTime, int> UserAiUsage { get; set; } = new();
} 