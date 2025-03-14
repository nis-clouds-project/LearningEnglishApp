using Backend.Integrations;
using Backend.Models;

namespace Backend.Integrations.Interfaces;


/// <summary>
/// Интерфейс для сервиса генерации текста.
/// </summary>
public interface ITextGenerator
{
    /// <summary>
    /// Генерирует текст на основе слов с их переводами.
    /// </summary>
    /// <param name="wordsWithTranslations">Словарь слов и их переводов.</param>
    /// <returns>Сгенерированный текст.</returns>
    Task<GeneratedText> GenerateTextWithTranslationsAsync(IDictionary<string, string> wordsWithTranslations);
}