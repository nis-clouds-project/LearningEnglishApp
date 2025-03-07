using IntegrationWithTelegram.clients;
using IntegrationWithTelegram.managers;
using Telegram.Bot;

namespace IntegrationWithTelegram.handlers;

/// <summary>
/// Обработчик для добавления новых слов в словарь пользователя.
/// </summary>
public static class AddingWordsHandler
{
    /// <summary>
    /// Обрабатывает добавление нового слова в словарь пользователя.
    /// </summary>
    /// <param name="chatId">Идентификатор чата пользователя.</param>
    /// <param name="messageText">Текст сообщения пользователя (слово для добавления).</param>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <param name="botClient">Клиент бота для взаимодействия с Telegram API.</param>
    public static async Task HandleAddingNewWord(
        long chatId,
        string messageText,
        CancellationToken cancellationToken,
        ITelegramBotClient botClient)
    {
        try
        {

            // Добавляем слово в словарь пользователя.
            await WordApiClient.AddCustomWordInUserVocabularyAsync(chatId, messageText.ToLower());

            // Уведомляем пользователя об успешном добавлении.
            await NotifyUser(chatId, "Слово добавлено.", cancellationToken, botClient);

            // Возвращаем пользователя в главное меню.
            await ButtonPanelManager.SendButtonsToGetAction(chatId, botClient);
        }
        catch (Exception e)
        {
            await HandleError(chatId,e.Message , cancellationToken, botClient);
        }
    }

    /// <summary>
    /// Уведомляет пользователя о результате операции.
    /// </summary>
    /// <param name="chatId">Идентификатор чата пользователя.</param>
    /// <param name="message">Сообщение для отправки.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <param name="botClient">Клиент бота для взаимодействия с Telegram API.</param>
    private static async Task NotifyUser(
        long chatId,
        string message,
        CancellationToken cancellationToken,
        ITelegramBotClient botClient)
    {
        await botClient.SendMessage(chatId, message, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Обрабатывает ошибку и возвращает пользователя в главное меню.
    /// </summary>
    /// <param name="chatId">Идентификатор чата пользователя.</param>
    /// <param name="errorMessage">Сообщение об ошибке.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <param name="botClient">Клиент бота для взаимодействия с Telegram API.</param>
    private static async Task HandleError(
        long chatId,
        string errorMessage,
        CancellationToken cancellationToken,
        ITelegramBotClient botClient)
    {
        // Уведомляем пользователя об ошибке.
        await NotifyUser(chatId, errorMessage, cancellationToken, botClient);

        // Возвращаем пользователя в главное меню.
        await ButtonPanelManager.SendButtonsToGetAction(chatId, botClient);
    }
}