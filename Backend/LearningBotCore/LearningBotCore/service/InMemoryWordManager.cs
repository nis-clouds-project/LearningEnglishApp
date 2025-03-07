using LearningBotCore.exceptions;
using LearningBotCore.model;
using LearningBotCore.service.interfaces;

namespace LearningBotCore.service;

/// <summary>
/// Реализация интерфейса <see cref="IWordManager"/> для управления словами в памяти.
/// Хранит слова в словаре и предоставляет методы для работы с ними.
/// </summary>
public class InMemoryWordManager(IUserManager userManager) : IWordManager
{
    // Словарь для хранения слов, где ключ — идентификатор слова (int), а значение — объект Word.
    private readonly Dictionary<int, Word> _words = new()
    {
        { 1, new Word(1, "apple", "яблоко", CategoryType.Food) },
        { 2, new Word(2, "beard", "борода", CategoryType.Appearance) },
        { 3, new Word(3, "chair", "стул", CategoryType.Furniture) },
        { 4, new Word(4, "contract", "контракт", CategoryType.Business) },
        { 5, new Word(5, "laptop", "ноутбук", CategoryType.Technology) },
        { 6, new Word(6, "jacket", "куртка", CategoryType.Clothing) },
        { 7, new Word(7, "passport", "паспорт", CategoryType.Travel) },
        { 8, new Word(8, "honesty", "честность", CategoryType.PersonalityTraits) },
        { 9, new Word(9, "recycling", "переработка", CategoryType.Ecology) },
        { 10, new Word(10, "sibling", "брат/сестра", CategoryType.Family) },
        { 11, new Word(11, "brand", "бренд", CategoryType.Marketing) },
        { 12, new Word(12, "currency", "валюта", CategoryType.Money) },
        { 13, new Word(13, "subway", "метро", CategoryType.City) },
        { 14, new Word(14, "banana", "банан", CategoryType.Food) },
        { 15, new Word(15, "cherry", "вишня", CategoryType.Food) }
    };

    /// <summary>
    /// Добавляет слово в словарь пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="wordId">Идентификатор слова.</param>
    /// <exception cref="WordNotFoundException">Выбрасывается, если слово не найдено в базе.</exception>
    /// <exception cref="WordAlreadyInVocabularyException">Выбрасывается, если слово уже есть в словаре пользователя.</exception>
    /// <exception cref="UserNotFoundException">Выбрасывается, если пользователь не найден.</exception>
    public void AddWordInUserVocabulary(long userId, int wordId)
    {
        var user = GetUser(userId);
        CheckIsValidWord(userId, wordId);
        user.LearnedWordIds.Add(wordId);
    }

    /// <summary>
    /// Добавляет пользовательское слово в словарь пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="wordText">Текст слова.</param>
    /// <exception cref="WordNotFoundException">Выбрасывается, если слово не найдено в базе.</exception>
    /// <exception cref="WordAlreadyInVocabularyException">Выбрасывается, если слово уже есть в словаре пользователя.</exception>
    /// <exception cref="UserNotFoundException">Выбрасывается, если пользователь не найден.</exception>
    public void AddCustomWordInUserVocabulary(long userId, string wordText)
    {
        var user = GetUser(userId);
        CheckIsValidWord(userId, wordText);
        var wordId = _words.First(w => w.Value.Text.Equals(wordText)).Key;
        user.LearnedWordIds.Add(wordId);
    }

    /// <summary>
    /// Получает случайные слова для генерации текста.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="category">Категория слов (опционально). Если не указана, выбираются слова из всех категорий.</param>
    /// <returns>Список слов.</returns>
    /// <exception cref="WordAlreadyInVocabularyException">Выбрасывается, если нет доступных слов для пользователя.</exception>
    /// <exception cref="UserNotFoundException">Выбрасывается, если пользователь не найден.</exception>
    public List<Word> GetRandomWordsForGeneratingText(long userId, CategoryType? category = null)
    {
        var user = GetUser(userId);

        // Выборка по категории.
        var userWords = user.LearnedWordIds
            .Select(id => _words[id])
            .Where(word => category == null || word.Category == category)
            .ToList();

        if (!userWords.Any())
        {
            throw new NoWordsAvailableException();
        }

        // Выборка по менее популярным словам, берется 10 самых старых.
        var sortedWords = userWords
            .OrderBy(word => word.LastShown)
            .Take(10)
            .ToList();

        // Присваиваем выбранному слову последнее время выборки.
        sortedWords.ForEach(word => word.LastShown = DateTime.Now);

        return sortedWords;
    }

