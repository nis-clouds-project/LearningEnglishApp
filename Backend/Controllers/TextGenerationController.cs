using Backend.Integrations.Interfaces;
using Backend.Models;
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
        private readonly ITextGenerator _textGenerator;
        private readonly IWordManager _wordManager;
        private readonly IUserManager _userManager;

        /// <summary>
        /// Конструктор для внедрения зависимостей ITextGenerator, IWordManager и IUserManager.
        /// </summary>
        /// <param name="textGenerator">Сервис для генерации текста.</param>
        /// <param name="wordManager">Менеджер слов.</param>
        /// <param name="userManager">Менеджер пользователей.</param>
        public TextGenerationController(
            ITextGenerator textGenerator,
            IWordManager wordManager,
            IUserManager userManager)
        {
            _textGenerator = textGenerator;
            _wordManager = wordManager;
            _userManager = userManager;
        }

        /// <summary>
        /// Генерирует текст на основе слов из словаря пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="category">Категория слов (опционально).</param>
        /// <returns>Сгенерированный текст.</returns>
        /// <response code="200">Текст успешно сгенерирован.</response>
        /// <response code="404">Пользователь или слова не найдены.</response>
        /// <response code="500">Ошибка при генерации текста.</response>
        [HttpGet("generate")]
        public async Task<IActionResult> GenerateText([FromQuery] long userId, [FromQuery] string? category = null)
        {
            try
            {
                var user = await _userManager.GetUserAsync(userId);
                if (user == null)
                    return NotFound($"Пользователь с ID {userId} не найден");

                var words = await _wordManager.GetRandomWordsForGeneratingTextAsync(userId, category);
                if (!words.Any())
                    return NotFound("Недостаточно слов для генерации текста");

                var wordsWithTranslations = words.ToDictionary(w => w.Text, w => w.Translation);
                var text = await _textGenerator.GenerateTextWithTranslationsAsync(wordsWithTranslations);

                return Ok(text);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}