using LearningBotCore.exceptions;
using LearningBotCore.model;


namespace LearningBotCore.service.interfaces;

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
    List<Word> GetRandomWordsForGeneratingText(long userId, CategoryType? category = null);

    /// <summary>
    /// Добавляет слово в словарь пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="wordId">Идентификатор слова.</param>
    /// <exception cref="WordNotFoundException">Выбрасывается, если слово не найдено в базе.</exception>
    /// <exception cref="WordAlreadyInVocabularyException">Выбрасывается, если слово уже есть в словаре пользователя.</exception>
    /// <exception cref="UserNotFoundException">Выбрасывается, если пользователь не найден.</exception>
    void AddWordInUserVocabulary(long userId, int wordId);

    /// <summary>
    /// Получает случайное слово для изучения.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="category">Категория слова.</param>
    /// <returns>Идентификатор слова.</returns>
    /// <exception cref="NoWordsAvailableException">Выбрасывается, если нет доступных слов для пользователя.</exception>
    /// <exception cref="UserNotFoundException">Выбрасывается, если пользователь не найден.</exception>
    int GetRandomWordForLearning(long userId, CategoryType category);

    /// <summary>
    /// Получает слово по его идентификатору.
    /// </summary>
    /// <param name="wordId">Идентификатор слова.</param>
    /// <returns>Объект слова.</returns>
    /// <exception cref="WordNotFoundException">Выбрасывается, если слово не найдено в базе.</exception>
    Word GetWordById(int wordId);

    /// <summary>
    /// Добавляет пользовательское слово в словарь пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="word">Текст слова.</param>
    /// <exception cref="WordNotFoundException">Выбрасывается, если слово не найдено в базе.</exception>
    /// <exception cref="WordAlreadyInVocabularyException">Выбрасывается, если слово уже есть в словаре пользователя.</exception>
    /// <exception cref="UserNotFoundException">Выбрасывается, если пользователь не найден.</exception>
    void AddCustomWordInUserVocabulary(long userId, string word);
}