namespace Frontend.Models;

/// <summary>
/// Представляет категорию слов в словаре пользователя.
/// </summary>
public class VocabularyCategory
{
    /// <summary>
    /// Название категории.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Список слов в категории.
    /// </summary>
    public List<VocabularyWord> Words { get; set; } = new();
}

/// <summary>
/// Представляет слово в словаре пользователя.
/// </summary>
public class VocabularyWord
{
    /// <summary>
    /// Идентификатор слова.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Текст слова на английском.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Перевод слова на русский.
    /// </summary>
    public string Translation { get; set; } = string.Empty;

    /// <summary>
    /// Время последнего просмотра слова.
    /// </summary>
    public DateTime LastShown { get; set; }
} 