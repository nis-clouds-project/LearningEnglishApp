using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

/// <summary>
/// Модель пользователя, хранящая только идентификаторы изученных и собственных слов
/// </summary>
public class User
{
    /// <summary>
    /// Уникальный идентификатор пользователя (Telegram ID)
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// Список идентификаторов изученных слов, хранится в формате JSON
    /// </summary>
    public List<long> learned_words { get; set; } = new();

    /// <summary>
    /// Список идентификаторов пользовательских слов, хранится в формате JSON
    /// </summary>
    public List<long> my_words { get; set; } = new();

    public Dictionary<DateTime, int> UserAiUsage { get; set; }

    // Навигационные свойства для отношений с Word
    public virtual ICollection<Word> LearnedWords { get; set; }
    public virtual ICollection<Word> ViewedWords { get; set; }
    public virtual ICollection<Word> CustomWords { get; set; }

    public User(long id)
    {
        Id = id;
        learned_words = new List<long>();
        my_words = new List<long>();
        UserAiUsage = new Dictionary<DateTime, int>();
        
        // Инициализация навигационных свойств
        LearnedWords = new List<Word>();
        ViewedWords = new List<Word>();
        CustomWords = new List<Word>();
    }

    public User()
    {
        learned_words = new List<long>();
        my_words = new List<long>();
        UserAiUsage = new Dictionary<DateTime, int>();
        
        // Инициализация навигационных свойств
        LearnedWords = new List<Word>();
        ViewedWords = new List<Word>();
        CustomWords = new List<Word>();
    }
}