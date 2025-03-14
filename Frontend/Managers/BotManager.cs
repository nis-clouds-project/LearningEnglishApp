using Frontend.Services;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Frontend.Models;

namespace Frontend.Managers
{
    /// <summary>
    /// Менеджер для управления ботом.
    /// Отвечает за инициализацию бота, обработку входящих сообщений и управление состоянием пользователя.
    /// </summary>
    public static class BotManager
    {
        public static TelegramBotClient? Bot { get; private set; }
        public static ApiClient? ApiClient { get; private set; }
        private static CancellationTokenSource? _cts;

        /// <summary>
        /// Запускает бота и начинает обработку входящих сообщений.
        /// </summary>
        public static async Task StartAsync()
        {
            var token = Environment.GetEnvironmentVariable("TELEGRAMBotManager.Bot_TOKEN") 
                       ?? throw new InvalidOperationException("TELEGRAMBotManager.Bot_TOKEN не задан");
            var baseUrl = Environment.GetEnvironmentVariable("BACKEND_API_URL") 
                       ?? throw new InvalidOperationException("BACKEND_API_URL не задан");
            
            
            Bot = new TelegramBotClient(token);
            ApiClient = new ApiClient(baseUrl);
            _cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = [],
                ThrowPendingUpdates = true
            };

            try
            {
                await Bot.GetMeAsync(_cts.Token);

                Bot.StartReceiving(
                    updateHandler: UpdateRoute.HandleUpdateAsync,
                    pollingErrorHandler: UpdateRoute.HandleErrorAsync,
                    receiverOptions: receiverOptions,
                    cancellationToken: _cts.Token
                );

                while (!_cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(1000, _cts.Token);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при запуске бота: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Останавливает бота.
        /// </summary>
        public static void Stop()
        {
            _cts?.Cancel();
            ApiClient?.Dispose();
        }
    }
}