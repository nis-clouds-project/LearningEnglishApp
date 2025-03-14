using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Controllers.Responses;

namespace Backend.Integrations.Interfaces
{
    /// <summary>
    /// Интерфейс для сервиса перевода текста с использованием Yandex Translator API
    /// </summary>
    public interface ITranslatorService
    {
        /// <summary>
        /// Переводит текст на указанный язык
        /// </summary>
        /// <param name="text">Текст для перевода</param>
        /// <param name="targetLanguage">Целевой язык перевода</param>
        /// <returns>Переведенный текст</returns>
        Task<string> TranslateAsync(string text, string targetLanguage);

        /// <summary>
        /// Получает список поддерживаемых языков
        /// </summary>
        /// <returns>Список поддерживаемых языков</returns>
        Task<List<LanguageInfo>> GetSupportedLanguagesAsync();
    }
}