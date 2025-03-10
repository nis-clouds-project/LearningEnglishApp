using Backend.Models;

namespace Backend.Integrations.Interfaces;

public record GeneratedText(string EnglishText, string RussianText);

/// <summary>
/// Интерфейс для сервиса генерации текста.
/// </summary>
public interface ITextGenerator
{
    /// <summary>
    /// Генерирует текст на основе списка слов.
    /// </summary>
    /// <param name="words">Список слов для использования в тексте.</param>
    /// <returns>Сгенерированный текст.</returns>
    Task<string> GenerateTextAsync(IEnumerable<string> words);

    /// <summary>
    /// Генерирует текст на основе слов с их переводами.
    /// </summary>
    /// <param name="wordsWithTranslations">Словарь слов и их переводов.</param>
    /// <returns>Сгенерированный текст.</returns>
    Task<GeneratedText> GenerateTextWithTranslationsAsync(IDictionary<string, string> wordsWithTranslations);
}