using Backend.Integrations.Interfaces;
using Backend.Models;
using RestSharp;

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
        public async Task<GeneratedText> GenerateTextWithTranslationsAsync(IDictionary<string, string> words)
        {
            try
            {
                _logger.LogInformation("Generating text with {Count} words", words.Count);

                var prompt = BuildPromptWithTranslations(words);
                var generatedText = await GenerateTextWithPrompt(prompt);
                var (englishText, russianText, usedWords) = ParseGeneratedText(generatedText);

                return new GeneratedText(englishText, russianText)
                {
                    Words = usedWords
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating text with translations");
                return GenerateFallbackText(words);
            }
        }

        private (string EnglishText, string RussianText, Dictionary<string, string> Words) ParseGeneratedText(string text)
        {
            try
            {
                _logger.LogInformation("Starting to parse generated text");
                
                var englishText = string.Empty;
                var russianText = string.Empty;
                var words = new Dictionary<string, string>();

                // Extract English text
                var englishStartMarker = "===ENGLISH_TEXT_START===";
                var englishEndMarker = "===ENGLISH_TEXT_END===";
                var englishStart = text.IndexOf(englishStartMarker);
                var englishEnd = text.IndexOf(englishEndMarker);
                
                if (englishStart != -1 && englishEnd != -1)
                {
                    englishStart += englishStartMarker.Length;
                    englishText = text.Substring(englishStart, englishEnd - englishStart).Trim();
                }

                // Extract Russian text
                var russianStartMarker = "===RUSSIAN_TEXT_START===";
                var russianEndMarker = "===RUSSIAN_TEXT_END===";
                var russianStart = text.IndexOf(russianStartMarker);
                var russianEnd = text.IndexOf(russianEndMarker);
                
                if (russianStart != -1 && russianEnd != -1)
                {
                    russianStart += russianStartMarker.Length;
                    russianText = text.Substring(russianStart, russianEnd - russianStart).Trim();
                }

                // Extract words
                var wordsStartMarker = "===USED_WORDS_START===";
                var wordsEndMarker = "===USED_WORDS_END===";
                var wordsStart = text.IndexOf(wordsStartMarker);
                var wordsEnd = text.IndexOf(wordsEndMarker);
                
                if (wordsStart != -1 && wordsEnd != -1)
                {
                    wordsStart += wordsStartMarker.Length;
                    var wordsSection = text.Substring(wordsStart, wordsEnd - wordsStart);
                    
                    foreach (var line in wordsSection.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var parts = line.Trim().Split(':', 2);
                        if (parts.Length == 2)
                        {
                            words[parts[0].Trim()] = parts[1].Trim();
                        }
                    }
                }

                if (string.IsNullOrEmpty(englishText) || string.IsNullOrEmpty(russianText))
                {
                    _logger.LogWarning("Failed to extract text using markers. Response format was incorrect");
                    return ("Error: Invalid response format", "Ошибка: Неверный формат ответа", new Dictionary<string, string>());
                }

                _logger.LogInformation("Successfully parsed generated text. Found {WordCount} words", words.Count);
                return (englishText, russianText, words);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing generated text");
                return ("Error generating text", "Ошибка генерации текста", new Dictionary<string, string>());
            }
        }

        private string BuildPromptWithTranslations(IDictionary<string, string> wordsWithTranslations)
        {
            var wordsList = string.Join("\n", wordsWithTranslations.Select(w => $"- {w.Key}: {w.Value}"));
            
            return @$"You are a story generator that creates engaging short stories in English and Russian.

Words to use (must use ALL of them):
{wordsList}

Generate a response in EXACTLY this format (keep all markers and sections):

===ENGLISH_TEXT_START===
[Your English story goes here, using ALL provided words]
===ENGLISH_TEXT_END===

===RUSSIAN_TEXT_START===
[Russian translation of the story goes here]
===RUSSIAN_TEXT_END===

===USED_WORDS_START===
[List each used word and its translation, one per line]
===USED_WORDS_END===

Rules:
1. Use ALL provided words in the English text
2. Make the story natural and engaging
3. Provide accurate Russian translation
4. List ALL used words with translations
5. Keep ALL section markers exactly as shown
6. Do not add any text outside the marked sections";
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
                        return true;
                    },
                    Timeout = TimeSpan.FromSeconds(30)
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