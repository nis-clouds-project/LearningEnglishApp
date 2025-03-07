using IntegrationWithTelegram.clients;
using IntegrationWithTelegram.managers;
using IntegrationWithTelegram.models;
using Telegram.Bot;

namespace IntegrationWithTelegram.handlers;

/// <summary>
/// Обработчик для работы с главным меню бота.
/// Отвечает за обработку команд пользователя в состоянии ожидания запроса и начала работы.
/// </summary>
public static class MainMenuHandler
{
    /// <summary>
    /// Обрабатывает состояние ожидания запроса от пользователя.
    /// </summary>
    /// <param name="chatId">Идентификатор чата пользователя.</param>
    /// <param name="messageText">Текст сообщения пользователя.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <param name="botClient">Клиент бота для взаимодействия с Telegram API.</param>
    public static async Task HandleWaitingRequestState(
        long chatId,
        string messageText,
        CancellationToken cancellationToken,
        ITelegramBotClient botClient)
    {
        // Обрабатываем команду пользователя в зависимости от текста сообщения.
        switch (messageText)
        {
            case "📚 Учить лексику":
                // Устанавливаем состояние выбора категории для изучения слов.
                UserStateManager.SetState(chatId, State.LearningWordsGetCategory);
                await BotManager.CommandHandler(chatId, "", cancellationToken);
                break;

            case "➕ Добавить новое слово в словарь":
                // Устанавливаем состояние добавления нового слова.
                await botClient.SendMessage(chatId, "Введите слово: ", cancellationToken: cancellationToken);
                UserStateManager.SetState(chatId, State.AddingWordGetText);
                break;

            case "🔤 Практика перевода":
                // Устанавливаем состояние выбора категории для практики перевода.
                UserStateManager.SetState(chatId, State.PracticeGetCategory);
                await BotManager.CommandHandler(chatId, "", cancellationToken);
                break;

            default:
                // Если команда не распознана, отправляем пользователю клавиатуру с основными действиями.
                await botClient.SendMessage(chatId, "Неверное действие.", cancellationToken: cancellationToken);
                await ButtonPanelManager.SendButtonsToGetAction(chatId, botClient);
                break;
        }
    }

    /// <summary>
    /// Обрабатывает состояние ожидания команды /start.
    /// </summary>
    /// <param name="chatId">Идентификатор чата пользователя.</param>
    /// <param name="messageText">Текст сообщения пользователя.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <param name="botClient">Клиент бота для взаимодействия с Telegram API.</param>
    public static async Task HandleWaitingStart(
        long chatId,
        string messageText,
        CancellationToken cancellationToken,
        ITelegramBotClient botClient)
    {
        // Проверяем, является ли сообщение командой /start.
        if (messageText == "/start")
        {
            // Создаем пользователя и отправляем клавиатуру с основными действиями.
            CreateUser(chatId);
            await ButtonPanelManager.SendButtonsToGetAction(chatId, botClient);
        }
        else
        {
            // Если команда не /start, просим пользователя ввести /start.
            await botClient.SendMessage(
                chatId,
                "Пожалуйста, введите команду /start, чтобы начать.",
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Создает нового пользователя и добавляет его в систему.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    private static void CreateUser(long userId)
    {
        // Создаем объект пользователя.
        var user = new User(userId);

        // Добавляем пользователя в систему.
        UserApiClient.AddUser(user);
    }
}