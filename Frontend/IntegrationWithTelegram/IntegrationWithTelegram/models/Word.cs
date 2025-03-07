using System.Text.Json.Serialization;

namespace IntegrationWithTelegram.models;

/// <summary>
/// Класс, представляющий слово для изучения.
/// Содержит информацию о слове, его переводе, категории и времени последнего показа.
/// </summary>
public class Word
{
    /// <summary>
    /// Уникальный идентификатор слова.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Текст слова на изучаемом языке (например, на английском).
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set;}

    /// <summary>
    /// Перевод слова на родной язык пользователя.
    /// </summary>
    [JsonPropertyName("translation")]
    public string Translation { get; set;}

    /// <summary>
    /// Категория, к которой относится слово (например, "Еда", "Технологии" и т.д.).
    /// </summary>
    [JsonPropertyName("category")]
    public CategoryType Category { get; set;}

    /// <summary>
    /// Время последнего показа слова пользователям.
    /// По умолчанию равно <see cref="DateTime.MinValue"/>, что означает, что слово еще не показывалось.
    /// </summary>
    [JsonPropertyName("lastShown")]
    public DateTime LastShown { get; set;}

    /// <summary>
    /// Переопределение метода <see cref="ToString"/> для удобного вывода слова.
    /// </summary>
    /// <returns>Текст слова.</returns>
    public override string ToString()
    {
        return $"{Text}";
    }
}