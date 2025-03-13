using Backend.Controllers.Responses;
using Backend.Exceptions;
using Backend.Integrations.Interfaces;
using Backend.Models;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        private readonly ITextGenerator _textGenerator;
        private readonly ILogger<WordController> _logger;

        /// <summary>
        /// Конструктор для внедрения зависимости IWordManager и IUserManager.
        /// </summary>
        /// <param name="wordManager">Сервис для управления словами.</param>
        /// <param name="userManager">Сервис для управления пользователями.</param>
        /// <param name="textGenerator">Сервис для генерации текста.</param>
        /// <param name="logger">Логгер для логирования.</param>
        /// <param name="context">Контекст базы данных.</param>
        public WordController(IWordManager wordManager, ITextGenerator textGenerator, ILogger<WordController> logger)
        {
            _wordManager = wordManager;
            _textGenerator = textGenerator;
            _logger = logger;
        }

        /// <summary>
        /// Получает все слова из базы данных.
        /// </summary>
        /// <returns>Список всех слов.</returns>
        /// <response code="200">Список слов успешно получен.</response>
        /// <response code="500">Произошла ошибка при получении списка слов.</response>
        [HttpGet("all")]
        public async Task<ActionResult<List<Word>>> GetAllWords()
        {
            try
            {
                var words = await _wordManager.GetAllWordsAsync();
                return Ok(words);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all words");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Генерирует текст на основе слов из словаря пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="categoryId">Идентификатор категории слов (опционально).</param>
        /// <returns>Сгенерированный текст.</returns>
        /// <response code="200">Текст успешно сгенерирован.</response>
        /// <response code="404">Пользователь не найден или нет доступных слов.</response>
        [HttpGet("generate-text")]
        public async Task<IActionResult> GenerateText([FromQuery] long userId, [FromQuery] long? categoryId = null)
        {
            try
            {
                var words = await _wordManager.GetRandomWordsForGeneratingTextAsync(userId, categoryId);
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
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Получает случайное слово для изучения.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="categoryId">Идентификатор категории слов (опционально).</param>
        /// <returns>Слово для изучения.</returns>
        /// <response code="200">Слово успешно получено.</response>
        /// <response code="403">Нет доступных слов для изучения.</response>
        /// <response code="404">Пользователь не найден.</response>
        [HttpGet("random")]
        public async Task<ActionResult<Word>> GetRandomWord([FromQuery] long userId, [FromQuery] long? categoryId = null)
        {
            try
            {
                var word = await _wordManager.GetRandomWordAsync(userId, categoryId);
                if (word == null)
                {
                    return NotFound("No words available for learning");
                }
                return Ok(word);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting random word for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
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
        public async Task<IActionResult> AddWordToVocabulary([FromQuery] long userId, [FromQuery] long wordId)
        {
            try
            {
                var result = await _wordManager.AddWordToVocabularyAsync(userId, wordId);
                if (!result)
                {
                    return BadRequest("Failed to add word to vocabulary");
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding word {WordId} to vocabulary for user {UserId}", wordId, userId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Получает случайное слово для изучения.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="categoryId">Идентификатор категории слов.</param>
        /// <returns>Слово для изучения.</returns>
        /// <response code="200">Слово успешно получено.</response>
        /// <response code="403">Нет доступных слов для изучения.</response>
        /// <response code="404">Пользователь не найден.</response>
        // [HttpGet("random-word")]
        // public async Task<IActionResult> GetRandomWordForLearningAsync(
        //     [FromQuery] long userId,
        //     [FromQuery] long categoryId)
        // {
        //     try
        //     {
        //         var word = await _wordManager.GetRandomWordForLearningAsync(userId, categoryId);
        //         return Ok(word);
        //     }
        //     catch (NoWordsAvailableException ex)
        //     {
        //         return StatusCode(409, ex.Message);
        //     }
        //     catch (UserNotFoundException ex)
        //     {
        //         return NotFound(ex.Message);
        //     }
        //     catch (Exception)
        //     {
        //         return StatusCode(500, "Непредвиденная ошибка");
        //     }
        // }

        /// <summary>
        /// Получает слово по его идентификатору.
        /// </summary>
        /// <param name="wordId">Идентификатор слова.</param>
        /// <returns>Объект слова.</returns>
        /// <response code="200">Слово успешно получено.</response>
        /// <response code="404">Слово не найдено.</response>
        [HttpGet("{wordId}")]
        public async Task<ActionResult<Word>> GetWordById(long wordId)
        {
            try
            {
                var word = await _wordManager.GetWordByIdAsync(wordId);
                if (word == null)
                {
                    return NotFound($"Word with ID {wordId} not found");
                }
                return Ok(word);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting word {WordId}", wordId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Получает слова по категории.
        /// </summary>
        /// <param name="categoryId">Идентификатор категории слов.</param>
        /// <returns>Список слов категории.</returns>
        /// <response code="200">Слова успешно получены.</response>
        /// <response code="404">Слова категории не найдены.</response>
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<List<Word>>> GetWordsByCategory(long categoryId)
        {
            try
            {
                var words = await _wordManager.GetWordsByCategory(categoryId);
                return Ok(words);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting words for category {CategoryId}", categoryId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Получает список изученных слов пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="categoryId">Идентификатор категории слов (опционально).</param>
        /// <returns>Список изученных слов.</returns>
        /// <response code="200">Список слов успешно получен.</response>
        /// <response code="404">Пользователь не найден.</response>
        [HttpGet("learned")]
        public async Task<ActionResult<List<Word>>> GetLearnedWords([FromQuery] long userId, [FromQuery] long? categoryId = null)
        {
            try
            {
                var words = await _wordManager.GetLearnedWordsAsync(userId, categoryId);
                return Ok(words);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting learned words for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Получает список пользовательских слов.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <returns>Список пользовательских слов.</returns>
        /// <response code="200">Список слов успешно получен.</response>
        /// <response code="404">Пользователь не найден.</response>
        [HttpGet("custom")]
        public async Task<ActionResult<List<Word>>> GetCustomWords([FromQuery] long userId)
        {
            try
            {
                var words = await _wordManager.GetUserCustomWordsAsync(userId);
                return Ok(words);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting custom words for user {UserId}", userId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Добавляет новое пользовательское слово.
        /// </summary>
        /// <param name="request">Запрос для добавления пользовательского слова.</param>
        /// <returns>Добавленное слово.</returns>
        /// <response code="200">Слово успешно добавлено.</response>
        /// <response code="400">Некорректный запрос.</response>
        /// <response code="500">Внутренняя ошибка сервера.</response>
        [HttpPost("custom")]
        public async Task<ActionResult<Word>> AddCustomWord([FromBody] CustomWordRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Request body is required");
                }

                if (string.IsNullOrWhiteSpace(request.Text) || string.IsNullOrWhiteSpace(request.Translation))
                {
                    return BadRequest("Text and Translation are required");
                }

                // Получаем категорию "My Words"
                var myWordsCategory = await _wordManager.GetAllCategoriesAsync()
                    .ContinueWith(t => t.Result.FirstOrDefault(c => c.Name == "My Words"));

                if (myWordsCategory == null)
                {
                    return StatusCode(500, "My Words category not found");
                }

                var word = await _wordManager.AddCustomWordAsync(
                    request.UserId,
                    request.Text,
                    request.Translation,
                    myWordsCategory.Id);

                return Ok(word);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding custom word for user {UserId}", request?.UserId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Удаляет пользовательское слово.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="wordId">Идентификатор слова.</param>
        /// <returns>Результат операции.</returns>
        /// <response code="200">Слово успешно удалено.</response>
        /// <response code="404">Слово не найдено или не принадлежит пользователю.</response>
        [HttpDelete("custom/{wordId}")]
        public async Task<IActionResult> DeleteCustomWord([FromQuery] long userId, long wordId)
        {
            try
            {
                var result = await _wordManager.DeleteCustomWordAsync(userId, wordId);
                if (!result)
                {
                    return NotFound("Word not found or not owned by user");
                }
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting custom word {WordId} for user {UserId}", wordId, userId);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Получает все категории
        /// </summary>
        [HttpGet("categories")]
        public async Task<ActionResult<List<Category>>> GetCategories()
        {
            try
            {
                var categories = await _wordManager.GetAllCategoriesAsync();
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Получает случайное слово из категории "My Words" для пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <returns>Случайное слово из категории "My Words".</returns>
        /// <response code="200">Слово успешно получено.</response>
        /// <response code="404">Слова категории не найдены или пользователь не найден.</response>
        [HttpGet("custom/random")]
        public async Task<ActionResult<Word>> GetRandomCustomWord([FromQuery] long userId)
        {
            try
            {
                var word = await _wordManager.GetRandomCustomWordAsync(userId);
                if (word == null)
                {
                    return NotFound("No available words found in My Words category");
                }
                return Ok(new
                {
                    id = word.Id,
                    text = word.Text,
                    translation = word.Translation,
                    category_id = word.category_id,
                    category = word.Category?.Name ?? string.Empty,
                    user_id = word.user_id,
                    is_custom = word.IsCustom,
                    created_at = word.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting random custom word for user {UserId}", userId);
                return StatusCode(500, "Internal server error while getting random word");
            }
        }
    }
}