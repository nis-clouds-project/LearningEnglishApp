namespace LearningBotCore.model;

/// <summary>
/// Класс, представляющий слово для изучения.
/// Содержит информацию о слове, его переводе, категории и времени последнего показа.
/// </summary>
public class Word(int id, string text, string translation, CategoryType category)
{
    /// <summary>
    /// Уникальный идентификатор слова.
    /// </summary>
    public int Id { get; } = id;

    /// <summary>
    /// Текст слова на изучаемом языке (например, на английском).
    /// </summary>
    public string Text { get; } = text;

    /// <summary>
    /// Перевод слова на родной язык пользователя.
    /// </summary>
    public string Translation { get; } = translation;

    /// <summary>
    /// Категория, к которой относится слово (например, "Еда", "Технологии" и т.д.).
    /// </summary>
    public CategoryType Category { get; } = category;

    /// <summary>
    /// Время последнего показа слова пользователям.
    /// По умолчанию равно <see cref="DateTime.MinValue"/>, что означает, что слово еще не показывалось.
    /// </summary>
    public DateTime LastShown { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Переопределение метода <see cref="ToString"/> для удобного вывода слова.
    /// </summary>
    /// <returns>Текст слова.</returns>
    public override string ToString()
    {
        return $"{Text}";
    }
}