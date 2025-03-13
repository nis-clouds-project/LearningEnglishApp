namespace Backend.Models
{
    /// <summary>
    /// Модель запроса для добавления пользовательского слова.
    /// </summary>
    public class CustomWordRequest
    {
        /// <summary>
        /// Идентификатор пользователя.
        /// </summary>
        public long UserId { get; set; }

        /// <summary>
        /// Текст слова на английском.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Перевод слова на русский.
        /// </summary>
        public string Translation { get; set; } = string.Empty;

        /// <summary>
        /// Идентификатор категории слова.
        /// </summary>
        public long CategoryId { get; set; }
    }
} 