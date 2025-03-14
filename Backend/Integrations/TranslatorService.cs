using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using Backend.Controllers.Responses;
using Backend.Integrations.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backend.Integrations
{
    public class TranslatorService : ITranslatorService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IYandexTokenService _tokenService;
        private readonly ILogger<TranslatorService> _logger;
        private readonly string _folderId;

        public TranslatorService(
            HttpClient httpClient,
            IConfiguration configuration,
            IYandexTokenService tokenService,
            ILogger<TranslatorService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _tokenService = tokenService;
            _logger = logger;
            _folderId = _configuration["Yandex:FolderId"] ?? throw new ArgumentNullException("Yandex:FolderId not configured");
        }

        public async Task<string> TranslateAsync(string text, string targetLanguage)
        {
            try
            {
                var token = await _tokenService.GetIamTokenAsync();
                
                var request = new
                {
                    folderId = _folderId,
                    texts = new[] { text },
                    targetLanguageCode = targetLanguage
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json"
                );

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var response = await _httpClient.PostAsync(
                    "https://translate.api.cloud.yandex.net/translate/v2/translate",
                    content
                );

                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<YandexTranslateResponse>(responseString);

                return result?.Translations?.FirstOrDefault()?.Text ?? text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error translating text: {Text} to language: {Language}", text, targetLanguage);
                throw;
            }
        }

        public async Task<List<LanguageInfo>> GetSupportedLanguagesAsync()
        {
            try
            {
                var token = await _tokenService.GetIamTokenAsync();

                var request = new
                {
                    folderId = _folderId
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json"
                );

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

                var response = await _httpClient.PostAsync(
                    "https://translate.api.cloud.yandex.net/translate/v2/languages",
                    content
                );

                response.EnsureSuccessStatusCode();

                var responseString = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<YandexLanguagesResponse>(responseString);

                return result?.Languages?.Select(l => new LanguageInfo
                {
                    Code = l.Code,
                    Name = l.Name
                }).ToList() ?? new List<LanguageInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting supported languages");
                throw;
            }
        }

        private class YandexTranslateResponse
        {
            public List<Translation> Translations { get; set; } = new();

            public class Translation
            {
                public string Text { get; set; } = string.Empty;
            }
        }

        private class YandexLanguagesResponse
        {
            public List<Language> Languages { get; set; } = new();

            public class Language
            {
                public string Code { get; set; } = string.Empty;
                public string Name { get; set; } = string.Empty;
            }
        }
    }
}