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

        public GigaChatTextGenerator(
            IConfiguration configuration,
            ITokenService tokenService)
        {
            _apiUrl = configuration["GigaChat:ApiUrl"] ?? 
                throw new ArgumentNullException("GigaChat:ApiUrl not configured");
            _tokenService = tokenService ?? 
                throw new ArgumentNullException(nameof(tokenService));
        }

        /// <summary>
        /// Генерирует текст на основе слов с их переводами.
        /// </summary>
        public async Task<GeneratedText> GenerateTextWithTranslationsAsync(IDictionary<string, string> words)
        {
            try
            {

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
                return GenerateFallbackText(words);
            }
        }

        private (string EnglishText, string RussianText, Dictionary<string, string> Words) ParseGeneratedText(string text)
        {
            try
            {
                var englishText = string.Empty;
                var russianText = string.Empty;
                var words = new Dictionary<string, string>();

                var englishStartMarker = "===ENGLISH_TEXT_START===";
                var englishEndMarker = "===ENGLISH_TEXT_END===";
                var englishStart = text.IndexOf(englishStartMarker);
                var englishEnd = text.IndexOf(englishEndMarker);
                
                if (englishStart != -1 && englishEnd != -1)
                {
                    englishStart += englishStartMarker.Length;
                    englishText = text.Substring(englishStart, englishEnd - englishStart).Trim();
                }

                var russianStartMarker = "===RUSSIAN_TEXT_START===";
                var russianEndMarker = "===RUSSIAN_TEXT_END===";
                var russianStart = text.IndexOf(russianStartMarker);
                var russianEnd = text.IndexOf(russianEndMarker);
                
                if (russianStart != -1 && russianEnd != -1)
                {
                    russianStart += russianStartMarker.Length;
                    russianText = text.Substring(russianStart, russianEnd - russianStart).Trim();
                }

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
                    return ("Error: Invalid response format", "Ошибка: Неверный формат ответа", new Dictionary<string, string>());
                }

                return (englishText, russianText, words);
            }
            catch (Exception ex)
            {
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
                string accessToken = _tokenService.GetAccessToken();

                var options = new RestClientOptions(_apiUrl)
                {
                    RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
                    {
                        return true;
                    },
                    Timeout = TimeSpan.FromSeconds(30)
                };

                using var client = new RestClient(options);
                var request = BuildRequest(accessToken, prompt);
                
                var response = await client.ExecuteAsync(request);

                if (!response.IsSuccessful)
                {
                    throw new InvalidOperationException($"Error during request: {response.ErrorMessage}");
                }
                var result = ExtractTextFromResponse(response);
                
                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        
        private RestRequest BuildRequest(string accessToken, string message)
        {
            try
            {
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
                return request;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private string ExtractTextFromResponse(RestResponse response)
        {
            try
            {
                if (string.IsNullOrEmpty(response.Content))
                {
                    throw new InvalidOperationException("Empty response from server.");
                }

                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(response.Content);

                if (!jsonResponse.TryGetProperty("choices", out var choices) ||
                    choices.GetArrayLength() == 0)
                {
                    throw new InvalidOperationException("Invalid response format: missing or empty choices array");
                }

                var firstChoice = choices[0];
                if (!firstChoice.TryGetProperty("message", out var message) ||
                    !message.TryGetProperty("content", out var content))
                {
                    throw new InvalidOperationException("Invalid response format: missing message or content");
                }

                var generatedText = content.GetString();
                if (string.IsNullOrEmpty(generatedText))
                {
                    throw new InvalidOperationException("Server response does not contain generated text.");
                }

                return generatedText;
            }
            catch (JsonException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}