    /// <summary>
    /// Получает случайное слово для изучения.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="category">Категория слова.</param>
    /// <returns>Идентификатор слова.</returns>
    /// <exception cref="InvalidOperationException">Выбрасывается, если нет доступных слов для пользователя.</exception>
    /// <exception cref="UserNotFoundException">Выбрасывается, если пользователь не найден.</exception>
    public int GetRandomWordForLearning(long userId, CategoryType category)
    {
        var user = GetUser(userId);

        // Выборка по категории + проверяется корректность и отсутствие просмотренности пользователем.
        var userWords = _words.Values
            .Where(word => word.Category == category &&
                           !IsExistWordInUserVocabulary(userId, word.Id) &&
                           !IsWordCheckedByUser(userId, word.Id))
            .ToList();

        if (!userWords.Any())
        {
            throw new NoWordsAvailableException();
        }

        // Выборка по менее популярным словам, берется 10 самых старых.
        var word = userWords
            .OrderBy(word => word.LastShown)
            .First();

        user.ViewedWordsWordIds.Add(word.Id);
        word.LastShown = DateTime.Now;

        return word.Id;
    }

    /// <summary>
    /// Проверяет, было ли слово проверено пользователем.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="wordId">Идентификатор слова.</param>
    /// <returns>True, если слово было проверено, иначе — False.</returns>
    /// <exception cref="UserNotFoundException">Выбрасывается, если пользователь не найден.</exception>
    private bool IsWordCheckedByUser(long userId, int wordId)
    {
        var user = GetUser(userId);
        return user.ViewedWordsWordIds.Contains(wordId);
    }

    /// <summary>
    /// Получает слово по его идентификатору.
    /// </summary>
    /// <param name="wordId">Идентификатор слова.</param>
    /// <returns>Объект слова.</returns>
    /// <exception cref="WordNotFoundException">Выбрасывается, если слово не найдено в базе.</exception>
    public Word GetWordById(int wordId)
    {
        if (!_words.ContainsKey(wordId))
        {
            throw new WordNotFoundException();
        }

        return _words[wordId];
    }

    /// <summary>
    /// Получает пользователя по его идентификатору.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <returns>Объект пользователя.</returns>
    /// <exception cref="UserNotFoundException">Выбрасывается, если пользователь не найден.</exception>
    private User GetUser(long userId)
    {
        return userManager.GetUser(userId);
    }

    /// <summary>
    /// Проверяет, существует ли слово в базе.
    /// </summary>
    /// <param name="wordText">Текст слова.</param>
    /// <returns>True, если слово существует, иначе — False.</returns>
    private bool IsExistWord(string wordText)
    {
        return _words.Values.Any(w => w.Text.Equals(wordText));
    }

    /// <summary>
    /// Проверяет, существует ли слово в базе.
    /// </summary>
    /// <param name="wordId">Идентификатор слова.</param>
    /// <returns>True, если слово существует, иначе — False.</returns>
    private bool IsExistWord(int wordId)
    {
        return _words.ContainsKey(wordId);
    }

    /// <summary>
    /// Проверяет, есть ли слово в словаре пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="wordId">Идентификатор слова.</param>
    /// <returns>True, если слово есть в словаре пользователя, иначе — False.</returns>
    /// <exception cref="UserNotFoundException">Выбрасывается, если пользователь не найден.</exception>
    private bool IsExistWordInUserVocabulary(long userId, int wordId)
    {
        var user = GetUser(userId);
        return user.LearnedWordIds.Contains(wordId);
    }

    /// <summary>
    /// Проверяет, есть ли слово в словаре пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="customWord">Текст слова.</param>
    /// <returns>True, если слово есть в словаре пользователя, иначе — False.</returns>
    /// <exception cref="UserNotFoundException">Выбрасывается, если пользователь не найден.</exception>
    private bool IsExistWordInUserVocabulary(long userId, string customWord)
    {
        var user = GetUser(userId);
        var word = _words.First(w => w.Value.Text.Equals(customWord)).Key;
        return user.LearnedWordIds.Contains(word);
    }

    /// <summary>
    /// Проверяет, можно ли добавить слово в словарь пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="wordId">Идентификатор слова.</param>
    /// <exception cref="WordNotFoundException">Выбрасывается, если слово не найдено в базе.</exception>
    /// <exception cref="WordAlreadyInVocabularyException">Выбрасывается, если слово уже есть в словаре пользователя.</exception>
    private void CheckIsValidWord(long userId, int wordId)
    {
        if (!IsExistWord(wordId))
        {
            throw new WordNotFoundException();
        }

        if (IsExistWordInUserVocabulary(userId, wordId))
        {
            throw new WordAlreadyInVocabularyException();
        }
    }

    /// <summary>
    /// Проверяет, можно ли добавить пользовательское слово в словарь пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="word">Текст слова.</param>
    /// <exception cref="WordNotFoundException">Выбрасывается, если слово не найдено в базе.</exception>
    /// <exception cref="WordAlreadyInVocabularyException">Выбрасывается, если слово уже есть в словаре пользователя.</exception>
    private void CheckIsValidWord(long userId, string word)
    {
        if (!IsExistWord(word))
        {
            throw new WordNotFoundException();
        }

        if (IsExistWordInUserVocabulary(userId, word))
        {
            throw new WordAlreadyInVocabularyException();
        }
    }
}