using Backend.Models;

namespace Backend.Services.Interfaces;

public interface ITextGenerationService
{
    /// <summary>
    /// Генерирует текст на основе списка слов с помощью GigaChat
    /// </summary>
    /// <param name="words">Список слов для генерации текста</param>
    /// <returns>Сгенерированный текст</returns>
    Task<string> GenerateTextAsync(List<Word> words);
} 