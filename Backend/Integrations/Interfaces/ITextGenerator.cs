using Backend.Models;

namespace Backend.Integrations.Interfaces;


/// <summary>
/// Интерфейс для генерации текста с использованием ИИ.
/// </summary>
public interface ITextGenerator
{
    /// <summary>
    /// Генерирует текст на основе сообщения пользователя.
    /// </summary>
    /// <returns>Сгенерированный текст.</returns>
    /// <exception cref="ArgumentException">Если сообщение пустое или null.</exception>
    /// <exception cref="InvalidOperationException">Если произошла ошибка при генерации текста.</exception>
    string GenerateText(List<Word> words);
}