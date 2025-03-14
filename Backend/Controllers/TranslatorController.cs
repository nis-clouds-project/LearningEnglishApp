using Microsoft.AspNetCore.Mvc;
using Backend.Services.Interfaces;
using Backend.Models;
using Backend.Controllers.Responses;
using Backend.Integrations.Interfaces;

namespace Backend.Controllers
{
    /// <summary>
    /// Контроллер для работы с переводами.
    /// Предоставляет API для перевода слов с использованием Yandex Translator API.
    /// </summary
    [ApiController]
    [Route("api/[controller]")]

    public class TranslatorController : ControllerBase
    {
        private readonly ITranslatorService _translatorService;
        private readonly IWordManager _wordManager;
        private readonly ILogger<TranslatorController> _logger;

        public TranslatorController(ITranslatorService translatorService, IWordManager wordManager, ILogger<TranslatorController> logger)
        {
            _translatorService = translatorService;
            _wordManager = wordManager;
            _logger = logger;
        }

         /// <summary>
        /// Переводит слово на указанный язык.
        /// </summary>
        /// <param name="request">Запрос на перевод.</param>
        /// <returns>Результат перевода.</returns>
        /// <response code="200">Перевод успешно выполнен.</response>
        /// <response code="400">Некорректный запрос.</response>
        /// <response code="500">Ошибка при выполнении перевода.</response>
        [HttpPost("translate")]
        public async Task<IActionResult> Translate([FromBody] TranslateRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Text))
                {
                    return BadRequest("Text is required");
                }

                // Если целевой язык русский, сначала проверяем в базе данных
                if (request.TargetLanguageCode.ToLower() == "ru")
                {
                    var allWords = await _wordManager.GetAllWordsAsync();
                    var existingWord = allWords.FirstOrDefault(w => 
                        w.Text.Equals(request.Text, StringComparison.OrdinalIgnoreCase));
                    
                    if (existingWord != null)
                    {
                        return Ok(new TranslationResponse
                        {
                            OriginalText = request.Text,
                            TranslatedText = existingWord.Translation,
                            TargetLanguage = "ru",
                            Source = "database"
                        });
                    }
                }

                // Если целевой язык не русский, используем Yandex Translator API
                var translation = await _translatorService.TranslateAsync(request.Text, request.TargetLanguageCode);

                return Ok(new TranslationResponse
                {
                    OriginalText = request.Text,
                    TranslatedText = translation,
                    TargetLanguage = request.TargetLanguageCode,
                    Source = "yandex"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error translating text: {Text}", request.Text);
                return StatusCode(500, "Translation service error");
            }
        }

        /// <summary>
        /// Получает список всех поддерживаемых языков.
        /// </summary>
        /// <returns>Список всех поддерживаемых языков.</returns>
        [HttpGet("languages")]
        public async Task<IActionResult> GetSupportedLanguages()
        {
            try
            {
                var languages = await _translatorService.GetSupportedLanguagesAsync();
                return Ok(languages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supported languages");
                return StatusCode(500, "Error getting supported languages");
            }
        }


    /// <summary>
        /// Сохраняет переведенное слово в базу данных.
        /// </summary>
        /// <param name="request">Запрос на сохранение перевода.</param>
        /// <returns>Сохраненное слово.</returns>
        /// <response code="200">Слово успешно сохранено.</response>
        /// <response code="400">Некорректный запрос.</response>
        [HttpPost("save")]
        public async Task<ActionResult<Word>> SaveTranslation([FromBody] SaveTranslationRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Text) || 
                    string.IsNullOrWhiteSpace(request.Translation))
                {
                    return BadRequest("Text and Translation are required");
                }

                // Use a default category ID (e.g., 1) if none is provided
                var categoryId = request.CategoryId ?? 1L; // 1L creates a long literal

                var word = await _wordManager.AddCustomWordAsync(
                    request.UserId,
                    request.Text,
                    request.Translation,
                    categoryId);  // Now passing a non-nullable long

                return Ok(word);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving translation for user {UserId}", request.UserId);
                return StatusCode(500, "Error saving translation");
            }
        }
    }
}

