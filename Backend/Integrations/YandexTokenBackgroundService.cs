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
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _tokenService.RefreshTokenAsync();
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken); 
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing token");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }
    }
}