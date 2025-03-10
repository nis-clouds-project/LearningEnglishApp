using System.Text.Json.Serialization;

namespace Frontend.Models
{
    /// <summary>
    /// Модель слова для изучения.
    /// </summary>
    public class Word
    {
        /// <summary>
        /// Идентификатор слова.
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Текст слова на английском языке.
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Перевод слова на русский язык.
        /// </summary>
        [JsonPropertyName("translation")]
        public string Translation { get; set; } = string.Empty;

        /// <summary>
        /// Категория слова.
        /// </summary>
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Дата последнего показа слова пользователю.
        /// </summary>
        [JsonPropertyName("lastShown")]
        public DateTime LastShown { get; set; }

        /// <summary>
        /// Переопределение метода ToString для удобного вывода слова.
        /// </summary>
        public override string ToString() => $"{Text} - {Translation} ({Category})";
    }
}