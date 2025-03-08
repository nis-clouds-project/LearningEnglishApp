using System.Text.Json.Serialization;

namespace Backend.Models;

/// <summary>
/// Класс, представляющий пользователя.
/// Содержит информацию о пользователе, включая идентификатор, списки изученных и просмотренных слов.
/// </summary>
public class User
{
    /// <summary>
    /// Создает нового пользователя с указанным идентификатором.
    /// </summary>
    /// <param name="id">Уникальный идентификатор пользователя.</param>
    public User(long id)
    {
        Id = id;
        LearnedWordIds = new List<int>();
        ViewedWordsWordIds = new List<int>();
        UserAiUsage = new Dictionary<DateTime, int>();
    }

    /// <summary>
    /// Конструктор без параметров для Entity Framework
    /// </summary>
    public User()
    {
        LearnedWordIds = new List<int>();
        ViewedWordsWordIds = new List<int>();
        UserAiUsage = new Dictionary<DateTime, int>();
    }

    /// <summary>
    /// Уникальный идентификатор пользователя.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Список идентификаторов слов, которые пользователь выучил.
    /// </summary>
    public List<int> LearnedWordIds { get; set; } = new();

    /// <summary>
    /// Список идентификаторов слов, которые пользователь просмотрел.
    /// </summary>
    public List<int> ViewedWordsWordIds { get; set; } = new();
    
    public Dictionary<DateTime, int> UserAiUsage { get; set; } = new();
}