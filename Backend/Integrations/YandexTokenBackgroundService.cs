using Backend.Integrations.Interfaces;

namespace Backend.Integrations
{
    public class YandexTokenBackgroundService : BackgroundService
    {
        private readonly IYandexTokenService _tokenService;
        private readonly ILogger<YandexTokenBackgroundService> _logger;

        public YandexTokenBackgroundService(
            IYandexTokenService tokenService,
            ILogger<YandexTokenBackgroundService> logger)
        {
            _tokenService = tokenService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await _tokenService.StartTokenRefreshAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in token refresh background service");
            }
        }
    }
}