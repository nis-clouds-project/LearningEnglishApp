using LearningBotCore.exceptions;
using LearningBotCore.model;
using LearningBotCore.service.interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LearningBotCore.controllers
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
        /// <param name="category">Категория слов (опционально). Если не указана, выбираются слова из всех категорий.</param>
        /// <returns>Список слов.</returns>
        /// <response code="200">Слова успешно получены.</response>
        /// <response code="403">Нет доступных слов для генерации текста.</response>
        /// <response code="404">Пользователь не найден.</response>
        [HttpGet("random-words")]
        public IActionResult GetRandomWordsForGeneratingText([FromQuery] long userId,
            [FromQuery] CategoryType? category)
        {
            try
            {
                // Получаем случайные слова для генерации текста через сервис _wordManager
                var words = _wordManager.GetRandomWordsForGeneratingText(userId, category);
                // Возвращаем список слов с кодом 200 OK
                return Ok(words);
            }
            catch (NoWordsAvailableException ex)
            {
                // Если нет доступных слов, возвращаем 409 Conflict с сообщением об ошибке
                return StatusCode(409, ex.Message);
            }
            catch (UserNotFoundException ex)
            {
                // Если пользователь не найден, возвращаем 404 Not Found с сообщением об ошибке
                return NotFound(ex.Message);
            }
            catch (Exception)
            {
                // В случае непредвиденной ошибки возвращаем 500 Internal Server Error
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
        public IActionResult AddWordInUserVocabulary([FromQuery] long userId, [FromQuery] int wordId)
        {
            try
            {
                // Добавляем слово в словарь пользователя через сервис _wordManager
                _wordManager.AddWordInUserVocabulary(userId, wordId);
                // Возвращаем 200 OK
                return Ok();
            }
            catch (WordNotFoundException ex)
            {
                // Если слово не найдено, возвращаем 404 Not Found с сообщением об ошибке
                return NotFound(ex.Message);
            }
            catch (UserNotFoundException ex)
            {
                // Если пользователь не найден, возвращаем 404 Not Found с сообщением об ошибке
                return NotFound(ex.Message);
            }
            catch (WordAlreadyInVocabularyException ex)
            {
                // Если слово уже есть в словаре пользователя, возвращаем 403 Forbidden с сообщением об ошибке
                return StatusCode(403, ex.Message);
            }
            catch (Exception)
            {
                // В случае непредвиденной ошибки возвращаем 500 Internal Server Error
                return StatusCode(500, "Непредвиденная ошибка");
            }
        }

        /// <summary>
        /// Получает случайное слово для изучения.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="category">Категория слова.</param>
        /// <returns>Идентификатор слова.</returns>
        /// <response code="200">Слово успешно получено.</response>
        /// <response code="403">Нет доступных слов для изучения.</response>
        /// <response code="404">Пользователь не найден.</response>
        [HttpGet("random-word")]
        public IActionResult GetRandomWordForLearning([FromQuery] long userId, [FromQuery] CategoryType category)
        {
            try
            {
                // Получаем случайное слово для изучения через сервис _wordManager
                var wordId = _wordManager.GetRandomWordForLearning(userId, category);
                // Возвращаем идентификатор слова с кодом 200 OK
                return Ok(wordId);
            }
            catch (WordAlreadyInVocabularyException ex)
            {
                // Если слово уже есть в словаре пользователя, возвращаем 403 Forbidden с сообщением об ошибке
                return StatusCode(403, ex.Message);
            }
            catch (NoWordsAvailableException ex)
            {
                // Если нет доступных слов для изучения, возвращаем 409 Conflict с сообщением об ошибке
                return StatusCode(409, ex.Message);
            }
            catch (UserNotFoundException ex)
            {
                // Если пользователь не найден, возвращаем 404 Not Found с сообщением об ошибке
                return NotFound(ex.Message);
            }
            catch (Exception)
            {
                // В случае непредвиденной ошибки возвращаем 500 Internal Server Error
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
        public IActionResult GetWordById([FromQuery] int wordId)
        {
            try
            {
                // Получаем слово по его идентификатору через сервис _wordManager
                var word = _wordManager.GetWordById(wordId);
                // Возвращаем объект слова с кодом 200 OK
                return Ok(word);
            }
            catch (WordNotFoundException ex)
            {
                // Если слово не найдено, возвращаем 404 Not Found с сообщением об ошибке
                return NotFound(ex.Message);
            }
            catch (Exception)
            {
                // В случае непредвиденной ошибки возвращаем 500 Internal Server Error
                return StatusCode(500, "Непредвиденная ошибка");
            }
        }

        /// <summary>
        /// Добавляет пользовательское слово в словарь пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="word">Текст слова.</param>
        /// <returns>Результат операции.</returns>
        /// <response code="200">Слово успешно добавлено.</response>
        /// <response code="403">Слово уже есть в словаре пользователя.</response>
        /// <response code="404">Слово или пользователь не найдены.</response>
        [HttpPost("add-custom-word")]
        public IActionResult AddCustomWordInUserVocabulary([FromQuery] long userId, [FromQuery] string word)
        {
            try
            {
                // Добавляем пользовательское слово в словарь пользователя через сервис _wordManager
                _wordManager.AddCustomWordInUserVocabulary(userId, word);
                // Возвращаем 200 OK
                return Ok();
            }
            catch (WordNotFoundException ex)
            {
                // Если слово не найдено, возвращаем 404 Not Found с сообщением об ошибке
                return NotFound(ex.Message);
            }
            catch (WordAlreadyInVocabularyException ex)
            {
                // Если слово уже есть в словаре пользователя, возвращаем 403 Forbidden с сообщением об ошибке
                return StatusCode(403, ex.Message);
            }
            catch (UserNotFoundException ex)
            {
                // Если пользователь не найден, возвращаем 404 Not Found с сообщением об ошибке
                return NotFound(ex.Message);
            }
            catch (Exception)
            {
                // В случае непредвиденной ошибки возвращаем 500 Internal Server Error
                return StatusCode(500, "Непредвиденная ошибка");
            }
        }
    }
}