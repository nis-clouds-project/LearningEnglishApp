using Backend.Models;

namespace Backend.Services.Interfaces;

/// <summary>
/// Интерфейс для управления словами.
/// Предоставляет методы для работы со словами, включая их добавление, получение и генерацию случайных слов.
/// </summary>
public interface IWordManager
{
    /// <summary>
    /// Получает список всех слов из базы данных.
    /// </summary>
    /// <returns>Список всех слов.</returns>
    Task<List<Word>> GetAllWordsAsync();

    /// <summary>
    /// Получает список случайных слов для генерации текста.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="categoryId">Идентификатор категории слов (опционально).</param>
    /// <returns>Список слов.</returns>
    /// <exception cref="NoWordsAvailableException">Выбрасывается, если нет доступных слов для пользователя.</exception>
    /// <exception cref="UserNotFoundException">Выбрасывается, если пользователь не найден.</exception>
    Task<List<Word>> GetRandomWordsForGeneratingTextAsync(long userId, long? categoryId = null);

    /// <summary>
    /// Получает случайное слово для пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="categoryId">Идентификатор категории слова (опционально).</param>
    /// <returns>Случайное слово или null, если подходящих слов нет.</returns>
    Task<Word?> GetRandomWordAsync(long userId, long? categoryId = null);

    /// <summary>
    /// Добавляет слово в список изученных слов пользователя
    /// </summary>
    Task<bool> AddWordToVocabularyAsync(long userId, long wordId);

    /// <summary>
    /// Получает все слова указанной категории.
    /// </summary>
    /// <param name="categoryId">Идентификатор категории слов.</param>
    /// <returns>Список слов указанной категории.</returns>
    Task<List<Word>> GetWordsByCategory(long categoryId);

    /// <summary>
    /// Получает случайное слово для изучения
    /// </summary>
    Task<Word?> GetRandomWordForLearningAsync(long userId, long categoryId);

    /// <summary>
    /// Получает слово по ID
    /// </summary>
    Task<Word?> GetWordByIdAsync(long wordId);

    /// <summary>
    /// Добавляет пользовательское слово
    /// </summary>
    Task<Word> AddCustomWordAsync(long userId, string text, string translation, long categoryId);

    /// <summary>
    /// Получает список изученных слов пользователя
    /// </summary>
    Task<List<Word>> GetLearnedWordsAsync(long userId, long? categoryId = null);

    /// <summary>
    /// Получает список пользовательских слов.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <returns>Список пользовательских слов.</returns>
    Task<List<Word>> GetUserCustomWordsAsync(long userId);

    /// <summary>
    /// Удаляет пользовательское слово
    /// </summary>
    Task<bool> DeleteCustomWordAsync(long userId, long wordId);

    /// <summary>
    /// Получает все категории из базы данных.
    /// </summary>
    /// <returns>Список всех категорий.</returns>
    Task<List<Category>> GetAllCategoriesAsync();
}