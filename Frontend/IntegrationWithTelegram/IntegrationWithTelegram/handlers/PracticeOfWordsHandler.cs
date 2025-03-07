using IntegrationWithTelegram.clients;
using IntegrationWithTelegram.managers;
using IntegrationWithTelegram.models;
using Telegram.Bot;

namespace IntegrationWithTelegram.handlers;

/// <summary>
/// Обработчик для практики перевода слов.
/// Отвечает за обработку команд пользователя в процессе практики перевода.
/// </summary>
public static class PracticeOfWordsHandler
{
    /// <summary>
    /// Обрабатывает запрос на выбор категории для практики перевода.
    /// </summary>
    /// <param name="chatId">Идентификатор чата пользователя.</param>
    /// <param name="botClient">Клиент бота для взаимодействия с Telegram API.</param>
    public static async Task HandleGetCategory(long chatId, ITelegramBotClient botClient)
    {
        // Устанавливаем состояние показа слов для практики перевода.
        UserStateManager.SetState(chatId, State.PracticeShowWord);

        // Отправляем клавиатуру с категориями.
        await ButtonPanelManager.SendButtonsToGetCategory(chatId, botClient);
    }

    /// <summary>
    /// Обрабатывает генерацию текста для практики перевода.
    /// </summary>
    /// <param name="chatId">Идентификатор чата пользователя.</param>
    /// <param name="messageText">Текст сообщения пользователя (выбранная категория).</param>
    /// <param name="botClient">Клиент бота для взаимодействия с Telegram API.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    public static async Task HandleGenerateText(
        long chatId,
        string messageText,
        ITelegramBotClient botClient,
        CancellationToken cancellationToken)
    {
        // Устанавливаем состояние ожидания запроса.
        UserStateManager.SetState(chatId, State.WaitingRequest);

        try
        {
            // Получаем пользователя.
            var user = UserApiClient.GetUser(chatId).Result ?? throw new Exception();

            // Проверяем, можно ли выполнить запрос к ИИ.
            if (!user.UserAiUsage.CanMakeRequest())
            {
                // Если лимит запросов превышен, уведомляем пользователя.
                await HandleError(chatId, "Лимит превышен! Попробуй позже.", cancellationToken, botClient);
                return;
            }

            // Получаем случайные слова для генерации текста.
            var category = LearningWordsHandler.GetCategory(chatId, messageText);
            var words = WordApiClient.GetRandomWordsForGeneratingTextAsync(chatId, category).Result ??
                        throw new Exception();

            // Генерируем и отправляем текст пользователю.
            await SendGeneratedText(chatId, words, botClient, cancellationToken);

            // Увеличиваем счетчик запросов к ИИ.
            user.UserAiUsage.IncrementRequestCount();

            // Возвращаем пользователя в главное меню.
            await ButtonPanelManager.SendButtonsToGetAction(chatId, botClient);
        }
        catch (Exception e)
        {
            await HandleError(chatId, e.InnerException.Message, cancellationToken, botClient);
        }
    }

    /// <summary>
    /// Отправляет сгенерированный текст пользователю.
    /// </summary>
    /// <param name="chatId">Идентификатор чата пользователя.</param>
    /// <param name="words">Список слов для генерации текста.</param>
    /// <param name="botClient">Клиент бота для взаимодействия с Telegram API.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    private static async Task SendGeneratedText(
        long chatId,
        List<Word> words,
        ITelegramBotClient botClient,
        CancellationToken cancellationToken)
    {
        var answer = TextGenerationApiClient.GenerateTextAsync(words).Result;

        // Отправляем сгенерированный текст пользователю.
        await botClient.SendMessage(
            chatId,
            answer,
            cancellationToken: cancellationToken);
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
        // Устанавливаем состояние ожидания запроса.
        UserStateManager.SetState(chatId, State.WaitingRequest);

        // Уведомляем пользователя об ошибке.
        await botClient.SendMessage(chatId, errorMessage, cancellationToken: cancellationToken);

        // Возвращаем пользователя в главное меню.
        await ButtonPanelManager.SendButtonsToGetAction(chatId, botClient);
    }
}