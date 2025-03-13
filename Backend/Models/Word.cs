using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

/// <summary>
/// Модель слова с базовыми полями
/// </summary>
public class Word
{
    /// <summary>
    /// Уникальный идентификатор слова
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// Текст слова на английском языке
    /// </summary>
    [Required]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Перевод слова на русский язык
    /// </summary>
    [Required]
    public string Translation { get; set; } = string.Empty;

    /// <summary>
    /// Идентификатор категории слова
    /// </summary>
    public long? category_id { get; set; }

    /// <summary>
    /// Идентификатор пользователя-создателя слова (-1 для общих слов)
    /// </summary>
    public long user_id { get; set; } = -1;

    /// <summary>
    /// Дата создания слова
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Дата последнего обновления слова
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Является ли слово пользовательским
    /// </summary>
    public bool IsCustom { get; set; }

    /// <summary>
    /// Навигационное свойство для категории
    /// </summary>
    public virtual Category? Category { get; set; }

    /// <summary>
    /// Навигационные свойства для отношений с User
    /// </summary>
    public virtual ICollection<User> LearnedByUsers { get; set; }
    public virtual ICollection<User> ViewedByUsers { get; set; }

    public Word()
    {
        Text = string.Empty;
        Translation = string.Empty;
        CreatedAt = DateTime.UtcNow;
        IsCustom = false;
        
        LearnedByUsers = new List<User>();
        ViewedByUsers = new List<User>();
    }
    
    public Word(string text, string translation, long? categoryId = null, long userId = -1)
    {
        Text = text;
        Translation = translation;
        category_id = categoryId;
        user_id = userId;
        CreatedAt = DateTime.UtcNow;
        IsCustom = false;
        
        LearnedByUsers = new List<User>();
        ViewedByUsers = new List<User>();
    }

    /// <summary>
    /// Переопределение метода <see cref="ToString"/> для удобного вывода слова.
    /// </summary>
    /// <returns>Текст слова.</returns>
    public override string ToString()
    {
        return $"{Text} - {Translation}";
    }
}