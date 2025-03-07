using IntegrationWithTelegram.handlers;
using IntegrationWithTelegram.models;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace IntegrationWithTelegram.managers;

/// <summary>
/// Менеджер для управления ботом.
/// Отвечает за инициализацию бота, обработку входящих сообщений и управление состоянием пользователя.
/// </summary>
public static class BotManager
{
    // Токен бота для доступа к API Telegram.
    private const string Token = "7565442867:AAFdGWP_L46qCKZLRHeJujyna_9xy1rvUE4";

    // Клиент для взаимодействия с Telegram Bot API.
    private static readonly ITelegramBotClient BotClient = new TelegramBotClient(Token);

    /// <summary>
    /// Запускает бота и начинает обработку входящих сообщений.
    /// </summary>
    public static void Start()
    {
        BotClient.StartReceiving(UpdateHandler, ErrorHandler);
        Console.ReadLine(); // Ожидание ввода для предотвращения завершения программы.
    }

    /// <summary>
    /// Обрабатывает входящие обновления от Telegram.
    /// </summary>
    /// <param name="botClient">Клиент бота.</param>
    /// <param name="update">Входящее обновление.</param>
    /// <param name="token">Токен отмены.</param>
    private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken token)
    {
        // Проверяем, содержит ли обновление текстовое сообщение.
        if (update.Message?.Text != null)
        {
            var chatId = update.Message.Chat.Id; // ID чата пользователя.
            var messageText = update.Message.Text; // Текст сообщения.

            // Обрабатываем команду пользователя.
            await CommandHandler(chatId, messageText, token);
        }
    }

    /// <summary>
    /// Обрабатывает команды пользователя в зависимости от текущего состояния.
    /// </summary>
    /// <param name="chatId">ID чата пользователя.</param>
    /// <param name="messageText">Текст сообщения пользователя.</param>
    /// <param name="token">Токен отмены.</param>
    public static async Task CommandHandler(long chatId, string messageText, CancellationToken token)
    {
        // Получаем текущее состояние пользователя.
        var state = UserStateManager.GetState(chatId);

        // В зависимости от состояния вызываем соответствующий обработчик.
        switch (state)
        {
            case State.WaitingStart:
                await MainMenuHandler.HandleWaitingStart(chatId, messageText, token, BotClient);
                break;
            case State.WaitingRequest:
                await MainMenuHandler.HandleWaitingRequestState(chatId, messageText, token, BotClient);
                break;

            case State.LearningWordsGetCategory:
                await LearningWordsHandler.HandleGetCategory(chatId, BotClient);
                break;

            case State.LearningWordsShowWords:
                await LearningWordsHandler.HandleShowWords(chatId, messageText, token, BotClient);
                break;

            case State.LearningWordsAskNext:
                await LearningWordsHandler.HandleAskNext(chatId, messageText, token, BotClient);
                break;

            case State.AddingWordGetText:
                await AddingWordsHandler.HandleAddingNewWord(chatId, messageText, token,
                    BotClient);
                break;

            case State.PracticeGetCategory:
                await PracticeOfWordsHandler.HandleGetCategory(chatId, BotClient);
                break;

            case State.PracticeShowWord:
                await PracticeOfWordsHandler.HandleGenerateText(chatId, messageText, BotClient, token);
                break;
        }
    }

    /// <summary>
    /// Обрабатывает ошибки, возникающие при работе бота.
    /// </summary>
    /// <param name="botClient">Клиент бота.</param>
    /// <param name="exception">Исключение, вызвавшее ошибку.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Задача, представляющая асинхронную операцию.</returns>
    private static Task ErrorHandler(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        // Выводим сообщение об ошибке в консоль.
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }
}