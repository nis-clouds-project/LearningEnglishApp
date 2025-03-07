using IntegrationWithTelegram.clients;
using IntegrationWithTelegram.managers;
using IntegrationWithTelegram.models;
using Telegram.Bot;

namespace IntegrationWithTelegram.handlers;

/// <summary>
/// Обработчик для работы с изучением слов.
/// Отвечает за обработку команд пользователя в процессе изучения слов.
/// </summary>
public static class LearningWordsHandler
{
    // Словарь для хранения настроек пользователей.
    private static readonly Dictionary<long, OptionsOfUser> Options = new();

    /// <summary>
    /// Обрабатывает запрос на выбор категории для изучения слов.
    /// </summary>
    /// <param name="chatId">Идентификатор чата пользователя.</param>
    /// <param name="botClient">Клиент бота для взаимодействия с Telegram API.</param>
    public static async Task HandleGetCategory(long chatId, ITelegramBotClient botClient)
    {
        // Устанавливаем состояние показа слов.
        UserStateManager.SetState(chatId, State.LearningWordsShowWords);

        // Отправляем клавиатуру с категориями.
        await ButtonPanelManager.SendButtonsToGetCategory(chatId, botClient);
    }

    /// <summary>
    /// Обрабатывает показ слов из выбранной категории.
    /// </summary>
    /// <param name="chatId">Идентификатор чата пользователя.</param>
    /// <param name="messageText">Текст сообщения пользователя.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <param name="botClient">Клиент бота для взаимодействия с Telegram API.</param>
    public static async Task HandleShowWords(
        long chatId,
        string messageText,
        CancellationToken cancellationToken,
        ITelegramBotClient botClient)
    {
        // Если пользователь выбрал "Закончить", возвращаем его в главное меню.
        if (messageText.Equals("Закончить"))
        {
            await ButtonPanelManager.SendButtonsToGetAction(chatId, botClient);
            return;
        }

        // Получаем категорию из сообщения пользователя.
        var category = GetCategory(chatId, messageText);

        try
        {
            var wordId = WordApiClient.GetRandomWordForLearningAsync(chatId, category).Result ?? throw new Exception();
            
            UpdateUserOptions(chatId, wordId, category);
            
            var word = WordApiClient.GetWordByIdAsync(wordId).Result ?? throw new Exception();
            
            await botClient.SendMessage(chatId, "Знаешь ли ты это слово?", cancellationToken: cancellationToken);
            await botClient.SendMessage(chatId, $"{word.Text} - {word.Translation}",
                cancellationToken: cancellationToken);

            // Отправляем клавиатуру с действиями для добавления слова.
            await ButtonPanelManager.SendButtonsAboutAddingWordInVocabulary(chatId, botClient);
        }
        catch (Exception ex)
        {
            // Обрабатываем ошибку, если слово не найдено.
            await HandleError(chatId, ex.InnerException!.Message, cancellationToken, botClient);
        }
    }

    /// <summary>
    /// Обрабатывает запрос пользователя на следующее действие после показа слова.
    /// </summary>
    /// <param name="chatId">Идентификатор чата пользователя.</param>
    /// <param name="messageText">Текст сообщения пользователя.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    /// <param name="botClient">Клиент бота для взаимодействия с Telegram API.</param>
    public static async Task HandleAskNext(
        long chatId,
        string messageText,
        CancellationToken cancellationToken,
        ITelegramBotClient botClient)
    {
        if (Options.TryGetValue(chatId, out var optionsOfUser))
        {
            switch (messageText.ToLower())
            {
                case "не знаю":
                    // Добавляем слово в словарь пользователя.
                    await WordApiClient.AddWordInUserVocabularyAsync(chatId, optionsOfUser.CurrentWord);

                    // Уведомляем пользователя.
                    await botClient.SendMessage(chatId, "Слово добавлено в ваш словарь.",
                        cancellationToken: cancellationToken);

                    // Устанавливаем состояние показа следующего слова.
                    UserStateManager.SetState(chatId, State.LearningWordsShowWords);

                    // Переходим к следующему шагу.
                    await NextStep(chatId, cancellationToken);
                    break;

                case "знаю":
                    // Устанавливаем состояние показа следующего слова.
                    UserStateManager.SetState(chatId, State.LearningWordsShowWords);

                    // Переходим к следующему шагу.
                    await NextStep(chatId, cancellationToken);
                    break;

                case "закончить":
                    // Возвращаем пользователя в главное меню.
                    await ButtonPanelManager.SendButtonsToGetAction(chatId, botClient);
                    break;

                default:
                    // Обрабатываем неизвестную команду.
                    await botClient.SendMessage(chatId, "Ошибка: неверная команда.",
                        cancellationToken: cancellationToken);
                    break;
            }
        }
    }

    /// <summary>
    /// Получает категорию из сообщения пользователя.
    /// </summary>
    /// <param name="chatId">Идентификатор чата пользователя.</param>
    /// <param name="messageText">Текст сообщения пользователя.</param>
    /// <returns>Категория слов.</returns>
    public static CategoryType GetCategory(long chatId, string messageText)
    {
        if (Enum.TryParse(messageText, out CategoryType category))
        {
            return category;
        }

        // Если категория не распознана, возвращаем текущую категорию из настроек пользователя.
        return Options[chatId].CategoryType;
    }

    /// <summary>
    /// Обновляет настройки пользователя.
    /// </summary>
    /// <param name="chatId">Идентификатор чата пользователя.</param>
    /// <param name="wordId">Идентификатор текущего слова.</param>
    /// <param name="category">Категория слов.</param>
    private static void UpdateUserOptions(long chatId, int wordId, CategoryType category)
    {
        var option = Options.TryGetValue(chatId, out var existingOption) ? existingOption : new OptionsOfUser();
        option.CurrentWord = wordId;
        option.CategoryType = category;
        Options[chatId] = option;
    }

    /// <summary>
    /// Переходит к следующему шагу.
    /// </summary>
    /// <param name="chatId">Идентификатор чата пользователя.</param>
    /// <param name="cancellationToken">Токен отмены для асинхронной операции.</param>
    private static async Task NextStep(long chatId, CancellationToken cancellationToken)
    {
        await BotManager.CommandHandler(chatId, "", cancellationToken);
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
        await botClient.SendMessage(chatId, errorMessage, cancellationToken: cancellationToken);
        await ButtonPanelManager.SendButtonsToGetAction(chatId, botClient);
    }
}