using Backend.Integrations.Interfaces;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;


namespace Backend.Controllers
{
    /// <summary>
    /// Контроллер для генерации текста на основе списка слов.
    /// Предоставляет API для взаимодействия с сервисом генерации текста.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TextGenerationController : ControllerBase
    {
        private readonly IWordManager _wordManager;
        private readonly ITextGenerator _textGenerator;
        private readonly ILogger<TextGenerationController> _logger;

        /// <summary>
        /// Конструктор для внедрения зависимостей ITextGenerator и ILogger.
        /// </summary>
        /// <param name="wordManager">Сервис для управления словами.</param>
        /// <param name="textGenerator">Сервис для генерации текста.</param>
        /// <param name="logger">Логгер для логирования ошибок.</param>
        public TextGenerationController(
            IWordManager wordManager,
            ITextGenerator textGenerator,
            ILogger<TextGenerationController> logger)
        {
            _wordManager = wordManager;
            _textGenerator = textGenerator;
            _logger = logger;
        }

        /// <summary>
        /// Генерирует текст на основе изученных слов пользователя
        /// </summary>
        /// <param name="userId">ID пользователя</param>
        /// <param name="categoryId">ID категории (опционально)</param>
        /// <returns>Сгенерированный текст с переводом</returns>
        [HttpGet("generate")]
        public async Task<ActionResult<GeneratedTextResponse>> GenerateText(
            [FromQuery] long userId,
            [FromQuery] long? categoryId = null)
        {
            try
            {
                // Получаем изученные слова пользователя
                var words = await _wordManager.GetLearnedWordsAsync(userId, categoryId);
                if (!words.Any())
                {
                    return BadRequest("No learned words found for text generation");
                }

                // Создаем словарь слов с переводами
                var wordsDict = words.ToDictionary(w => w.Text, w => w.Translation);

                // Генерируем текст
                var generatedText = await _textGenerator.GenerateTextWithTranslationsAsync(wordsDict);

                var response = new GeneratedTextResponse
                {
                    EnglishText = generatedText.EnglishText,
                    RussianText = generatedText.RussianText,
                    Words = wordsDict
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating text for userId: {UserId}", userId);
                return StatusCode(500, "Error generating text");
            }
        }

        public class GeneratedTextResponse
        {
            public string EnglishText { get; set; } = string.Empty;
            public string RussianText { get; set; } = string.Empty;
            public Dictionary<string, string> Words { get; set; } = new();
        }
    }
}