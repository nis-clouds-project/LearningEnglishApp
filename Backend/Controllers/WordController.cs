using Backend.Exceptions;
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

        /// <summary>
        /// Конструктор для внедрения зависимости IWordManager.
        /// </summary>
        /// <param name="wordManager">Сервис для управления словами.</param>
        public WordController(IWordManager wordManager)
        {
            _wordManager = wordManager;
        }

        /// <summary>
        /// Получает случайные слова для генерации текста.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="category">Категория слов (опционально).</param>
        /// <returns>Список слов.</returns>
        /// <response code="200">Слова успешно получены.</response>
        /// <response code="403">Нет доступных слов для генерации текста.</response>
        /// <response code="404">Пользователь не найден.</response>
        [HttpGet("random-words")]
        public async Task<IActionResult> GetRandomWordsForGeneratingTextAsync(
            [FromQuery] long userId,
            [FromQuery] CategoryType? category = null)
        {
            try
            {
                var words = await _wordManager.GetRandomWordsForGeneratingTextAsync(userId, category);
                return Ok(words);
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
        /// Добавляет слово в словарь пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="wordId">Идентификатор слова.</param>
        /// <returns>Результат операции.</returns>
        /// <response code="200">Слово успешно добавлено.</response>
        /// <response code="403">Слово уже есть в словаре пользователя.</response>
        /// <response code="404">Слово или пользователь не найдены.</response>
        [HttpPost("add-word")]
        public async Task<IActionResult> AddWordToUserVocabularyAsync(
            [FromQuery] long userId,
            [FromQuery] int wordId)
        {
            try
            {
                await _wordManager.AddWordToUserVocabularyAsync(userId, wordId);
                return Ok();
            }
            catch (WordNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (WordAlreadyInVocabularyException ex)
            {
                return StatusCode(403, ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Непредвиденная ошибка");
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
            [FromQuery] CategoryType category)
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
        [HttpGet("word-by-id")]
        public async Task<IActionResult> GetWordByIdAsync([FromQuery] int wordId)
        {
            try
            {
                var word = await _wordManager.GetWordByIdAsync(wordId);
                return Ok(word);
            }
            catch (WordNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "Непредвиденная ошибка");
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
        [HttpPost("add-custom-word")]
        public async Task<IActionResult> AddCustomWordAsync(
            [FromQuery] long userId,
            [FromBody] Word word)
        {
            try
            {
                var addedWord = await _wordManager.AddCustomWordAsync(userId, word);
                return Ok(addedWord);
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
    }
}