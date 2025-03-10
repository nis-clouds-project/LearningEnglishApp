using Backend.Integrations.Interfaces;
using Backend.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;
using System.Text.Json;

using JsonElement = System.Text.Json.JsonElement;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Backend.Integrations
{
    /// <summary>
    /// Класс для генерации текста с использованием GigaChat API.
    /// Реализует интерфейс <see cref="ITextGenerator"/> для генерации текста на основе списка слов.
    /// </summary>
    public class GigaChatTextGenerator : ITextGenerator
    {
        private readonly string _apiUrl;
        private readonly ITokenService _tokenService;
        private readonly ILogger<GigaChatTextGenerator> _logger;

        public GigaChatTextGenerator(
            IConfiguration configuration,
            ITokenService tokenService,
            ILogger<GigaChatTextGenerator> logger)
        {
            _apiUrl = configuration["GigaChat:ApiUrl"] ?? 
                throw new ArgumentNullException("GigaChat:ApiUrl not configured");
            _tokenService = tokenService ?? 
                throw new ArgumentNullException(nameof(tokenService));
            _logger = logger;
        }

        /// <summary>
        /// Генерирует текст на основе списка слов.
        /// </summary>
        public async Task<string> GenerateTextAsync(IEnumerable<string> words)
        {
            try
            {
                _logger.LogInformation("Начало генерации текста. Получение списка слов");
                var wordList = words.ToList();
                
                if (!wordList.Any())
                {
                    _logger.LogWarning("Попытка генерации текста без слов");
                    return "No words provided for text generation.";
                }

                // Формируем промпт для запроса к API
                string prompt = $"Generate a text in English using the following words: {string.Join(", ", wordList)}. " +
                              "The text should be natural and engaging, using the provided words in context.";

                _logger.LogInformation("Сформирован промпт для GigaChat: {Prompt}", prompt);
                return await GenerateTextWithPrompt(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при генерации текста");
                return $"Failed to generate text using GigaChat API. Using fallback: Here are the words: {string.Join(", ", words)}.";
            }
        }

        /// <summary>
        /// Генерирует текст на основе слов с их переводами.
        /// </summary>
        public async Task<GeneratedText> GenerateTextWithTranslationsAsync(IDictionary<string, string> wordsWithTranslations)
        {
            try
            {
                _logger.LogInformation("Начало генерации текста с переводами. Количество слов: {Count}", wordsWithTranslations.Count);
                
                if (!wordsWithTranslations.Any())
                {
                    _logger.LogWarning("Попытка генерации текста без слов");
                    return new GeneratedText("No words provided.", "Слова не предоставлены.");
                }

                var prompt = BuildPromptWithTranslations(wordsWithTranslations);
                _logger.LogInformation("Сформирован промпт для GigaChat: {Prompt}", prompt);

                var result = await GenerateTextWithPrompt(prompt);
                _logger.LogInformation("Текст успешно сгенерирован. Длина текста: {Length}", result.Length);

                // Разделяем английский и русский текст
                var (englishText, russianText) = SplitGeneratedText(result);
                return new GeneratedText(englishText, russianText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при генерации текста с переводами");
                return GenerateFallbackText(wordsWithTranslations);
            }
        }

        private (string EnglishText, string RussianText) SplitGeneratedText(string text)
        {
            try
            {
                _logger.LogInformation("Начало разделения текста на английскую и русскую части");
        
                // Разделяем текст по двойному переносу строки
                var parts = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        
                if (parts.Length >= 2)
                {
                    // Проверяем, какая часть содержит русский текст (кириллицу)
                    var firstPartHasRussian = parts[0].Any(c => c >= 'а' && c <= 'я' || c >= 'А' && c <= 'Я');
                    var secondPartHasRussian = parts[1].Any(c => c >= 'а' && c <= 'я' || c >= 'А' && c <= 'Я');
        
                    // Если первая часть содержит русский текст, а вторая — английский
                    if (firstPartHasRussian && !secondPartHasRussian)
                    {
                        _logger.LogInformation("Первая часть определена как русский текст, вторая — как английский");
                        return (parts[1].Trim(), parts[0].Trim());
                    }
                    // Если вторая часть содержит русский текст, а первая — английский
                    else if (!firstPartHasRussian && secondPartHasRussian)
                    {
                        _logger.LogInformation("Первая часть определена как английский текст, вторая — как русский");
                        return (parts[0].Trim(), parts[1].Trim());
                    }
                }
        
                // Если не удалось разделить, анализируем весь текст
                var isEnglishText = text.Count(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')) >
                                    text.Count(c => (c >= 'а' && c <= 'я') || (c >= 'А' && c <= 'Я'));
        
                if (isEnglishText)
                {
                    _logger.LogWarning("Текст не удалось разделить, весь текст определен как английский");
                    return (text.Trim(), "Перевод не был предоставлен.");
                }
                else
                {
                    _logger.LogWarning("Текст не удалось разделить, весь текст определен как русский");
                    return ("Text was not provided in English.", text.Trim());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при разделении текста на английскую и русскую части");
                return (text.Trim(), "Ошибка при выделении перевода.");
            }
        }

        private string BuildPromptWithTranslations(IDictionary<string, string> wordsWithTranslations)
        {
            var wordPairs = wordsWithTranslations.Select(w => $"{w.Key} ({w.Value})");
            return "Create a bilingual story using the following English words with their Russian translations: " +
                   $"{string.Join(", ", wordPairs)}.\n\n" +
                   "Instructions:\n" +
                   "1. First, write an engaging paragraph in English using all the English words naturally in context.\n" +
                   "2. Then, add two newlines and write a Russian translation of the same story.\n" +
                   "3. Make sure to use all provided words in a natural and connected way.\n" +
                   "4. The story should be engaging and meaningful.\n\n" +
                   "Format your response as:\n" +
                   "[English text]\n\n" +
                   "[Russian translation]";
        }

        private GeneratedText GenerateFallbackText(IDictionary<string, string> wordsWithTranslations)
        {
            var englishSentences = new List<string>();
            var russianSentences = new List<string>();
            
            foreach (var pair in wordsWithTranslations)
            {
                englishSentences.Add($"The word '{pair.Key}' in English.");
                russianSentences.Add($"Слово '{pair.Key}' переводится как '{pair.Value}'.");
            }
            
            return new GeneratedText(
                string.Join("\n", englishSentences),
                string.Join("\n", russianSentences)
            );
        }

        private async Task<string> GenerateTextWithPrompt(string prompt)
        {
            try
            {
                _logger.LogInformation("Получение токена доступа");
                string accessToken = _tokenService.GetAccessToken();
                _logger.LogInformation("Токен доступа получен успешно");

                var options = new RestClientOptions(_apiUrl)
                {
                    RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
                    {
                        _logger.LogInformation("Проверка SSL сертификата. Ошибки: {Errors}", errors);
                        return true; // Принимаем любой сертификат
                    },
                    MaxTimeout = 30000 // 30 секунд таймаут для генерации текста
                };

                using var client = new RestClient(options);
                var request = BuildRequest(accessToken, prompt);
                
                _logger.LogInformation("Отправка запроса к GigaChat API");
                var response = await client.ExecuteAsync(request);
                _logger.LogInformation("Получен ответ от GigaChat API. Статус: {Status}", response.StatusCode);

                if (!response.IsSuccessful)
                {
                    _logger.LogError("Ошибка при запросе к GigaChat API. Статус: {Status}, Ошибка: {Error}, Содержимое: {Content}", 
                        response.StatusCode, response.ErrorMessage, response.Content);
                    throw new InvalidOperationException($"Error during request: {response.ErrorMessage}");
                }
                _logger.LogInformation("Начало обработки ответа от GigaChat");
                var result = ExtractTextFromResponse(response);
                _logger.LogInformation("Ответ от GigaChat успешно обработан");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при генерации текста через GigaChat API");
                throw;
            }
        }

        /// <summary>
        /// Создает и настраивает запрос к GigaChat API.
        /// </summary>
        private RestRequest BuildRequest(string accessToken, string message)
        {
            try
            {
                _logger.LogInformation("Создание запроса к GigaChat API");
                var request = new RestRequest("", Method.Post);

                request.AddHeader("Accept", "application/json");
                request.AddHeader("Authorization", $"Bearer {accessToken}");
                request.AddHeader("Content-Type", "application/json");

                var requestBody = new
                {
                    model = "GigaChat",
                    messages = new[]
                    {
                        new { role = "user", content = message }
                    },
                    temperature = 0.7,
                    max_tokens = 1000
                };

                request.AddJsonBody(requestBody);
                _logger.LogInformation("Запрос к GigaChat API успешно создан");
                return request;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании запроса к GigaChat API");
                throw;
            }
        }

        private string ExtractTextFromResponse(RestResponse response)
        {
            try
            {
                _logger.LogInformation("Начало извлечения текста из ответа GigaChat");
                
                if (string.IsNullOrEmpty(response.Content))
                {
                    _logger.LogError("Получен пустой ответ от сервера GigaChat");
                    throw new InvalidOperationException("Empty response from server.");
                }

                _logger.LogInformation("Десериализация ответа от GigaChat. Содержимое: {Content}", response.Content);
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response.Content);

                // Проверяем наличие необходимых полей
                if (!jsonResponse.TryGetProperty("choices", out var choices) || 
                    choices.GetArrayLength() == 0)
                {
                    _logger.LogError("В ответе отсутствует массив choices или он пуст");
                    throw new InvalidOperationException("Invalid response format: missing or empty choices array");
                }

                var firstChoice = choices[0];
                if (!firstChoice.TryGetProperty("message", out var message) ||
                    !message.TryGetProperty("content", out var content))
                {
                    _logger.LogError("В ответе отсутствуют необходимые поля message или content");
                    throw new InvalidOperationException("Invalid response format: missing message or content");
                }

                var generatedText = content.GetString();
                if (string.IsNullOrEmpty(generatedText))
                {
                    _logger.LogError("Сгенерированный текст пуст");
                    throw new InvalidOperationException("Server response does not contain generated text.");
                }

                _logger.LogInformation("Текст успешно извлечен из ответа GigaChat");
                return generatedText;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Ошибка при десериализации ответа сервера. Содержимое ответа: {Content}", response.Content);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке ответа сервера");
                throw;
            }
        }

        /// <summary>
        /// Формирует промпт для запроса к API на основе списка слов.
        /// </summary>
        /// <param name="words">Список слов, которые должны быть использованы в тексте.</param>
        /// <returns>Сформированный промпт.</returns>
        /// <exception cref="ArgumentException">Если список слов пуст.</exception>
        public string BuildPromt(List<Word> words)
        {
            // Проверяем, что список слов не пустой
            if (words == null || words.Count == 0)
            {
                throw new ArgumentException("Список слов не может быть пустым.", nameof(words));
            }

            // Формируем промпт для ИИ
            var prompt = CreatePrompt(words);

            // Возвращаем промпт
            return prompt;
        }

        /// <summary>
        /// Создает промпт для запроса к API, используя список слов.
        /// </summary>
        /// <param name="words">Список слов, которые должны быть использованы в тексте.</param>
        /// <returns>Сформированный промпт.</returns>
        private string CreatePrompt(List<Word> words)
        {
            // Формируем строку с ключевыми словами
            var keywords = string.Join(", ", words);

            // Создаем промпт для ИИ
            var prompt =
                $"Напиши текст, состоящий из русской версии рассказа и английской, так чтобы на английском использовались следующие слова как можно чаще: {keywords}";

            return prompt;
        }
    }
}