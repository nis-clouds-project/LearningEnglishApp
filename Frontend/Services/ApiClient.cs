using System.Net.Http.Json;
using Frontend.Models;
using Microsoft.Extensions.Logging;

namespace Frontend.Services
{
    /// <summary>
    /// Клиент для взаимодействия с API бэкенда.
    /// </summary>
    public class ApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiClient> _logger;
        private bool _disposed;

        public ApiClient(string baseUrl)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ApiClient>();
        }

        /// <summary>
        /// Получает список категорий слов.
        /// </summary>
        /// <returns>Список категорий слов или null в случае ошибки.</returns>
        public async Task<List<Category>?> GetCategoriesAsync()
        {
            try
            {
                _logger.LogInformation("Getting categories");
                var response = await _httpClient.GetAsync("api/Word/categories");
                
                if (response.IsSuccessStatusCode)
                {
                    var categories = await response.Content.ReadFromJsonAsync<List<Category>>();
                    return categories;
                }
                
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get categories. Status: {Status}, Error: {Error}", 
                    response.StatusCode, error);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return null;
            }
        }

        /// <summary>
        /// Получает список изученных слов пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="categoryId">Идентификатор категории (опционально).</param>
        /// <returns>Список изученных слов или null в случае ошибки.</returns>
        public async Task<List<Word>?> GetLearnedWordsAsync(long userId, long? categoryId = null)
        {
            try
            {
                var url = categoryId.HasValue 
                    ? $"api/Word/learned?userId={userId}&categoryId={categoryId}"
                    : $"api/Word/learned?userId={userId}";
                
                _logger.LogInformation("Getting learned words for user {UserId}", userId);
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var words = await response.Content.ReadFromJsonAsync<List<Word>>();
                    return words;
                }
                
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get learned words. Status: {Status}, Error: {Error}", 
                    response.StatusCode, error);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting learned words for user {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// Генерирует текст на основе слов из словаря пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <returns>Сгенерированный текст или null в случае ошибки.</returns>
        public async Task<GeneratedTextResponse?> GenerateTextFromVocabularyAsync(long userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/TextGeneration/generate?userId={userId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<GeneratedTextResponse>();
                    return result;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Проверяет существование пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <returns>true, если пользователь существует; иначе false.</returns>
        public async Task<bool> UserExistsAsync(long userId)
        {
            try
            {
                _logger.LogInformation("Checking if user exists: {UserId}", userId);
                var response = await _httpClient.GetAsync($"api/User/exists?userId={userId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var exists = await response.Content.ReadFromJsonAsync<bool>();
                    return exists;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user existence {UserId}", userId);
                return false;
            }
        }

        /// <summary>
        /// Добавляет нового пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <returns>true, если пользователь успешно добавлен; иначе false.</returns>
        public async Task<User?> AddUserAsync(long userId)
        {
            try
            {
                _logger.LogInformation("Adding new user: {UserId}", userId);
                var response = await _httpClient.PostAsJsonAsync("api/User/add", userId);
                
                if (response.IsSuccessStatusCode)
                {
                    var user = await response.Content.ReadFromJsonAsync<User>();
                    _logger.LogInformation("Successfully added user {UserId}", userId);
                    return user;
                }
                
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to add user {UserId}. Status: {Status}, Error: {Error}", 
                    userId, response.StatusCode, error);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding user {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// Получает случайное слово для изучения.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="categoryId">Идентификатор категории (опционально).</param>
        /// <returns>Случайное слово или null в случае ошибки.</returns>
        public async Task<Word?> GetRandomWordAsync(long userId, long? categoryId = null)
        {
            try
            {
                var url = categoryId.HasValue 
                    ? $"api/Word/random?userId={userId}&categoryId={categoryId}"
                    : $"api/Word/random?userId={userId}";
                
                _logger.LogInformation("Getting random word for user {UserId} from category {CategoryId}", userId, categoryId);
                var response = await _httpClient.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var word = await response.Content.ReadFromJsonAsync<Word>();
                    return word;
                }
                
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to get random word. Status: {Status}, Error: {Error}", 
                    response.StatusCode, error);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting random word for user {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// Добавляет слово в словарь пользователя.
        /// </summary>
        /// <param name="userId">Идентификатор пользователя.</param>
        /// <param name="wordId">Идентификатор слова.</param>
        /// <returns>true, если слово успешно добавлено; иначе false.</returns>
        public async Task<bool> AddWordToVocabularyAsync(long userId, long wordId)
        {
            try
            {
                _logger.LogInformation("Adding word {WordId} to vocabulary for user {UserId}", wordId, userId);
                var response = await _httpClient.PostAsync($"api/Word/vocabulary/add?userId={userId}&wordId={wordId}", null);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully added word {WordId} to vocabulary for user {UserId}", wordId, userId);
                    return true;
                }
                
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to add word to vocabulary. Status: {Status}, Error: {Error}", 
                    response.StatusCode, error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding word {WordId} to vocabulary for user {UserId}", wordId, userId);
                return false;
            }
        }

        /// <summary>
        /// Получает слово по его идентификатору.
        /// </summary>
        public async Task<Word?> GetWordByIdAsync(long wordId)
        {
            try
            {
                _logger.LogInformation("Getting word: {WordId}", wordId);
                var response = await _httpClient.GetAsync($"api/Word/{wordId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var word = await response.Content.ReadFromJsonAsync<Word>();
                    return word;
                }
                
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Word {WordId} not found. Status: {Status}, Error: {Error}", 
                    wordId, response.StatusCode, error);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting word {WordId}", wordId);
                return null;
            }
        }
        
        public async Task<Word?> AddCustomWordAsync(long userId, string text, string translation)
        {
            try
            {
                _logger.LogInformation("Adding custom word for user {UserId}: {Text} - {Translation}", 
                    userId, text, translation);

                var request = new CustomWordRequest
                {
                    UserId = userId,
                    Text = text?.Trim(),
                    Translation = translation?.Trim()
                };

                var response = await _httpClient.PostAsJsonAsync("api/Word/custom", request);

                if (response.IsSuccessStatusCode)
                {
                    var word = await response.Content.ReadFromJsonAsync<Word>();
                    _logger.LogInformation("Successfully added custom word {WordId} for user {UserId}", 
                        word?.Id, userId);
                    return word;
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to add custom word. Status: {Status}, Error: {Error}", 
                    response.StatusCode, error);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding custom word for user {UserId}", userId);
                return null;
            }
        }

        public async Task<Word?> GetRandomCustomWordAsync(long userId)
        {
            try
            {
                _logger.LogInformation("Getting random custom word for user {UserId}", userId);
                var response = await _httpClient.GetAsync($"api/Word/custom/random?userId={userId}");
                
                if (response.IsSuccessStatusCode)
                {
                    var word = await response.Content.ReadFromJsonAsync<Word>();
                    return word;
                }
                
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to get random custom word. Status: {Status}, Error: {Error}", 
                    response.StatusCode, error);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting random custom word for user {UserId}", userId);
                return null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _httpClient.Dispose();
                }
                _disposed = true;
            }
        }
    }
} 