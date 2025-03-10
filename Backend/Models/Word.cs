namespace Backend.Models;

/// <summary>
/// Класс, представляющий слово для изучения.
/// Содержит информацию о слове, его переводе, категории и времени последнего показа.
/// </summary>
public class Word
{
    /// <summary>
    /// Уникальный идентификатор слова.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Текст слова на изучаемом языке (например, на английском).
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Перевод слова на родной язык пользователя.
    /// </summary>
    public string Translation { get; set; } = string.Empty;

    /// <summary>
    /// Категория, к которой относится слово (например, "Еда", "Технологии" и т.д.).
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Время последнего показа слова пользователям.
    /// По умолчанию равно <see cref="DateTime.MinValue"/>, что означает, что слово еще не показывалось.
    /// </summary>
    public DateTime LastShown { get; set; }

    /// <summary>
    /// Переопределение метода <see cref="ToString"/> для удобного вывода слова.
    /// </summary>
    /// <returns>Текст слова.</returns>
    public override string ToString()
    {
        return $"{Text} - {Translation} ({Category})";
    }
}