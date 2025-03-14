
namespace Backend.Controllers.Responses
{
    public class TranslationResponse
    {
        /// <summary>
        /// Исходный текст
    /// </summary>
    public string OriginalText { get; set; } = string.Empty;

    /// <summary>
    /// Переведенный текст
    /// </summary>
    public string TranslatedText { get; set; } = string.Empty;

    /// <summary>
    /// Целевой язык перевода
    /// </summary>
    public string TargetLanguage { get; set; } = string.Empty;

    /// <summary>
    /// Источник перевода (database/yandex)
    /// </summary>
    public string Source { get; set; } = string.Empty;
}
}