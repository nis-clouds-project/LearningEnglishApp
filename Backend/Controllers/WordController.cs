using Backend.Exceptions;
using Backend.Integrations.Interfaces;
using Backend.Models;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    /// <summary>
    /// Контроллер для работы со словами.
    /// Предоставляет API для управления словами, включая получение случайных слов и добавление слов в словарь пользователя.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")] // Базовый маршрут для контроллера: /api/word
    public class WordController : ControllerBase
    {
        private readonly IWordManager _wordManager;
        private readonly IUserManager _userManager;
        private readonly ITextGenerator _textGenerator;

        /// <summary>
        /// Конструктор для внедрения зависимости IWordManager и IUserManager.
        /// </summary>
        /// <param name="wordManager">Сервис для управления словами.</param>
        /// <param name="userManager">Сервис для управления пользователями.</param>
        /// <param name="textGenerator">Сервис для генерации текста.</param>
        public WordController(IWordManager wordManager, IUserManager userManager, ITextGenerator textGenerator)
        {
            _wordManager = wordManager;
            _userManager = userManager;
            _textGenerator = textGenerator;
        }

        /// <summary>
        /// Генерирует текст на основе слов из словаря пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="category">Категория слов (опционально).</param>
        /// <returns>Сгенерированный текст.</returns>
        /// <response code="200">Текст успешно сгенерирован.</response>
        /// <response code="404">Пользователь не найден или нет доступных слов.</response>
        [HttpGet("generate-text")]
        public async Task<IActionResult> GenerateText([FromQuery] long userId, [FromQuery] string? category = null)
        {
            try
            {
                var words = await _wordManager.GetRandomWordsForGeneratingTextAsync(userId, category);
                if (!words.Any())
                {
                    return NotFound("Недостаточно слов для генерации текста");
                }

                // Создаем словарь слов с переводами для генерации текста
                var wordsWithTranslations = words.ToDictionary(w => w.Text, w => w.Translation);
                
                var generatedText = await _textGenerator.GenerateTextWithTranslationsAsync(wordsWithTranslations);
                return Ok(new
                {
                    englishText = generatedText.EnglishText,
                    russianText = generatedText.RussianText,
                    words = wordsWithTranslations
                });
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (NoWordsAvailableException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Получает случайное слово для изучения.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="category">Категория слова.</param>
        /// <returns>Слово для изучения.</returns>
        /// <response code="200">Слово успешно получено.</response>
        /// <response code="403">Нет доступных слов для изучения.</response>
        /// <response code="404">Пользователь не найден.</response>
        [HttpGet("random")]
        public async Task<IActionResult> GetRandomWord(
            [FromQuery] long userId,
            [FromQuery] string? category = null)
        {
            try
            {
                var user = await _userManager.GetUserAsync(userId);
                if (user == null)
                    return NotFound($"Пользователь с ID {userId} не найден");

                var word = await _wordManager.GetRandomWordAsync(user, category);
                if (word == null)
                    return NotFound("Подходящих слов не найдено");

                return Ok(word);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Добавляет слово в словарь пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="wordId">Идентификатор слова.</param>
        /// <returns>Результат операции.</returns>
        /// <response code="200">Слово успешно добавлено.</response>
        /// <response code="403">Слово уже есть в словаре пользователя.</response>
        /// <response code="404">Слово или пользователь не найдены.</response>
        [HttpPost("vocabulary/add")]
        public async Task<IActionResult> AddWordToVocabulary(
            [FromQuery] long userId,
            [FromQuery] int wordId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(userId);
                var success = await _wordManager.AddWordToVocabularyAsync(user, wordId);
                if (!success)
                    return NotFound($"Слово с ID {wordId} не найдено");

                return Ok();
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Получает случайное слово для изучения.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="category">Категория слова.</param>
        /// <returns>Слово для изучения.</returns>
        /// <response code="200">Слово успешно получено.</response>
        /// <response code="403">Нет доступных слов для изучения.</response>
        /// <response code="404">Пользователь не найден.</response>
        [HttpGet("random-word")]
        public async Task<IActionResult> GetRandomWordForLearningAsync(
            [FromQuery] long userId,
            [FromQuery] string category)
        {
            try
            {
                var word = await _wordManager.GetRandomWordForLearningAsync(userId, category);
                return Ok(word);
            }
            catch (NoWordsAvailableException ex)
            {
                return StatusCode(409, ex.Message);
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Непредвиденная ошибка");
            }
        }

        /// <summary>
        /// Получает слово по его идентификатору.
        /// </summary>
        /// <param name="wordId">Идентификатор слова.</param>
        /// <returns>Объект слова.</returns>
        /// <response code="200">Слово успешно получено.</response>
        /// <response code="404">Слово не найдено.</response>
        [HttpGet("{wordId}")]
        public async Task<IActionResult> GetWordById(int wordId)
        {
            try
            {
                var word = await _wordManager.GetWordByIdAsync(wordId);
                return Ok(word);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Добавляет новое слово.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="word">Слово для добавления.</param>
        /// <returns>Добавленное слово.</returns>
        /// <response code="200">Слово успешно добавлено.</response>
        /// <response code="404">Пользователь не найден.</response>
        [HttpPost]
        public async Task<IActionResult> AddCustomWord(
            [FromQuery] long userId,
            [FromBody] Word word)
        {
            try
            {
                var addedWord = await _wordManager.AddCustomWordAsync(userId, word);
                return Ok(addedWord);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Получает слова по категории.
        /// </summary>
        /// <param name="category">Категория слов.</param>
        /// <returns>Список слов категории.</returns>
        /// <response code="200">Слова успешно получены.</response>
        /// <response code="404">Слова категории не найдены.</response>
        [HttpGet("category/{category}")]
        public async Task<IActionResult> GetWordsByCategory(string category)
        {
            try
            {
                var words = await _wordManager.GetWordsByCategory(category);
                if (!words.Any())
                    return NotFound($"Слова категории {category} не найдены");

                return Ok(words);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Получает список изученных слов пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="category">Категория слов (опционально).</param>
        /// <returns>Список изученных слов.</returns>
        /// <response code="200">Список слов успешно получен.</response>
        /// <response code="404">Пользователь не найден.</response>
        [HttpGet("learned")]
        public async Task<IActionResult> GetLearnedWords([FromQuery] long userId, [FromQuery] string? category = null)
        {
            try
            {
                var user = await _userManager.GetUserAsync(userId);
                if (user == null)
                    return NotFound($"Пользователь с ID {userId} не найден");

                var words = await _wordManager.GetLearnedWordsAsync(userId, category);
                
                // Группируем слова по категориям для удобного отображения
                var groupedWords = words
                    .GroupBy(w => w.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        Words = g.Select(w => new
                        {
                            w.Id,
                            w.Text,
                            w.Translation,
                            w.LastShown
                        }).OrderBy(w => w.Text).ToList()
                    })
                    .OrderBy(g => g.Category)
                    .ToList();

                return Ok(groupedWords);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}