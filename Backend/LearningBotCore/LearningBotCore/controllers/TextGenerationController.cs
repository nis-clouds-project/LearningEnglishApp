using LearningBotCore.integrations.interfaces;
using LearningBotCore.model;
using Microsoft.AspNetCore.Mvc;

namespace LearningBotCore.controllers
{
    /// <summary>
    /// Контроллер для генерации текста на основе списка слов.
    /// Предоставляет API для взаимодействия с сервисом генерации текста.
    /// </summary>
    [ApiController]
    [Route("api/text-generation")] // Базовый маршрут для контроллера: /api/text-generation
    public class TextGenerationController : ControllerBase
    {
        private readonly ITextGenerator _textGenerator;

        /// <summary>
        /// Конструктор для внедрения зависимости ITextGenerator.
        /// </summary>
        /// <param name="textGenerator">Сервис для генерации текста.</param>
        public TextGenerationController(ITextGenerator textGenerator)
        {
            _textGenerator = textGenerator;
        }

        /// <summary>
        /// Генерирует текст на основе списка слов.
        /// </summary>
        /// <param name="words">Список слов, на основе которых будет сгенерирован текст.</param>
        /// <returns>Сгенерированный текст.</returns>
        /// <response code="200">Текст успешно сгенерирован.</response>
        /// <response code="400">Некорректный запрос (например, пустой список слов).</response>
        /// <response code="500">Ошибка при генерации текста.</response>
        [HttpPost("generate")]
        public IActionResult GenerateText([FromBody] List<Word> words)
        {
            try
            {
                // Генерируем текст с использованием ITextGenerator
                var generatedText = _textGenerator.GenerateText(words);

                // Возвращаем сгенерированный текст с кодом 200 OK
                return Ok(generatedText);
            }
            catch (ArgumentException ex)
            {
                // Возвращаем 400 Bad Request с сообщением об ошибке
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Возвращаем 500 Internal Server Error с сообщением об ошибке
                return StatusCode(500, ex.Message);
            }
        }
    }
}