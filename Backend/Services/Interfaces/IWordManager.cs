using Backend.Models;

namespace Backend.Services.Interfaces;

/// <summary>
/// Интерфейс для управления словами.
/// Предоставляет методы для работы со словами, включая их добавление, получение и генерацию случайных слов.
/// </summary>
public interface IWordManager
{
    /// <summary>
    /// Получает список случайных слов для генерации текста.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="category">Категория слов (опционально). Если не указана, выбираются слова из всех категорий.</param>
    /// <returns>Список слов.</returns>
    /// <exception cref="NoWordsAvailableException">Выбрасывается, если нет доступных слов для пользователя.</exception>
    /// <exception cref="UserNotFoundException">Выбрасывается, если пользователь не найден.</exception>
    Task<List<Word>> GetRandomWordsForGeneratingTextAsync(long userId, string? category = null);

    /// <summary>
    /// Получает случайное слово для пользователя.
    /// </summary>
    /// <param name="user">Пользователь.</param>
    /// <param name="category">Категория слова (опционально).</param>
    /// <returns>Случайное слово или null, если подходящих слов нет.</returns>
    Task<Word?> GetRandomWordAsync(User user, string? category = null);

    /// <summary>
    /// Добавляет слово в словарь пользователя.
    /// </summary>
    /// <param name="user">Пользователь.</param>
    /// <param name="wordId">Идентификатор слова.</param>
    /// <returns>true, если слово успешно добавлено; иначе false.</returns>
    Task<bool> AddWordToVocabularyAsync(User user, int wordId);

    /// <summary>
    /// Получает все слова указанной категории.
    /// </summary>
    /// <param name="category">Категория слов.</param>
    /// <returns>Список слов указанной категории.</returns>
    Task<List<Word>> GetWordsByCategory(string category);

    /// <summary>
    /// Получает случайное слово для изучения.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="category">Категория слова.</param>
    /// <returns>Слово для изучения.</returns>
    /// <exception cref="NoWordsAvailableException">Выбрасывается, если нет доступных слов для пользователя.</exception>
    /// <exception cref="UserNotFoundException">Выбрасывается, если пользователь не найден.</exception>
    Task<Word> GetRandomWordForLearningAsync(long userId, string category);

    /// <summary>
    /// Получает слово по его идентификатору.
    /// </summary>
    /// <param name="wordId">Идентификатор слова.</param>
    /// <returns>Объект слова.</returns>
    /// <exception cref="KeyNotFoundException">Выбрасывается, если слово не найдено.</exception>
    Task<Word> GetWordByIdAsync(int wordId);

    /// <summary>
    /// Добавляет пользовательское слово.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="word">Слово для добавления.</param>
    /// <returns>Добавленное слово.</returns>
    /// <exception cref="KeyNotFoundException">Выбрасывается, если пользователь не найден.</exception>
    Task<Word> AddCustomWordAsync(long userId, Word word);
}