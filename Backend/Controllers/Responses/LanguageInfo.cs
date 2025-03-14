namespace Backend.Controllers.Responses
{
    /// <summary>
    /// Модель для представления информации о языке перевода
    /// </summary>
    public class LanguageInfo
    {
        /// <summary>
        /// Код языка
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Название языка
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}